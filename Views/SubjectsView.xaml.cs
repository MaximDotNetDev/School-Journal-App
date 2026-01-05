using System;
using System.Data;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using SchoolJournalApp.Services;

namespace SchoolJournalApp.Views
{
    public partial class SubjectsView : UserControl
    {
        public SubjectsView()
        {
            InitializeComponent();
            LoadSubjects();
        }

        // 1. Завантаження списку предметів
        private void LoadSubjects()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    string sql = "SELECT SubjectID, SubjectName FROM Subjects ORDER BY SubjectName";
                    SqlDataAdapter da = new SqlDataAdapter(sql, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    cmbSubjects.ItemsSource = dt.DefaultView;
                }
            }
            catch { }
        }

        // 2. Вибір предмета
        private void CmbSubjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSubjects.SelectedValue == null) return;
            int subjectId = (int)cmbSubjects.SelectedValue;
            LoadSubjectDetails(subjectId);
        }

        // 3. Завантаження деталей
        private void LoadSubjectDetails(int id)
        {
            try
            {
                // Встановлюємо назву в шапку (беремо з ComboBox для швидкості)
                DataRowView selectedRow = (DataRowView)cmbSubjects.SelectedItem;
                txtSubjectName.Text = selectedRow["SubjectName"].ToString();

                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();

                    // === ОНОВЛЕНИЙ SQL ЗАПИТ ===
                    // Додано JOIN до Teachers двічі: для вчителя-предметника (t_subject) 
                    // та для класного керівника (t_homeroom) через таблицю Classes.
                    string sql = @"
                        SELECT 
                            c.ClassName,
                            t_subject.LastName + ' ' + t_subject.FirstName + ' ' + ISNULL(t_subject.MiddleName, '') AS SubjectTeacher,
                            t_homeroom.LastName + ' ' + t_homeroom.FirstName + ' ' + ISNULL(t_homeroom.MiddleName, '') AS HomeroomTeacher
                        FROM TeachingAssignments ta
                        JOIN Classes c ON ta.ClassID = c.ClassID
                        JOIN Teachers t_subject ON ta.TeacherID = t_subject.TeacherID -- Вчитель, що викладає предмет
                        LEFT JOIN Teachers t_homeroom ON c.HomeroomTeacherID = t_homeroom.TeacherID -- Класний керівник
                        WHERE ta.SubjectID = @ID
                        ORDER BY c.GradeLevel";

                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@ID", id);

                    DataTable dt = new DataTable();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);

                    // Заповнюємо таблицю
                    gridTeaching.ItemsSource = dt.DefaultView;

                    // Рахуємо статистику
                    txtClassesCount.Text = dt.Rows.Count.ToString();

                    // Рахуємо унікальних вчителів
                    // Важливо: використовуємо нове ім'я колонки SubjectTeacher
                    DataView view = new DataView(dt);
                    DataTable distinctTeachers = view.ToTable(true, "SubjectTeacher");
                    txtTeachersCount.Text = distinctTeachers.Rows.Count.ToString();

                    SubjectCard.Visibility = System.Windows.Visibility.Visible;

                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Помилка завантаження деталей предмету: " + ex.Message);
            }
        }



    }
}