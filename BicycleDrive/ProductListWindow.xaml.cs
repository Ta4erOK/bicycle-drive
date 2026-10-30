using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BicycleDrive.Models;
using Microsoft.EntityFrameworkCore;

namespace BicycleDrive
{
    /// <summary>
    /// Главное окно: список товаров с фотографиями, поиском, фильтрацией и сортировкой.
    /// </summary>
    public partial class ProductListWindow : Window
    {
        private readonly User _user;
        private readonly bool _isGuest;
        private readonly ObservableCollection<ProductCard> _cards = new ObservableCollection<ProductCard>();
        private ICollectionView _view;
        private List<string> _categoryNames = new List<string>();

        public ProductListWindow(User user = null, bool isGuest = true)
        {
            InitializeComponent();
            _user = user;
            _isGuest = isGuest;

            LoadProducts();
            SetupHeader();
        }

        /// <summary>Пользователь нажал «Выход» — нужно вернуться к окну входа.</summary>
        public bool LogoutRequested { get; private set; }

        /// <summary>Администратор и менеджер работают с поиском, фильтром и сортировкой.</summary>
        private bool CanManage
        {
            get
            {
                if (_isGuest || _user?.Role == null)
                {
                    return false;
                }

                string role = _user.Role.Name ?? "";
                return role.Equals("Администратор", StringComparison.OrdinalIgnoreCase)
                    || role.Equals("Менеджер", StringComparison.OrdinalIgnoreCase);
            }
        }

        private void SetupHeader()
        {
            if (_isGuest)
            {
                UserText.Text = "Гость (только просмотр)";
            }
            else
            {
                UserText.Text = _user.FullName + " · " + (_user.Role?.Name ?? "пользователь");
            }

            FilterPanel.Visibility = CanManage ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadProducts()
        {
            try
            {
                using (var context = new BicycleDriveContext())
                {
                    List<Product> products = context.Products
                        .Include(p => p.Category)
                        .Include(p => p.Manufacturer)
                        .Include(p => p.Supplier)
                        .Include(p => p.Unit)
                        .OrderBy(p => p.Id)
                        .ToList();

                    _cards.Clear();
                    foreach (Product product in products)
                    {
                        _cards.Add(new ProductCard(product));
                    }

                    _categoryNames = products
                        .Select(p => p.Category?.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct()
                        .OrderBy(n => n)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось загрузить список товаров: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            _view = CollectionViewSource.GetDefaultView(_cards);
            _view.Filter = FilterCard;
            ProductsList.ItemsSource = _view;

            var categories = new List<string> { "Все категории" };
            categories.AddRange(_categoryNames);
            CategoryBox.ItemsSource = categories;
            CategoryBox.SelectedIndex = 0;

            SortBox.ItemsSource = new List<string>
            {
                "По наименованию (А–Я)",
                "По цене (возрастание)",
                "По цене (убывание)",
                "По остатку на складе"
            };
            SortBox.SelectedIndex = 0;
        }

        private bool FilterCard(object item)
        {
            if (!(item is ProductCard card))
            {
                return true;
            }

            string category = CategoryBox?.SelectedItem as string;
            if (!string.IsNullOrEmpty(category) && category != "Все категории"
                && !string.Equals(card.CategoryName, category, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!CanManage || SearchBox == null)
            {
                return true;
            }

            string query = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
            if (query.Length == 0)
            {
                return true;
            }

            // каждое слово запроса должно встречаться в объединённом тексте полей товара
            foreach (string word in query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!card.SearchText.Contains(word))
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplySort()
        {
            if (_view == null)
            {
                return;
            }

            _view.SortDescriptions.Clear();
            switch (SortBox.SelectedIndex)
            {
                case 1:
                    _view.SortDescriptions.Add(new SortDescription(nameof(ProductCard.SortPrice), ListSortDirection.Ascending));
                    break;
                case 2:
                    _view.SortDescriptions.Add(new SortDescription(nameof(ProductCard.SortPrice), ListSortDirection.Descending));
                    break;
                case 3:
                    _view.SortDescriptions.Add(new SortDescription(nameof(ProductCard.SortQuantity), ListSortDirection.Descending));
                    break;
                default:
                    _view.SortDescriptions.Add(new SortDescription(nameof(ProductCard.Title), ListSortDirection.Ascending));
                    break;
            }
        }

        private void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            _view?.Refresh();
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            _view?.Refresh();
        }

        private void SortChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplySort();
        }

        private void ResetClick(object sender, RoutedEventArgs e)
        {
            if (SearchBox != null)
            {
                SearchBox.Text = "";
            }

            if (CategoryBox != null)
            {
                CategoryBox.SelectedIndex = 0;
            }

            if (SortBox != null)
            {
                SortBox.SelectedIndex = 0;
            }

            _view?.Refresh();
        }

        private void LogoutClick(object sender, RoutedEventArgs e)
        {
            LogoutRequested = true;
            Close();
        }
    }
}
