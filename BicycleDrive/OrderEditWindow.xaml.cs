using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using BicycleDrive.Models;
using Microsoft.EntityFrameworkCore;

namespace BicycleDrive
{
    /// <summary>
    /// Форма добавления и редактирования заказа (доступна администратору).
    /// </summary>
    public partial class OrderEditWindow : Window
    {
        private readonly BicycleDriveContext _context = new BicycleDriveContext();
        private readonly Order _order;
        private readonly bool _isNew;
        private readonly ObservableCollection<OrderLine> _lines = new ObservableCollection<OrderLine>();
        private readonly List<ProductChoice> _products = new List<ProductChoice>();

        public OrderEditWindow(int? orderId = null)
        {
            InitializeComponent();

            ItemsList.ItemsSource = _lines;
            LoadDictionaries();

            _isNew = orderId == null;
            if (_isNew)
            {
                Title = "Добавление заказа — ВелосипедДрайв";
                HeaderText.Text = "Добавление заказа";
                DeleteButton.Visibility = Visibility.Collapsed;
                OrderDateBox.SelectedDate = DateTime.Today;

                int nextNumber = 1;
                using (var context = new BicycleDriveContext())
                {
                    if (context.Orders.Any())
                    {
                        nextNumber = context.Orders.Max(o => o.Number) + 1;
                    }
                }

                NumberBox.Text = nextNumber.ToString();
                CodeBox.Text = "0";
            }
            else
            {
                Title = "Редактирование заказа — ВелосипедДрайв";
                HeaderText.Text = "Редактирование заказа";
                DeleteButton.Visibility = Visibility.Visible;

                _order = _context.Orders
                    .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                    .FirstOrDefault(o => o.Id == orderId.Value);

                if (_order == null)
                {
                    MessageBox.Show(this, "Заказ не найден.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    return;
                }

                FillFields(_order);
            }
        }

        private void LoadDictionaries()
        {
            var users = _context.Users.Include(u => u.Role).OrderBy(u => u.FullName).ToList();
            UserBox.ItemsSource = users
                .Select(u => new UserChoice { Id = u.Id, Display = u.FullName + " · " + (u.Role?.Name ?? "") })
                .ToList();

            StatusBox.ItemsSource = _context.Statuses.OrderBy(s => s.Name).ToList();
            PickupBox.ItemsSource = _context.PickupPoints.OrderBy(p => p.Address).ToList();

            var products = _context.Products.OrderBy(p => p.Name).ToList();
            foreach (Product product in products)
            {
                _products.Add(new ProductChoice
                {
                    Id = product.Id,
                    Display = product.Article + " · " + product.Name,
                    Price = product.Price,
                    Discount = product.Discount
                });
            }

            ProductBox.ItemsSource = _products;
        }

        private void FillFields(Order order)
        {
            NumberBox.Text = order.Number.ToString();
            CodeBox.Text = order.Code.ToString();
            OrderDateBox.SelectedDate = order.OrderDate.ToDateTime(TimeOnly.MinValue);
            DeliveryDateBox.SelectedDate = order.DeliveryDate?.ToDateTime(TimeOnly.MinValue);

            UserBox.SelectedItem = (UserBox.ItemsSource as List<UserChoice>)?
                .FirstOrDefault(u => u.Id == order.UserId);
            StatusBox.SelectedItem = (StatusBox.ItemsSource as List<Status>)?
                .FirstOrDefault(s => s.Id == order.StatusId);
            PickupBox.SelectedItem = (PickupBox.ItemsSource as List<PickupPoint>)?
                .FirstOrDefault(p => p.Id == order.PickupPointId);

            _lines.Clear();
            foreach (OrderItem item in order.OrderItems)
            {
                ProductChoice choice = _products.FirstOrDefault(p => p.Id == item.ProductId);
                if (choice == null)
                {
                    continue;
                }

                _lines.Add(new OrderLine(choice, item.Quantity));
            }
        }

        private void AddItemClick(object sender, RoutedEventArgs e)
        {
            if (!(ProductBox.SelectedItem is ProductChoice choice))
            {
                Error("Выберите товар для добавления в заказ.");
                return;
            }

            if (!int.TryParse((ItemQuantityBox.Text ?? "").Trim(), out int quantity) || quantity <= 0)
            {
                Error("Количество должно быть целым положительным числом.");
                return;
            }

            OrderLine existing = _lines.FirstOrDefault(l => l.ProductId == choice.Id);
            if (existing != null)
            {
                existing.Quantity += quantity;
                ItemsList.Items.Refresh();
            }
            else
            {
                _lines.Add(new OrderLine(choice, quantity));
            }

            ItemQuantityBox.Text = "";
        }

        private void RemoveItemClick(object sender, RoutedEventArgs e)
        {
            if (ItemsList.SelectedItem is OrderLine line)
            {
                _lines.Remove(line);
            }
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse((NumberBox.Text ?? "").Trim(), out int number) || number <= 0)
            {
                Error("Номер заказа должен быть целым положительным числом.");
                return;
            }

            if (!int.TryParse((CodeBox.Text ?? "").Trim(), out int code) || code < 0)
            {
                Error("Код должен быть целым неотрицательным числом.");
                return;
            }

            if (!(UserBox.SelectedItem is UserChoice userChoice))
            {
                Error("Выберите клиента.");
                return;
            }

            if (!(StatusBox.SelectedItem is Status status))
            {
                Error("Выберите статус заказа.");
                return;
            }

            if (!(PickupBox.SelectedItem is PickupPoint pickup))
            {
                Error("Выберите пункт выдачи.");
                return;
            }

            if (OrderDateBox.SelectedDate == null)
            {
                Error("Укажите дату заказа.");
                return;
            }

            if (_lines.Count == 0)
            {
                Error("Добавьте хотя бы одну позицию в заказ.");
                return;
            }

            try
            {
                int currentId = _order?.Id ?? 0;
                bool numberTaken = _context.Orders.Any(o => o.Number == number && o.Id != currentId);
                if (numberTaken)
                {
                    Error("Заказ с номером " + number + " уже существует.");
                    return;
                }

                Order target = _order;
                if (_isNew)
                {
                    target = new Order();
                    _context.Orders.Add(target);
                }

                target.Number = number;
                target.Code = code;
                target.UserId = userChoice.Id;
                target.StatusId = status.Id;
                target.PickupPointId = pickup.Id;
                target.OrderDate = DateOnly.FromDateTime(OrderDateBox.SelectedDate.Value);
                target.DeliveryDate = DeliveryDateBox.SelectedDate == null
                    ? (DateOnly?)null
                    : DateOnly.FromDateTime(DeliveryDateBox.SelectedDate.Value);

                _context.SaveChanges();

                // состав заказа пересобирается целиком
                List<OrderItem> oldItems = _context.OrderItems.Where(i => i.OrderId == target.Id).ToList();
                _context.OrderItems.RemoveRange(oldItems);
                _context.SaveChanges();

                foreach (OrderLine line in _lines)
                {
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = target.Id,
                        ProductId = line.ProductId,
                        Quantity = line.Quantity
                    });
                }

                _context.SaveChanges();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                Error("Не удалось сохранить заказ: " + ex.Message);
            }
        }

        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            if (_order == null)
            {
                return;
            }

            MessageBoxResult answer = MessageBox.Show(this,
                "Удалить заказ №" + _order.Number + "?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                List<OrderItem> items = _context.OrderItems.Where(i => i.OrderId == _order.Id).ToList();
                _context.OrderItems.RemoveRange(items);
                _context.Orders.Remove(_order);
                _context.SaveChanges();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                Error("Не удалось удалить заказ: " + ex.Message);
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Error(string message)
        {
            MessageBox.Show(this, message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>Пользователь в выпадающем списке.</summary>
    public class UserChoice
    {
        public int Id { get; set; }
        public string Display { get; set; }
    }

    /// <summary>Товар в выпадающем списке.</summary>
    public class ProductChoice
    {
        public int Id { get; set; }
        public string Display { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }

        public decimal FinalPrice => Math.Round(Price * (1m - Discount / 100m), 2);
    }

    /// <summary>Позиция заказа в форме.</summary>
    public class OrderLine
    {
        public OrderLine(ProductChoice product, int quantity)
        {
            ProductId = product.Id;
            Display = product.Display;
            FinalPrice = product.FinalPrice;
            Quantity = quantity;
        }

        public int ProductId { get; }
        public string Display { get; }
        public decimal FinalPrice { get; }
        public int Quantity { get; set; }

        public string PriceText => FinalPrice.ToString("N2", CultureInfo.GetCultureInfo("ru-RU"));
        public string SumText => (FinalPrice * Quantity).ToString("N2", CultureInfo.GetCultureInfo("ru-RU"));
    }
}
