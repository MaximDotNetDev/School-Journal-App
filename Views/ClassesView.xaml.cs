using Microsoft.Data.SqlClient;
using SchoolJournalApp.Services;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace SchoolJournalApp.Views
{
    public partial class ClassesView : UserControl
    {
        private int _currentClassId = 0; // 0 = Новий клас, >0 = Редагування

        public ClassesView()
        {
            InitializeComponent();
            LoadTeachers();
            LoadClasses();
        }

        // --- 1. ЗАВАНТАЖЕННЯ ДАНИХ ---

        private void LoadTeachers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    DataTable dt = new DataTable();
                    // Створюємо структуру таблиці вручну, щоб додати рядок "Не призначено"
                    dt.Columns.Add("TeacherID", typeof(int));
                    dt.Columns.Add("FullName", typeof(string));

                    // 1. Додаємо порожній варіант
                    dt.Rows.Add(0, "Не призначено");

                    // 2. Завантажуємо вчителів з БД
                    string sql = "SELECT TeacherID, LastName + ' ' + FirstName + ' ' + ISNULL(MiddleName,'') AS FullName FROM Teachers ORDER BY LastName";
                    SqlDataAdapter da = new SqlDataAdapter(sql, con);

                    // Заповнюємо тимчасову таблицю і копіюємо в основну
                    DataTable dtTemp = new DataTable();
                    da.Fill(dtTemp);
                    foreach (DataRow row in dtTemp.Rows)
                    {
                        dt.Rows.Add(row["TeacherID"], row["FullName"]);
                    }

                    cmbTeachers.ItemsSource = dt.DefaultView;
                    cmbTeachers.SelectedValue = 0; // Вибираємо "Не призначено" за замовчуванням
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження вчителів: " + ex.Message);
            }
        }

        private void LoadClasses()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    string sql = "SELECT ClassID, ClassName, GradeLevel FROM Classes ORDER BY GradeLevel, ClassName";
                    SqlDataAdapter da = new SqlDataAdapter(sql, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    listClasses.ItemsSource = dt.DefaultView;

                    // Очищаємо інтерфейс, але БЕЗ виклику SelectionChanged, якщо можливо
                    ClearFormUIOnly();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження класів: " + ex.Message);
            }
        }

        // --- 2. ЛОГІКА ВИБОРУ КЛАСУ ---

        private void ListClasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Якщо виділення знято (null), просто ховаємо деталі та очищаємо поля введення
            if (listClasses.SelectedValue == null)
            {
                ClassDetailsCard.Visibility = Visibility.Collapsed;
                ClearInputsOnly();
                return;
            }

            // Якщо клас вибрано - завантажуємо деталі
            if (listClasses.SelectedValue is int classId)
            {
                LoadClassDetails(classId);
            }
        }

        private void LoadClassDetails(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();

                    // --- Деталі класу ---
                    string sqlClass = @"
                        SELECT c.ClassName, c.GradeLevel, 
                               t.LastName + ' ' + t.FirstName + ' ' + ISNULL(t.MiddleName, '') AS HomeroomTeacher
                        FROM Classes c
                        LEFT JOIN Teachers t ON c.HomeroomTeacherID = t.TeacherID
                        WHERE c.ClassID = @ID";

                    SqlCommand cmdClass = new SqlCommand(sqlClass, con);
                    cmdClass.Parameters.AddWithValue("@ID", id);

                    using (SqlDataReader r = cmdClass.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            txtClassHeader.Text = $"{r["ClassName"]} ({r["GradeLevel"]} клас)";
                            txtHomeroomTeacher.Text = r["HomeroomTeacher"] != DBNull.Value
                                                      ? r["HomeroomTeacher"].ToString()
                                                      : "Не призначено";
                        }
                    }

                    // --- Список учнів ---
                    string sqlStudents = @"
                        SELECT StudentID, LastName + ' ' + FirstName + ' ' + ISNULL(MiddleName, '') AS FullName, DateOfBirth
                        FROM Students
                        WHERE ClassID = @ID
                        ORDER BY LastName";

                    SqlDataAdapter da = new SqlDataAdapter(sqlStudents, con);
                    da.SelectCommand.Parameters.AddWithValue("@ID", id);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // Нумерація рядків
                    dt.Columns.Add("Index", typeof(int));
                    int idx = 1;
                    foreach (DataRow row in dt.Rows) row["Index"] = idx++;

                    gridStudentsInClass.ItemsSource = dt.DefaultView;
                    txtStudentCount.Text = $"{dt.Rows.Count} учнів";

                    // Валідація видалення
                    bool hasStudents = dt.Rows.Count > 0;
                    btnDeleteClass.IsEnabled = !hasStudents;
                    btnDeleteClass.ToolTip = hasStudents ? "Клас не можна видалити, поки в ньому є учні." : null;

                    ClassDetailsCard.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка деталей: " + ex.Message);
            }
        }

        // --- 3. ДОДАВАННЯ / РЕДАГУВАННЯ ---

        private void BtnEditClass_Click(object sender, RoutedEventArgs e)
        {
            if (listClasses.SelectedItem == null) return;

            _currentClassId = (int)listClasses.SelectedValue;
            DataRowView row = (DataRowView)listClasses.SelectedItem;

            txtClassName.Text = row["ClassName"].ToString();
            txtGradeLevel.Text = row["GradeLevel"].ToString();

            // Отримуємо ID вчителя для ComboBox
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    object res = new SqlCommand($"SELECT HomeroomTeacherID FROM Classes WHERE ClassID={_currentClassId}", con).ExecuteScalar();
                    cmbTeachers.SelectedValue = (res != DBNull.Value) ? (int)res : 0;
                }
            }
            catch { cmbTeachers.SelectedValue = 0; }

            txtFormTitle.Text = "✏️ Редагувати клас";
            btnSaveClass.Content = "ЗБЕРЕГТИ ЗМІНИ";
            btnCancelEdit.Visibility = Visibility.Visible;
        }

        private void BtnSaveClass_Click(object sender, RoutedEventArgs e)
        {
            string name = txtClassName.Text.Trim();
            if (string.IsNullOrEmpty(name) || !int.TryParse(txtGradeLevel.Text, out int level))
            {
                MessageBox.Show("Введіть коректну назву та числовий рівень!", "Помилка");
                return;
            }

            int teacherId = (int)cmbTeachers.SelectedValue;

            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;

                    if (_currentClassId == 0) // INSERT
                    {
                        cmd.CommandText = "INSERT INTO Classes (ClassName, GradeLevel, HomeroomTeacherID) VALUES (@N, @L, @T)";
                    }
                    else // UPDATE
                    {
                        cmd.CommandText = "UPDATE Classes SET ClassName=@N, GradeLevel=@L, HomeroomTeacherID=@T WHERE ClassID=@ID";
                        cmd.Parameters.AddWithValue("@ID", _currentClassId);
                    }

                    cmd.Parameters.AddWithValue("@N", name);
                    cmd.Parameters.AddWithValue("@L", level);
                    cmd.Parameters.AddWithValue("@T", teacherId == 0 ? DBNull.Value : (object)teacherId);

                    cmd.ExecuteNonQuery();
                }

                LoadClasses(); // Перезавантажити список
                MessageBox.Show("Дані збережено!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка збереження: " + ex.Message);
            }
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ClearFormUIOnly();
        }

        // --- ДОПОМІЖНІ МЕТОДИ ОЧИЩЕННЯ ---

        // Очищає все, включаючи виділення списку
        private void ClearFormUIOnly()
        {
            listClasses.SelectedValue = null; // Це викличе SelectionChanged, який викличе ClearInputsOnly
        }

        // Очищає тільки поля вводу (викликається при SelectionChanged = null)
        private void ClearInputsOnly()
        {
            _currentClassId = 0;
            txtFormTitle.Text = "➕ Створити новий клас";
            btnSaveClass.Content = "ЗБЕРЕГТИ КЛАС";
            btnCancelEdit.Visibility = Visibility.Collapsed;

            txtClassName.Text = "";
            txtGradeLevel.Text = "";
            cmbTeachers.SelectedValue = 0;
        }

        // --- ВИДАЛЕННЯ ---
        private void BtnDeleteClass_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Видалити цей клас?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                    {
                        con.Open();
                        // Спочатку видаляємо навантаження вчителів для цього класу
                        new SqlCommand($"DELETE FROM TeachingAssignments WHERE ClassID={(int)listClasses.SelectedValue}", con).ExecuteNonQuery();
                        // Видаляємо клас
                        new SqlCommand($"DELETE FROM Classes WHERE ClassID={(int)listClasses.SelectedValue}", con).ExecuteNonQuery();
                    }
                    LoadClasses();
                }
                catch (Exception ex) { MessageBox.Show("Помилка видалення: " + ex.Message); }
            }
        }

        private void BtnManageStudents_Click(object sender, RoutedEventArgs e)
        {
            if (listClasses.SelectedValue == null) return;
            // Відкрити вікно керування учнями
            var win = new ClassStudentsWindow((int)listClasses.SelectedValue, txtClassName.Text);
            if (win.ShowDialog() == true) LoadClassDetails((int)listClasses.SelectedValue);
        }
    }
}