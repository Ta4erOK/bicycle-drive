using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using BicycleDrive.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

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

        /// <summary>Файл, выбранный пользователем в диалоге (ещё не скопирован в Images).</summary>
        private FileInfo _selectedImage;

        /// <summary>Текущее относительное имя файла изображения (то, что лежит в БД).</summary>
        private string _imageName = ImageStorage.Placeholder;

        /// <summary>Пользователь нажал «Удалить изображение» — при сохранении ставим заглушку.</summary>
        private bool _imageRemoved;

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
                UpdateImagePreview();
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

            // В БД лежит относительное имя файла — картинку строим через ImageStorage.
            _imageName = string.IsNullOrWhiteSpace(product.ImagePath)
                ? ImageStorage.Placeholder
                : product.ImagePath;
            UpdateImagePreview();

            CategoryBox.SelectedItem = CategoryBox.Items.Cast<Category>()
                .FirstOrDefault(c => c.Id == product.CategoryId);
            ManufacturerBox.SelectedItem = ManufacturerBox.Items.Cast<Manufacturer>()
                .FirstOrDefault(m => m.Id == product.ManufacturerId);
            SupplierBox.SelectedItem = SupplierBox.Items.Cast<Supplier>()
                .FirstOrDefault(s => s.Id == product.SupplierId);
            UnitBox.SelectedItem = UnitBox.Items.Cast<Unit>()
                .FirstOrDefault(u => u.Id == product.UnitId);
        }

        /// <summary>
        /// Показывает текущее изображение товара (то, что записано в БД).
        /// В БД хранится только имя файла, поэтому путь собирает ImageStorage.
        /// </summary>
        private void UpdateImagePreview()
        {
            PreviewImage.Source = ImageStorage.GetImageWithoutCaching(_imageName);

            bool isPlaceholder = string.Equals(_imageName, ImageStorage.Placeholder,
                StringComparison.OrdinalIgnoreCase);
            CurrentImageText.Text = "Текущий файл: " + _imageName
                + (isPlaceholder ? " (заглушка)" : "");
        }

        /// <summary>
        /// Показывает выбранный файл на форме сразу, ДО сохранения в БД.
        /// _selectedImage имеет тип FileInfo — то есть это реальный файл на диске,
        /// который ещё не скопирован в папку Images.
        /// </summary>
        private void ChangePhotoVisualOnly(FileInfo selectedImage)
        {
            _selectedImage = selectedImage;

            if (selectedImage == null)
            {
                PreviewImage.Source = ImageStorage.GetImageWithoutCaching(ImageStorage.Placeholder);
                CurrentImageText.Text = "Будет использована заглушка picture.png";
                return;
            }

            PreviewImage.Source = ImageStorage.GetImageFromFile(selectedImage.FullName);
            CurrentImageText.Text = "Выбран файл: " + selectedImage.Name
                + " (скопируется в папку Images при сохранении)";
        }

        private void ChooseImageClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выбор изображения товара",
                Filter = "Изображения (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif"
                       + "|Все файлы (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            _imageRemoved = false;
            ChangePhotoVisualOnly(new FileInfo(dialog.FileName));
        }

        private void RemoveImageClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult answer = MessageBox.Show(this,
                "Убрать изображение товара? Будет подставлена заглушка picture.png, "
                + "а текущий файл удалён после сохранения.", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            _imageRemoved = true;
            ChangePhotoVisualOnly(null);
        }

        /// <summary>
        /// Удаляет файл изображения, если на него больше не ссылается ни один товар.
        /// Вызывается после сохранения/удаления товара — чтобы в папке Images
        /// не оставалось «мусорных» файлов.
        /// </summary>
        private void DeleteImageIfUnused(string imageName, int exceptProductId)
        {
            if (string.IsNullOrWhiteSpace(imageName))
            {
                return;
            }

            if (string.Equals(Path.GetFileName(imageName), ImageStorage.Placeholder,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool usedByAnotherProduct = _context.Products
                .Any(p => p.ImagePath == imageName && p.Id != exceptProductId);
            if (usedByAnotherProduct)
            {
                return;
            }

            string error;
            if (!ImageStorage.DeleteImage(imageName, out error) && !string.IsNullOrEmpty(error))
            {
                // товар уже сохранён, поэтому это только предупреждение
                MessageBox.Show(this, "Изменения сохранены, но файл изображения удалить не удалось.\n"
                    + error, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>Разбор неотрицательного числа (принимает и точку, и запятую).</summary>
        private static bool TryParseNonNegative(string text, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            // убираем ЛЮБЫЕ пробельные символы (в т.ч. неразрывный пробел U+00A0,
            // которым ru-RU разделяет тысячи) и знак рубля
            var digits = new List<char>();
            foreach (char symbol in text.Trim())
            {
                if (!char.IsWhiteSpace(symbol) && symbol != '₽' && symbol != '\u00A0')
                {
                    digits.Add(symbol);
                }
            }

            string prepared = new string(digits.ToArray());            if (prepared.Contains(",") && prepared.Contains("."))
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

            // ---- изображение ----
            // previousImage — имя файла, которое уже записано в БД (или заглушка).
            // Если пользователь выбрал новый файл — копируем его в папку Images
            // и записываем в БД новое относительное имя.
            string previousImage = _imageName;
            string image = previousImage;

            if (_selectedImage != null)
            {
                string imageError;
                string savedName = ImageStorage.SaveImage(_selectedImage, out imageError);
                if (savedName == null)
                {
                    Error(imageError);
                    return;
                }

                image = savedName;
            }
            else if (_imageRemoved)
            {
                image = ImageStorage.Placeholder;
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

                // Старый файл больше не нужен — удаляем его с диска,
                // если на него не ссылается другой товар.
                if (!string.Equals(previousImage, image, StringComparison.OrdinalIgnoreCase))
                {
                    DeleteImageIfUnused(previousImage, target.Id);
                }

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
                // запоминаем имя файла до удаления записи из БД
                string imageToDelete = _product.ImagePath;
                int productId = _product.Id;

                _context.Products.Remove(_product);
                _context.SaveChanges();

                // товара в БД больше нет — его файл изображения тоже не нужен
                DeleteImageIfUnused(imageToDelete, productId);

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
