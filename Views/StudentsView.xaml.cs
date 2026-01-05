using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace SchoolJournalApp.Views
{
    public partial class StudentsView : UserControl
    {
        public StudentsView()
        {
            InitializeComponent();
            LoadClasses();
            // На початку роботи деактивуємо кнопку редагування
            btnEditStudent.IsEnabled = false;
        }

        private void LoadClasses()
        {
            try
            {
                // Просто один рядок!
                DataTable dt = DatabaseHelper.GetDataTable("SELECT ClassID, ClassName FROM Classes");
                cmbClasses.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження класів: " + ex.Message);
            }
        }

        private void CmbClasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbClasses.SelectedValue == null) return;
            ClearStudentDetails();

            try
            {
                string sql = "SELECT StudentID, LastName + ' ' + FirstName + ' ' + ISNULL(MiddleName, '') AS FullName FROM Students WHERE ClassID = @C ORDER BY LastName";

                // Використовуємо параметри для безпеки
                SqlParameter[] parameters = { new SqlParameter("@C", cmbClasses.SelectedValue) };

                DataTable dt = DatabaseHelper.GetDataTable(sql, parameters);
                listStudents.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження учнів: " + ex.Message);
            }
        }

        // НОВИЙ МЕТОД: Очищення деталей картки учня
        private void ClearStudentDetails()
        {
            ProfileCard.Visibility = Visibility.Collapsed;
            // Деактивуємо кнопку редагування
            btnEditStudent.IsEnabled = false;
        }


        private void ListStudents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listStudents.SelectedItem == null)
            {
                ClearStudentDetails();
                return;
            }

            btnEditStudent.IsEnabled = true; // Активуємо кнопку "Редагувати"

            DataRowView row = (DataRowView)listStudents.SelectedItem;
            if (row != null)
            {
                int studentId = (int)row["StudentID"];
                LoadStudentDetails(studentId);
            }
        }

        private void LoadStudentDetails(int id)
        {
            try
            {
                // Створюємо параметр ID один раз для обох запитів
                SqlParameter[] paramId = { new SqlParameter("@ID", id) };

                // --- 1. ЗАВАНТАЖУЄМО ДАНІ УЧНЯ ---
                string sql = @"
            SELECT s.*, c.ClassName, 
                    t.LastName + ' ' + LEFT(t.FirstName, 1) + '.' + ISNULL(LEFT(t.MiddleName, 1) + '.', '') AS HomeroomTeacher
            FROM Students s 
            JOIN Classes c ON s.ClassID = c.ClassID 
            LEFT JOIN Teachers t ON c.HomeroomTeacherID = t.TeacherID
            WHERE s.StudentID = @ID";

                DataTable dtStudent = DatabaseHelper.GetDataTable(sql, paramId);

                if (dtStudent.Rows.Count > 0)
                {
                    DataRow r = dtStudent.Rows[0];

                    string midName = r["MiddleName"] != DBNull.Value ? r["MiddleName"].ToString() : "";
                    txtFullName.Text = $"{r["LastName"]} {r["FirstName"]} {midName}";
                    txtClassBadge.Text = r["ClassName"].ToString() + " Клас";

                    if (r["HomeroomTeacher"] != DBNull.Value)
                        txtHomeroomTeacher.Text = "Класний керівник: " + r["HomeroomTeacher"].ToString();
                    else
                        txtHomeroomTeacher.Text = "Класний керівник: Не призначено";

                    txtBirthDate.Text = r["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(r["DateOfBirth"]).ToString("dd MMMM yyyy") : "-";
                    txtGender.Text = r["Gender"] != DBNull.Value ? r["Gender"].ToString() : "-";
                    txtSchool.Text = r["SchoolName"] != DBNull.Value ? r["SchoolName"].ToString() : "Загальноосвітня школа";

                    string docType = r["DocumentType"] != DBNull.Value ? r["DocumentType"].ToString() : "";
                    string docNum = r["DocumentNumber"] != DBNull.Value ? r["DocumentNumber"].ToString() : "Дані відсутні";
                    txtDocInfo.Text = docType + " №" + docNum;

                    txtEnrollDate.Text = r["EnrollmentDate"] != DBNull.Value ? Convert.ToDateTime(r["EnrollmentDate"]).ToString("dd.MM.yyyy") : "-";
                    txtEnrollReason.Text = r["EnrollmentReason"] != DBNull.Value ? r["EnrollmentReason"].ToString() : "";
                    txtParentContactFile.Text = r["ParentContactPhone"] != DBNull.Value ? r["ParentContactPhone"].ToString() : "-";
                }
                else
                {
                    MessageBox.Show("Не знайдено даних про учня!");
                    return;
                }

                // --- 2. ЗАВАНТАЖУЄМО БАТЬКІВ ---
                // Важливо: Нам треба створити НОВИЙ масив параметрів, оскільки старий міг бути використаний
                SqlParameter[] paramParents = { new SqlParameter("@ID", id) };

                string sqlParents = "SELECT LastName, FirstName, MiddleName, Role, Phone FROM Parents WHERE StudentID = @ID";
                DataTable dtParents = DatabaseHelper.GetDataTable(sqlParents, paramParents);

                List<ParentDisplay> parentsList = new List<ParentDisplay>();
                foreach (DataRow pr in dtParents.Rows)
                {
                    string role = pr["Role"].ToString();
                    string lastName = pr["LastName"].ToString();
                    string firstName = pr["FirstName"].ToString();
                    string middleName = pr["MiddleName"] != DBNull.Value ? pr["MiddleName"].ToString() : "";
                    string phone = pr["Phone"] != DBNull.Value ? pr["Phone"].ToString() : "Не вказано";

                    parentsList.Add(new ParentDisplay
                    {
                        FullName = $"{lastName} {firstName} {middleName}",
                        RoleAndPhone = $"{role} • {phone}"
                    });
                }
                listParents.ItemsSource = parentsList;

                ProfileCard.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("КРИТИЧНА ПОМИЛКА: " + ex.Message);
                ClearStudentDetails();
            }
        }

        public class ParentDisplay
        {
            public string FullName { get; set; }
            public string RoleAndPhone { get; set; }
        }


        // НОВИЙ МЕТОД: Обробник для кнопок "Додати" та "Редагувати"
        private void BtnOpenStudentForm_Click(object sender, RoutedEventArgs e)
        {
            int studentId = 0; // За замовчуванням - новий учень

            // Якщо натиснуто "Редагувати"
            if (sender is Button button && button.Name == "btnEditStudent" && listStudents.SelectedValue != null)
            {
                DataRowView row = (DataRowView)listStudents.SelectedItem;
                if (row != null)
                {
                    studentId = (int)row["StudentID"];
                }
            }

            try
            {
                // Відкриваємо вікно форми
                StudentFormWindow formWindow = new StudentFormWindow(studentId);

                bool? result = formWindow.ShowDialog();

                if (result == true)
                {
                    // Після успішного збереження/видалення оновлюємо дані

                    // 1. Оновлюємо список учнів для поточного класу
                    // (Викликаємо логіку CmbClasses_SelectionChanged, щоб перезавантажити список)
                    CmbClasses_SelectionChanged(cmbClasses, null);

                    // 2. Скидаємо вибір і картку
                    listStudents.SelectedValue = null;
                    ClearStudentDetails();

                    MessageBox.Show("Операція з даними учня успішно завершена!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при відкритті форми учня: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}