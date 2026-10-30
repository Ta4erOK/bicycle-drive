using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using BicycleDrive.Models;
using Microsoft.EntityFrameworkCore;

namespace BicycleDrive
{
    /// <summary>
    /// Форма добавления и редактирования товара (доступна администратору).
    /// </summary>
    public partial class ProductEditWindow : Window
    {
        private readonly BicycleDriveContext _context = new BicycleDriveContext();
        private readonly Product _product;
        private readonly bool _isNew;

        public ProductEditWindow(int? productId = null)
        {
            InitializeComponent();

            LoadComboBoxes();

            _isNew = productId == null;
            if (_isNew)
            {
                Title = "Добавление товара — ВелосипедДрайв";
                HeaderText.Text = "Добавление товара";
                IdLabel.Visibility = Visibility.Collapsed;
                IdBox.Visibility = Visibility.Collapsed;
                DeleteButton.Visibility = Visibility.Collapsed;

                DiscountBox.Text = "0";
                QuantityBox.Text = "0";
                ImageBox.Text = "";
            }
            else
            {
                Title = "Редактирование товара — ВелосипедДрайв";
                HeaderText.Text = "Редактирование товара";
                DeleteButton.Visibility = Visibility.Visible;

                _product = _context.Products.FirstOrDefault(p => p.Id == productId.Value);
                if (_product == null)
                {
                    MessageBox.Show(this, "Товар не найден.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    return;
                }

                FillFields(_product);
            }
        }

        private void LoadComboBoxes()
        {
            CategoryBox.ItemsSource = _context.Categories.OrderBy(c => c.Name).ToList();
            ManufacturerBox.ItemsSource = _context.Manufacturers.OrderBy(m => m.Name).ToList();
            SupplierBox.ItemsSource = _context.Suppliers.OrderBy(s => s.Name).ToList();
            UnitBox.ItemsSource = _context.Units.OrderBy(u => u.Name).ToList();
        }

        private void FillFields(Product product)
        {
            IdBox.Text = product.Id.ToString();
            ArticleBox.Text = product.Article ?? "";
            NameBox.Text = product.Name ?? "";
            DescriptionBox.Text = product.Description ?? "";
            PriceBox.Text = product.Price.ToString("N2", CultureInfo.GetCultureInfo("ru-RU"));
            QuantityBox.Text = product.Quantity.ToString();
            DiscountBox.Text = product.Discount.ToString("0.##", CultureInfo.GetCultureInfo("ru-RU"));
            ImageBox.Text = product.ImagePath ?? "";

            CategoryBox.SelectedItem = CategoryBox.Items.Cast<Category>()
                .FirstOrDefault(c => c.Id == product.CategoryId);
            ManufacturerBox.SelectedItem = ManufacturerBox.Items.Cast<Manufacturer>()
                .FirstOrDefault(m => m.Id == product.ManufacturerId);
            SupplierBox.SelectedItem = SupplierBox.Items.Cast<Supplier>()
                .FirstOrDefault(s => s.Id == product.SupplierId);
            UnitBox.SelectedItem = UnitBox.Items.Cast<Unit>()
                .FirstOrDefault(u => u.Id == product.UnitId);
        }

        /// <summary>Разбор неотрицательного числа (принимает и точку, и запятую).</summary>
        private static bool TryParseNonNegative(string text, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string prepared = text.Trim().Replace(" ", "").Replace("₽", "");
            if (prepared.Contains(",") && prepared.Contains("."))
            {
                prepared = prepared.Replace(".", "");
            }

            prepared = prepared.Replace(',', '.');

            if (!decimal.TryParse(prepared, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
            {
                return false;
            }

            if (parsed < 0m)
            {
                return false;
            }

            value = parsed;
            return true;
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            string article = (ArticleBox.Text ?? "").Trim();
            string name = (NameBox.Text ?? "").Trim();

            if (article.Length == 0)
            {
                Error("Укажите артикул товара.");
                return;
            }

            if (name.Length == 0)
            {
                Error("Укажите наименование товара.");
                return;
            }

            if (!(CategoryBox.SelectedItem is Category category))
            {
                Error("Выберите категорию товара.");
                return;
            }

            if (!(ManufacturerBox.SelectedItem is Manufacturer manufacturer))
            {
                Error("Выберите производителя товара.");
                return;
            }

            if (!(SupplierBox.SelectedItem is Supplier supplier))
            {
                Error("Выберите поставщика товара.");
                return;
            }

            if (!(UnitBox.SelectedItem is Unit unit))
            {
                Error("Выберите единицу измерения.");
                return;
            }

            if (!TryParseNonNegative(PriceBox.Text, out decimal price))
            {
                Error("Цена должна быть неотрицательным числом (допускаются копейки).");
                return;
            }

            if (!TryParseNonNegative(QuantityBox.Text, out decimal quantityValue)
                || quantityValue != Math.Floor(quantityValue))
            {
                Error("Количество на складе должно быть целым неотрицательным числом.");
                return;
            }

            if (!TryParseNonNegative(DiscountBox.Text, out decimal discount))
            {
                Error("Скидка должна быть неотрицательным числом.");
                return;
            }

            if (discount >= 100m)
            {
                Error("Скидка должна быть меньше 100 %.");
                return;
            }

            string image = (ImageBox.Text ?? "").Trim();
            if (image.Length == 0)
            {
                image = "picture.png";
            }

            try
            {
                Product target = _product;
                if (_isNew)
                {
                    target = new Product();
                    _context.Products.Add(target);
                }

                target.Article = article;
                target.Name = name;
                target.Description = (DescriptionBox.Text ?? "").Trim();
                target.CategoryId = category.Id;
                target.ManufacturerId = manufacturer.Id;
                target.SupplierId = supplier.Id;
                target.UnitId = unit.Id;
                target.Price = Math.Round(price, 2);
                target.Quantity = (int)quantityValue;
                target.Discount = Math.Round(discount, 2);
                target.ImagePath = image;

                _context.SaveChanges();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                Error("Не удалось сохранить товар: " + ex.Message);
            }
        }

        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            if (_product == null)
            {
                return;
            }

            bool usedInOrders = _context.OrderItems.Any(i => i.ProductId == _product.Id);
            if (usedInOrders)
            {
                Error("Товар присутствует в заказе, удалить его нельзя.");
                return;
            }

            MessageBoxResult answer = MessageBox.Show(this,
                "Удалить товар «" + _product.Name + "»?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _context.Products.Remove(_product);
                _context.SaveChanges();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                Error("Не удалось удалить товар: " + ex.Message);
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
}
