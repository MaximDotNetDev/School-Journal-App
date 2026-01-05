using Microsoft.Data.SqlClient;
using SchoolJournalApp.Services;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace SchoolJournalApp.Views
{
    public partial class TeachersView : UserControl
    {
        public TeachersView()
        {
            InitializeComponent();
            LoadTeachersList();
        }

        private void LoadTeachersList()
        {
            try
            {
                string sql = "SELECT TeacherID, LastName + ' ' + FirstName + ' ' + ISNULL(MiddleName,'') AS FullName FROM Teachers ORDER BY LastName";

                DataTable dt = DatabaseHelper.GetDataTable(sql);

                cmbTeachers.ItemsSource = null;
                cmbTeachers.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження списку вчителів: " + ex.Message);
                ClearTeacherDetails();
            }
        }

        // НОВИЙ МЕТОД: Очищення деталей картки вчителя
        private void ClearTeacherDetails()
        {
            TeacherCard.Visibility = Visibility.Collapsed;
            // Змінюємо btnDeleteTeacher на btnEditTeacher
            btnEditTeacher.IsEnabled = false;
            cmbTeachers.SelectedValue = null;
        }

        private void CmbTeachers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTeachers.SelectedValue == null)
            {
                ClearTeacherDetails();
                return;
            }
            // Змінюємо btnDeleteTeacher на btnEditTeacher
            btnEditTeacher.IsEnabled = true;
            int teacherId = (int)cmbTeachers.SelectedValue;
            LoadTeacherDetails(teacherId);
        }
        private void LoadTeacherDetails(int id)
        {
            try
            {
                // 1. ЗАВАНТАЖУЄМО ОСНОВНІ ДАНІ
                // Створюємо параметр прямо тут
                string sql = @"
            SELECT t.*, p.PositionName, q.QualificationName, pt.TitleName
            FROM Teachers t
            JOIN Positions p ON t.PositionID = p.PositionID
            LEFT JOIN Qualifications q ON t.QualificationID = q.QualificationID
            LEFT JOIN PedagogicalTitles pt ON t.PedagogicalTitleID = pt.TitleID
            WHERE t.TeacherID = @ID";

                // ТУТ ВАЖЛИВО: Створюємо new SqlParameter[] прямо у виклику
                DataTable dt = DatabaseHelper.GetDataTable(sql, new SqlParameter[] { new SqlParameter("@ID", id) });

                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];

                    string midName = r["MiddleName"] != DBNull.Value ? r["MiddleName"].ToString() : "";
                    txtName.Text = $"{r["LastName"]} {r["FirstName"]} {midName}";

                    txtPosition.Text = r["PositionName"].ToString();

                    if (r["DateOfBirth"] != DBNull.Value)
                        txtDOB.Text = Convert.ToDateTime(r["DateOfBirth"]).ToString("dd.MM.yyyy");
                    else txtDOB.Text = "-";

                    txtGender.Text = r["Gender"] != DBNull.Value ? r["Gender"].ToString() : "-";
                    txtPhone.Text = r["Phone"] != DBNull.Value ? r["Phone"].ToString() : "Не вказано";
                    txtEmail.Text = r["Email"] != DBNull.Value ? r["Email"].ToString() : "Не вказано";
                    txtCategory.Text = r["QualificationName"] != DBNull.Value ? r["QualificationName"].ToString() : "-";
                    txtTitle.Text = r["TitleName"] != DBNull.Value ? r["TitleName"].ToString() : "-";
                    txtSpecialization.Text = r["Specialization"] != DBNull.Value ? r["Specialization"].ToString() : "-";

                    if (r["Workload"] != DBNull.Value)
                        txtWorkload.Text = r["Workload"].ToString() + " ставки";
                    else
                        txtWorkload.Text = "-";

                    txtEducation.Text = r["EducationInfo"] != DBNull.Value ? r["EducationInfo"].ToString() : "-";
                }
                else
                {
                    ClearTeacherDetails();
                    return;
                }

                // 2. ПЕРЕВІРЯЄМО, ЧИ Є КЛАСНИМ КЕРІВНИКОМ
                string sqlHomeroom = "SELECT ClassName FROM Classes WHERE HomeroomTeacherID = @ID";

                // ЗНОВУ створюємо новий параметр
                DataTable dtHomeroom = DatabaseHelper.GetDataTable(sqlHomeroom, new SqlParameter[] { new SqlParameter("@ID", id) });

                if (dtHomeroom.Rows.Count > 0)
                {
                    string className = dtHomeroom.Rows[0]["ClassName"].ToString();
                    txtHomeroom.Text = $"Класний керівник {className} класу";
                    bdHomeroom.Visibility = Visibility.Visible;
                }
                else
                {
                    bdHomeroom.Visibility = Visibility.Collapsed;
                }

                // 3. ЗАВАНТАЖУЄМО ПРЕДМЕТИ
                string sqlSubj = @"SELECT s.SubjectName, c.ClassName 
                           FROM TeachingAssignments ta
                           JOIN Subjects s ON ta.SubjectID = s.SubjectID
                           JOIN Classes c ON ta.ClassID = c.ClassID
                           WHERE ta.TeacherID = @ID
                           ORDER BY c.ClassName";

                // І ЗНОВУ створюємо новий параметр
                DataTable dtSubj = DatabaseHelper.GetDataTable(sqlSubj, new SqlParameter[] { new SqlParameter("@ID", id) });
                gridSubjects.ItemsSource = dtSubj.DefaultView;

                TeacherCard.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження даних вчителя: " + ex.Message);
                ClearTeacherDetails();
            }
        }
        
        // Новий єдиний обробник для кнопок "Додати" та "Редагувати"
        private void BtnOpenTeacherForm_Click(object sender, RoutedEventArgs e)
        {
            int teacherId = 0; // За замовчуванням - новий вчитель

            if (sender is Button button && button.Name == "btnEditTeacher" && cmbTeachers.SelectedValue != null)
            {
                // Якщо натиснуто "Редагувати"
                teacherId = (int)cmbTeachers.SelectedValue;
            }
            // Якщо натиснуто "Додати Вчителя", teacherId залишається 0.

            try
            {
                // Відкриваємо єдине вікно TeacherFormWindow
                TeacherFormWindow formWindow = new TeacherFormWindow(teacherId);

                bool? result = formWindow.ShowDialog();

                if (result == true)
                {
                    // Після успішного збереження/видалення оновлюємо список
                    LoadTeachersList();

                    // Якщо ми були в режимі редагування і не видалили запис, 
                    // спробуємо оновити картку, вибравши його знову.
                    if (teacherId > 0 && cmbTeachers.SelectedValue != null)
                    {
                        cmbTeachers.SelectedValue = teacherId;
                    }

                    MessageBox.Show("Операція з даними вчителя успішно завершена!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при відкритті форми вчителя: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // НОВИЙ МЕТОД: Логіка видалення вчителя з бази даних
        private void DeleteTeacher(int teacherId, string teacherName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();

                    // ВИКОРИСТАННЯ ТРАНЗАКЦІЇ ДЛЯ ГАРАНТІЇ ЦІЛІСНОСТІ
                    SqlTransaction transaction = con.BeginTransaction();

                    try
                    {
                        // 1. ВИДАЛЕННЯ ЗВ'ЯЗКІВ (TeachingAssignments)
                        string sqlDeleteAssignments = "DELETE FROM TeachingAssignments WHERE TeacherID = @ID";
                        SqlCommand cmdDeleteAssignments = new SqlCommand(sqlDeleteAssignments, con, transaction);
                        cmdDeleteAssignments.Parameters.AddWithValue("@ID", teacherId);
                        cmdDeleteAssignments.ExecuteNonQuery();

                        // 2. СКИДАННЯ HomeroomTeacherID в таблиці Classes
                        string sqlUpdateHomeroom = "UPDATE Classes SET HomeroomTeacherID = NULL WHERE HomeroomTeacherID = @ID";
                        SqlCommand cmdUpdateHomeroom = new SqlCommand(sqlUpdateHomeroom, con, transaction);
                        cmdUpdateHomeroom.Parameters.AddWithValue("@ID", teacherId);
                        cmdUpdateHomeroom.ExecuteNonQuery();

                        // 3. ВИДАЛЕННЯ САМОГО ВЧИТЕЛЯ (Teachers)
                        string sqlDeleteTeacher = "DELETE FROM Teachers WHERE TeacherID = @ID";
                        SqlCommand cmdDeleteTeacher = new SqlCommand(sqlDeleteTeacher, con, transaction);
                        cmdDeleteTeacher.Parameters.AddWithValue("@ID", teacherId);
                        cmdDeleteTeacher.ExecuteNonQuery();

                        // ФІКСУЄМО ТРАНЗАКЦІЮ
                        transaction.Commit();

                        MessageBox.Show($"Вчителя {teacherName} успішно видалено.", "Видалення успішне", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Оновлюємо список вчителів та очищаємо картку
                        LoadTeachersList();
                        ClearTeacherDetails();
                    }
                    catch (Exception ex)
                    {
                        // ВІДКОЧУЄМО ТРАНЗАКЦІЮ У ВИПАДКУ ПОМИЛКИ
                        transaction.Rollback();
                        MessageBox.Show("Помилка при видаленні вчителя: " + ex.Message, "Помилка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка з'єднання з базою даних: " + ex.Message, "Помилка З'єднання", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}