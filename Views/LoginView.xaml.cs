using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using SchoolJournalApp.Services;

namespace SchoolJournalApp.Views
{
    public partial class LoginView : UserControl
    {
        private MainWindow _mainWindow;
        private bool isDatabaseConnected = false;

        public LoginView(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // Спробуємо завантажити збережений логін при старті
            try
            {
                string savedLogin = LocalSettings.GetSavedLogin();
                if (!string.IsNullOrEmpty(savedLogin))
                {
                    txtUsername.Text = savedLogin;
                    chkRememberMe.IsChecked = true;
                }
            }
            catch { /* Якщо помилка файлу - ігноруємо */ }

            // Логіка визначення стану: підключено чи ні
            if (!string.IsNullOrEmpty(AppSession.ConnectionString))
            {
                // Стан 1: БД ВЖЕ ПІДКЛЮЧЕНА
                DbSettings.Visibility = Visibility.Collapsed;
                isDatabaseConnected = true;
                txtStatus.Text = "Введіть облікові дані";
                btnLogin.Content = "УВІЙТИ";
                LoginFields.Visibility = Visibility.Visible;
            }
            else
            {
                // Стан 2: БД НЕ ПІДКЛЮЧЕНА
                txtStatus.Text = "Спершу підключіться до бази даних.";
                DbSettings.Visibility = Visibility.Visible;
                btnLogin.Content = "ПІДКЛЮЧИТИСЯ";
                LoginFields.Visibility = Visibility.Collapsed;
            }
        }

        // --- ГОЛОВНА КНОПКА (ЦЕЙ МЕТОД ВИКЛИКАЄТЬСЯ ПРИ КЛІКУ) ---
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (!isDatabaseConnected)
            {
                // Якщо ще не підключені -> Підключаємось
                AttemptDatabaseConnection();
            }
            else
            {
                // Якщо вже підключені -> Робимо вхід (Login)
                AttemptUserLogin(txtUsername.Text, txtPassword.Password);
            }
        }

        private void AttemptDatabaseConnection()
        {
            string server = txtServer.Text;
            string db = txtDatabase.Text;
            bool isTrusted = chkTrusted.IsChecked == true;

            string connString = $"Server={server};Database={db};TrustServerCertificate=True;";

            if (isTrusted)
                connString += "Trusted_Connection=True;";
            else
                connString += "User Id=sa;Password=yourpassword;";

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    AppSession.ConnectionString = connString;
                    isDatabaseConnected = true;

                    // Зміна стану інтерфейсу
                    DbSettings.Visibility = Visibility.Collapsed;
                    LoginFields.Visibility = Visibility.Visible; // Показуємо поля логіну
                    txtStatus.Text = "Підключення успішне! Введіть логін та пароль.";
                    btnLogin.Content = "УВІЙТИ"; // Змінюємо напис на кнопці
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося підключитися до БД:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Помилка підключення. Перевірте налаштування.";
                DbSettings.Visibility = Visibility.Visible;
                LoginFields.Visibility = Visibility.Collapsed;
            }
        }

        private void AttemptUserLogin(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                txtStatus.Text = "Введіть логін та пароль.";
                return;
            }

            string hashedPassword = AppSession.HashPassword(password);

            try
            {
                // 1. Готуємо запит
                string sql = @"SELECT t.TeacherID, 
                              t.LastName + ' ' + t.FirstName AS FullName, 
                              t.AccessRoleID 
                       FROM Teachers t 
                       WHERE t.Login = @Login AND t.Password = @HashedPassword";

                // 2. Готуємо параметри
                SqlParameter[] parameters = {
                new SqlParameter("@Login", username),
                new SqlParameter("@HashedPassword", hashedPassword)
                };

                // 3. Використовуємо наш Helper!
                DataTable dt = DatabaseHelper.GetDataTable(sql, parameters);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0]; // Беремо перший знайдений рядок

                    // === ВХІД УСПІШНИЙ ===
                    AppSession.CurrentUserId = (int)row["TeacherID"];
                    AppSession.CurrentUserName = row["FullName"].ToString();

                    // Зберігаємо роль
                    if (row["AccessRoleID"] != DBNull.Value)
                        AppSession.CurrentRoleID = (int)row["AccessRoleID"];
                    else
                        AppSession.CurrentRoleID = 3;

                    // Зберігаємо логін у файл (якщо стоїть галочка)
                    if (chkRememberMe.IsChecked == true)
                        LocalSettings.SaveLogin(username);
                    else
                        LocalSettings.ClearLogin();

                    // Перехід на головне вікно
                    _mainWindow.NavigateToDashboard();
                }
                else
                {
                    txtStatus.Text = "Невірний логін або пароль.";
                    txtPassword.Password = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при спробі входу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}