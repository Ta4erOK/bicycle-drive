using System;
using System.Linq;
using System.Windows;
using BicycleDrive.Models;
using Microsoft.EntityFrameworkCore;

namespace BicycleDrive
{
    /// <summary>Окно авторизации: вход по логину и паролю из БД или режим гостя.</summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            LoginBox.Focus();
        }

        /// <summary>Пользователь, вошедший в систему (null для гостя).</summary>
        public User SessionUser { get; private set; }

        /// <summary>Признак входа в режиме гостя.</summary>
        public bool IsGuest { get; private set; }

        private void LoginClick(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password;

            if (login.Length == 0 || password.Length == 0)
            {
                ErrorText.Text = "Введите логин и пароль.";
                return;
            }

            try
            {
                using (var context = new BicycleDriveContext())
                {
                    User user = context.Users
                        .Include(u => u.Role)
                        .FirstOrDefault(u => u.Login == login && u.Password == password);

                    if (user == null)
                    {
                        ErrorText.Text = "Неверный логин или пароль.";
                        PasswordBox.Clear();
                        PasswordBox.Focus();
                        return;
                    }

                    SessionUser = user;
                    IsGuest = false;
                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = "Ошибка подключения к базе данных: " + ex.Message;
            }
        }

        private void GuestClick(object sender, RoutedEventArgs e)
        {
            SessionUser = null;
            IsGuest = true;
            DialogResult = true;
        }
    }
}
