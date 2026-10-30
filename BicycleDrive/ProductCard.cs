using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BicycleDrive.Models;

namespace BicycleDrive
{
    /// <summary>
    /// Представление товара для карточки списка: текстовые поля, оформление
    /// (подсветка строки) и изображение.
    /// </summary>
    public class ProductCard
    {
        private static readonly Dictionary<string, ImageSource> ImageCache =
            new Dictionary<string, ImageSource>();

        private static readonly SolidColorBrush BrushText = Frozen("#1A1A2E");
        private static readonly SolidColorBrush BrushMuted = Frozen("#6B6B7B");
        private static readonly SolidColorBrush BrushWhite = Frozen("#FFFFFF");
        private static readonly SolidColorBrush BrushLightMuted = Frozen("#D8D3F0");
        private static readonly SolidColorBrush BrushOutText = Frozen("#6E6E6E");
        private static readonly SolidColorBrush BrushOldPrice = Frozen("#C62828");
        private static readonly SolidColorBrush BrushOldPriceOnDark = Frozen("#FF9A9A");

        private static readonly SolidColorBrush BgNormal = Frozen("#FFFFFF");
        private static readonly SolidColorBrush BgDiscount = Frozen("#483D8B");
        private static readonly SolidColorBrush BgOutOfStock = Frozen("#DCDCDC");

        public ProductCard(Product product)
        {
            Product = product;
            Photo = LoadImage(product.ImagePath);

            bool hasDiscount = product.Discount > 0m;
            bool isBigDiscount = product.Discount > 15m;
            bool outOfStock = product.Quantity <= 0;

            FinalPrice = hasDiscount
                ? Math.Round(product.Price * (1m - product.Discount / 100m), 2)
                : product.Price;

            // порядок приоритета оформления: нет на складе -> скидка > 15% -> обычная строка
            if (outOfStock)
            {
                RowBackground = BgOutOfStock;
                RowForeground = BrushOutText;
                MutedForeground = BrushOutText;
                OldPriceBrush = BrushOutText;
                FinalPriceBrush = BrushOutText;
            }
            else if (isBigDiscount)
            {
                RowBackground = BgDiscount;
                RowForeground = BrushWhite;
                MutedForeground = BrushLightMuted;
                OldPriceBrush = BrushOldPriceOnDark;
                FinalPriceBrush = BrushWhite;
            }
            else
            {
                RowBackground = BgNormal;
                RowForeground = BrushText;
                MutedForeground = BrushMuted;
                OldPriceBrush = BrushOldPrice;
                FinalPriceBrush = BrushText;
            }

            OldPriceText = product.Price.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";
            FinalPriceText = FinalPrice.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";
            OldPriceVisibility = hasDiscount
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;

            CategoryName = product.Category?.Name ?? "—";
            ManufacturerName = product.Manufacturer?.Name ?? "—";
            SupplierName = product.Supplier?.Name ?? "—";
            UnitName = product.Unit?.Name ?? "—";

            DiscountText = hasDiscount
                ? "Скидка: " + product.Discount.ToString("0.##", CultureInfo.GetCultureInfo("ru-RU")) + " %"
                : "Без скидки";

            StockText = outOfStock
                ? "Нет на складе"
                : "На складе: " + product.Quantity + " " + UnitName;

            SearchText = string.Join(" ", new[]
            {
                product.Article, product.Name, CategoryName, product.Description,
                ManufacturerName, SupplierName, UnitName
            }).ToLowerInvariant();
        }

        public Product Product { get; }
        public ImageSource Photo { get; }

        public string Article => Product.Article ?? "";
        public string Title => Product.Name ?? "";
        public string Description => Product.Description ?? "";
        public string CategoryName { get; }
        public string ManufacturerName { get; }
        public string SupplierName { get; }
        public string UnitName { get; }

        public string ArticleLine => "Артикул: " + Article;
        public string CategoryLine => "Категория: " + CategoryName;
        public string ManufacturerLine => "Производитель: " + ManufacturerName;
        public string SupplierLine => "Поставщик: " + SupplierName;
        public string UnitLine => "Единица измерения: " + UnitName;

        public decimal FinalPrice { get; }
        public string FinalPriceText { get; }
        public string OldPriceText { get; }
        public string DiscountText { get; }
        public string StockText { get; }
        public string SearchText { get; }

        public System.Windows.Visibility OldPriceVisibility { get; }

        public Brush RowBackground { get; }
        public Brush RowForeground { get; }
        public Brush MutedForeground { get; }
        public Brush OldPriceBrush { get; }
        public Brush FinalPriceBrush { get; }

        /// <summary>Ключ сортировки по цене (итоговая цена).</summary>
        public double SortPrice => (double)FinalPrice;

        /// <summary>Ключ сортировки по количеству на складе.</summary>
        public int SortQuantity => Product.Quantity;

        private static SolidColorBrush Frozen(string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze();
            return brush;
        }

        private static ImageSource LoadImage(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "picture.png";
            }

            if (ImageCache.TryGetValue(fileName, out ImageSource cached))
            {
                return cached;
            }

            string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            string path = Path.Combine(imagesDir, fileName);
            if (!File.Exists(path))
            {
                path = Path.Combine(imagesDir, "picture.png");
            }

            ImageSource source = null;
            try
            {
                if (File.Exists(path))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    source = bitmap;
                }
            }
            catch
            {
                source = null;
            }

            ImageCache[fileName] = source;
            return source;
        }
    }
}
