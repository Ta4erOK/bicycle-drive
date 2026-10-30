using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BicycleDrive.Models;
using Microsoft.EntityFrameworkCore;

namespace BicycleDrive
{
    /// <summary>
    /// Просмотр заказов (менеджер, администратор); изменение — только администратор.
    /// </summary>
    public partial class OrdersWindow : Window
    {
        private readonly User _user;
        private readonly ObservableCollection<OrderRow> _rows = new ObservableCollection<OrderRow>();

        public OrdersWindow(User user)
        {
            InitializeComponent();
            _user = user;

            UserText.Text = user.FullName + " · " + (user.Role?.Name ?? "пользователь");
            ActionPanel.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;

            OrdersList.ItemsSource = _rows;
            LoadOrders();
        }

        private bool IsAdmin => string.Equals(_user?.Role?.Name, "Администратор",
            StringComparison.OrdinalIgnoreCase);

        private OrderRow Selected => OrdersList.SelectedItem as OrderRow;

        private void LoadOrders()
        {
            try
            {
                using (var context = new BicycleDriveContext())
                {
                    List<Order> orders = context.Orders
                        .Include(o => o.User)
                        .Include(o => o.PickupPoint)
                        .Include(o => o.Status)
                        .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                        .OrderBy(o => o.Number)
                        .ToList();

                    _rows.Clear();
                    foreach (Order order in orders)
                    {
                        _rows.Add(new OrderRow(order));
                    }

                    StatusText.Text = "Всего заказов: " + orders.Count;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось загрузить заказы: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddClick(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin)
            {
                return;
            }

            var window = new OrderEditWindow(null) { Owner = this };
            if (window.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        private void EditClick(object sender, RoutedEventArgs e)
        {
            OpenSelected();
        }

        private void OrdersListDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenSelected();
        }

        private void OpenSelected()
        {
            if (!IsAdmin)
            {
                MessageBox.Show(this, "Изменение заказов доступно только администратору.",
                    "Доступ ограничен", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OrderRow row = Selected;
            if (row == null)
            {
                MessageBox.Show(this, "Выберите заказ в списке.", "Сообщение",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new OrderEditWindow(row.OrderId) { Owner = this };
            if (window.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin)
            {
                return;
            }

            OrderRow row = Selected;
            if (row == null)
            {
                MessageBox.Show(this, "Выберите заказ в списке.", "Сообщение",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult answer = MessageBox.Show(this,
                "Удалить заказ №" + row.Number + "?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using (var context = new BicycleDriveContext())
                {
                    List<OrderItem> items = context.OrderItems.Where(i => i.OrderId == row.OrderId).ToList();
                    context.OrderItems.RemoveRange(items);
                    Order order = context.Orders.FirstOrDefault(o => o.Id == row.OrderId);
                    if (order != null)
                    {
                        context.Orders.Remove(order);
                    }

                    context.SaveChanges();
                }

                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось удалить заказ: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>Строка списка заказов.</summary>
    public class OrderRow
    {
        public OrderRow(Order order)
        {
            OrderId = order.Id;
            Number = order.Number;
            OrderDateText = order.OrderDate.ToString("dd.MM.yyyy");
            CustomerName = order.User?.FullName ?? "—";
            PickupAddress = order.PickupPoint?.Address ?? "—";
            StatusName = order.Status?.Name ?? "—";
            Code = order.Code;
            ItemsCount = order.OrderItems?.Count ?? 0;

            decimal total = 0m;
            foreach (OrderItem item in order.OrderItems ?? new List<OrderItem>())
            {
                decimal price = item.Product?.Price ?? 0m;
                decimal discount = item.Product?.Discount ?? 0m;
                total += Math.Round(price * (1m - discount / 100m), 2) * item.Quantity;
            }

            TotalText = total.ToString("N2", CultureInfo.GetCultureInfo("ru-RU"));
        }

        public int OrderId { get; }
        public int Number { get; }
        public string OrderDateText { get; }
        public string CustomerName { get; }
        public string PickupAddress { get; }
        public string StatusName { get; }
        public int ItemsCount { get; }
        public string TotalText { get; }
        public int Code { get; }
    }
}
