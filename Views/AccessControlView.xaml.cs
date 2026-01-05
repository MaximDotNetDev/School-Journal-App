using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using SchoolJournalApp.Services;

namespace SchoolJournalApp.Views
{
    public partial class AccessControlView : UserControl
    {
        public AccessControlView()
        {
            InitializeComponent();
            LoadRoles();
            LoadEmployees();
        }

        // Клас для відображення в списку
        public class EmployeeAccessInfo
        {
            public int TeacherID { get; set; }
            public string FullName { get; set; }
            public string Login { get; set; }
            public string PositionName { get; set; }
            public int RoleID { get; set; }
            public string RoleName { get; set; }

            // Для відображення статусу (зелений/сірий)
            public string StatusIcon => string.IsNullOrEmpty(Login) ? "⚪" : "🟢";
            public string StatusColor => string.IsNullOrEmpty(Login) ? "#CFD8DC" : "#4CAF50";
        }

        // 1. Завантаження ролей
        private void LoadRoles()
        {
            try
            {
                DataTable dt = DatabaseHelper.GetDataTable("SELECT RoleID, RoleName FROM AccessRoles");
                cmbAccessRole.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Помилка ролей: " + ex.Message); }
        }

        // 2. Завантаження працівників
        private void LoadEmployees()
        {
            try
            {
                string sql = @"
                    SELECT t.TeacherID, 
                           t.LastName + ' ' + t.FirstName + ' ' + ISNULL(t.MiddleName, '') AS FullName, 
                           t.Login, 
                           p.PositionName,
                           t.AccessRoleID,
                           r.RoleName
                    FROM Teachers t
                    JOIN Positions p ON t.PositionID = p.PositionID
                    LEFT JOIN AccessRoles r ON t.AccessRoleID = r.RoleID
                    ORDER BY t.LastName";

                DataTable dt = DatabaseHelper.GetDataTable(sql);
                List<EmployeeAccessInfo> list = new List<EmployeeAccessInfo>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new EmployeeAccessInfo
                    {
                        TeacherID = (int)row["TeacherID"],
                        FullName = row["FullName"].ToString(),
                        Login = row["Login"] != DBNull.Value ? row["Login"].ToString() : null,
                        PositionName = row["PositionName"].ToString(),
                        RoleID = row["AccessRoleID"] != DBNull.Value ? (int)row["AccessRoleID"] : 3,
                        RoleName = row["RoleName"] != DBNull.Value ? row["RoleName"].ToString() : "Вчитель"
                    });
                }
                listEmployees.ItemsSource = list;
            }
            catch (Exception ex) { MessageBox.Show("Помилка завантаження списку: " + ex.Message); }
        }

        // 3. Вибір співробітника
        private void ListEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listEmployees.SelectedItem == null)
            {
                AccessCard.Visibility = Visibility.Collapsed;
                NoSelectionMsg.Visibility = Visibility.Visible;
                return;
            }

            EmployeeAccessInfo selected = (EmployeeAccessInfo)listEmployees.SelectedItem;

            txtSelectedName.Text = selected.FullName;
            txtSelectedPosition.Text = selected.PositionName;
            txtLogin.Text = selected.Login ?? "";
            txtPassword.Password = "";
            cmbAccessRole.SelectedValue = selected.RoleID;

            if (string.IsNullOrEmpty(selected.Login))
            {
                btnRevoke.Visibility = Visibility.Collapsed;
                btnSave.Content = "✨ Створити доступ";
            }
            else
            {
                btnRevoke.Visibility = Visibility.Visible;
                btnSave.Content = "💾 Оновити дані";
            }

            AccessCard.Visibility = Visibility.Visible;
            NoSelectionMsg.Visibility = Visibility.Collapsed;
        }

        // 4. Збереження змін
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (listEmployees.SelectedItem == null) return;
            EmployeeAccessInfo selected = (EmployeeAccessInfo)listEmployees.SelectedItem;

            string newLogin = txtLogin.Text.Trim();
            string newPass = txtPassword.Password;
            object newRole = cmbAccessRole.SelectedValue;

            if (string.IsNullOrEmpty(newLogin))
            {
                MessageBox.Show("Введіть логін!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(selected.Login) && string.IsNullOrEmpty(newPass))
            {
                MessageBox.Show("Для нового користувача потрібно задати пароль.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string sql = "UPDATE Teachers SET Login = @Login, AccessRoleID = @RoleID";

                // Використовуємо List для динамічного додавання параметрів
                List<SqlParameter> parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Login", newLogin),
                    new SqlParameter("@RoleID", newRole),
                    new SqlParameter("@ID", selected.TeacherID)
                };

                if (!string.IsNullOrEmpty(newPass))
                {
                    sql += ", Password = @Pass";
                    parameters.Add(new SqlParameter("@Pass", AppSession.HashPassword(newPass)));
                }

                sql += " WHERE TeacherID = @ID";

                // Перетворюємо List в Array для нашого Helper
                DatabaseHelper.ExecuteQuery(sql, parameters.ToArray());

                MessageBox.Show("Дані доступу успішно збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadEmployees();
                listEmployees.SelectedItem = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка збереження (можливо, такий логін вже зайнятий): " + ex.Message);
            }
        }

        // 5. Скасування доступу
        private void BtnRevoke_Click(object sender, RoutedEventArgs e)
        {
            if (listEmployees.SelectedItem == null) return;
            EmployeeAccessInfo selected = (EmployeeAccessInfo)listEmployees.SelectedItem;

            if (MessageBox.Show($"Заблокувати доступ для {selected.FullName}?", "Блокування", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    string sql = "UPDATE Teachers SET Login = NULL, Password = NULL, AccessRoleID = 3 WHERE TeacherID = @ID";
                    SqlParameter[] parameters = { new SqlParameter("@ID", selected.TeacherID) };

                    DatabaseHelper.ExecuteQuery(sql, parameters);

                    MessageBox.Show("Доступ скасовано.", "Виконано", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadEmployees();
                    listEmployees.SelectedItem = null;
                }
                catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
            }
        }
    }
}