using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using SchoolJournalApp.Services;

namespace SchoolJournalApp.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            UpdateUserInfo();
            LoadStats();
            LoadAnnouncements();
        }

        private void UpdateUserInfo()
        {
            if (!string.IsNullOrEmpty(AppSession.CurrentUserName))
            {
                string rolePrefix = (AppSession.CurrentRoleID == 1) ? "Адміністратор" :
                                    (AppSession.CurrentRoleID == 2) ? "Директор" : "Вчитель";

                txtCurrentUser.Text = $"{rolePrefix} {AppSession.CurrentUserName}";

                if (AppSession.CurrentUserName.Length > 0)
                {
                    txtUserInitial.Text = AppSession.CurrentUserName.Substring(0, 1).ToUpper();
                }
            }
            else
            {
                txtCurrentUser.Text = "Гість";
                txtUserInitial.Text = "Г";
            }
        }

        // 1. Статистика (Оптимізовано через Helper)
        // ... (інші методи) ...

        private void LoadStats()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd;

                    // АДМІНІСТРАТОРИ (RoleID 1 або 2) - Бачать ВСЕ
                    if (AppSession.CurrentRoleID == 1 || AppSession.CurrentRoleID == 2)
                    {
                        // 1. Всього учнів
                        cmd = new SqlCommand("SELECT COUNT(*) FROM Students", con);
                        txtStudentCount.Text = cmd.ExecuteScalar()?.ToString() ?? "0";

                        // 2. Всього класів
                        cmd = new SqlCommand("SELECT COUNT(*) FROM Classes", con);
                        txtClassCount.Text = cmd.ExecuteScalar()?.ToString() ?? "0";

                        // 3. Всього уроків сьогодні
                        cmd = new SqlCommand("SELECT COUNT(*) FROM Lessons WHERE LessonDate = CAST(GETDATE() AS DATE)", con);
                        txtLessonCount.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }
                    // ВЧИТЕЛЬ (RoleID 3) - Бачить ТІЛЬКИ СВОЄ
                    else
                    {
                        int teacherId = AppSession.CurrentUserId;

                        // 1. Учнів (Можна показувати скільки учнів він навчає, або всіх у школі. 
                        // Залишимо "Учнів у школі", як загальну інформацію, або порахуємо унікальних учнів вчителя)
                        // Варіант: Унікальні учні, яких вчить цей вчитель:
                        string sqlStud = @"SELECT COUNT(DISTINCT s.StudentID) 
                                   FROM Students s
                                   JOIN TeachingAssignments ta ON s.ClassID = ta.ClassID
                                   WHERE ta.TeacherID = @TID";
                        cmd = new SqlCommand(sqlStud, con);
                        cmd.Parameters.AddWithValue("@TID", teacherId);
                        txtStudentCount.Text = cmd.ExecuteScalar()?.ToString() ?? "0";

                        // 2. Активних класів (Тільки ті, де він викладає)
                        string sqlClass = "SELECT COUNT(DISTINCT ClassID) FROM TeachingAssignments WHERE TeacherID = @TID";
                        cmd = new SqlCommand(sqlClass, con);
                        cmd.Parameters.AddWithValue("@TID", teacherId);
                        txtClassCount.Text = cmd.ExecuteScalar()?.ToString() ?? "0";

                        // 3. Уроків сьогодні (Тільки його уроки)
                        string sqlLess = @"SELECT COUNT(*) 
                                   FROM Lessons l
                                   JOIN TeachingAssignments ta ON l.AssignmentID = ta.AssignmentID
                                   WHERE ta.TeacherID = @TID AND l.LessonDate = CAST(GETDATE() AS DATE)";
                        cmd = new SqlCommand(sqlLess, con);
                        cmd.Parameters.AddWithValue("@TID", teacherId);
                        txtLessonCount.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }
                }
            }
            catch (Exception ex)
            {
                // Можна розкоментувати для налагодження
                // MessageBox.Show("Помилка статистики: " + ex.Message);
            }
        }

        // 2. Завантаження Оголошень (Оптимізовано)
        private void LoadAnnouncements()
        {
            List<Announcement> items = new List<Announcement>();
            try
            {
                DataTable dt = DatabaseHelper.GetDataTable("SELECT AnnouncementID, Content FROM Announcements ORDER BY AnnouncementID DESC");

                // Перетворюємо рядки таблиці в об'єкти
                foreach (DataRow row in dt.Rows)
                {
                    items.Add(new Announcement
                    {
                        ID = (int)row["AnnouncementID"],
                        Content = "• " + row["Content"].ToString()
                    });
                }

                listAnnouncements.ItemsSource = items;
            }
            catch (Exception ex) { MessageBox.Show("Помилка оголошень: " + ex.Message); }
        }

        // 3. Відкриття вікна додавання
        private void BtnAddAnnouncement_Click(object sender, RoutedEventArgs e)
        {
            txtNewAnnouncement.Text = "";
            PopupAddAnnouncement.IsOpen = true;
        }

        // 4. Збереження в базу (Оптимізовано - INSERT)
        private void BtnSaveAnnouncement_Click(object sender, RoutedEventArgs e)
        {
            string text = txtNewAnnouncement.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                string sql = "INSERT INTO Announcements (Content) VALUES (@Txt)";
                SqlParameter[] parameters = { new SqlParameter("@Txt", text) };

                // Виконуємо запит через Helper
                DatabaseHelper.ExecuteQuery(sql, parameters);

                PopupAddAnnouncement.IsOpen = false;
                LoadAnnouncements(); // Оновлюємо список
            }
            catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
        }

        // 5. Закриття вікна
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            PopupAddAnnouncement.IsOpen = false;
        }

        // 6. Видалення оголошення (Оптимізовано - DELETE)
        private void BtnDeleteAnnouncement_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Видалити це оголошення?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.No) return;

                try
                {
                    string sql = "DELETE FROM Announcements WHERE AnnouncementID = @ID";
                    SqlParameter[] parameters = { new SqlParameter("@ID", id) };

                    // Виконуємо запит через Helper
                    DatabaseHelper.ExecuteQuery(sql, parameters);

                    LoadAnnouncements();
                }
                catch { }
            }
        }

        // ... ваші існуючі методи ...

        // 1. Обробник натискання на блок
        private void BtnLessonsToday_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoadTodayLessonsDetails();
            OverlayLessons.Visibility = Visibility.Visible;
            PopupLessons.Visibility = Visibility.Visible;
        }

        // 2. Обробник закриття
        private void BtnCloseLessons_Click(object sender, RoutedEventArgs e)
        {
            OverlayLessons.Visibility = Visibility.Collapsed;
            PopupLessons.Visibility = Visibility.Collapsed;
        }

        // 3. Завантаження деталей уроків
        private void LoadTodayLessonsDetails()
        {
            List<DailyLesson> lessons = new List<DailyLesson>();
            try
            {
                // Якщо це Адмін - показуємо заглушку або всі уроки. 
                // Якщо Вчитель - показуємо його розклад.

                string sql = "";

                if (AppSession.CurrentRoleID == 3) // Вчитель
                {
                    // Спробуємо взяти номер уроку з FixedSchedule, якщо він там є
                    sql = @"
                        SELECT 
                            COALESCE(fs.LessonNumber, 0) AS LessonNum,
                            s.SubjectName, 
                            c.ClassName, 
                            l.LessonTopic
                        FROM Lessons l
                        JOIN TeachingAssignments ta ON l.AssignmentID = ta.AssignmentID
                        JOIN Subjects s ON ta.SubjectID = s.SubjectID
                        JOIN Classes c ON ta.ClassID = c.ClassID
                        LEFT JOIN FixedSchedule fs ON ta.AssignmentID = fs.AssignmentID 
                                                  AND fs.DayOfWeek = DATEPART(WEEKDAY, GETDATE())
                        WHERE ta.TeacherID = @TID 
                          AND l.LessonDate = CAST(GETDATE() AS DATE)
                        ORDER BY LessonNum, c.ClassName";
                }
                else
                {
                    // Для адмінів покажемо просто список всіх уроків по школі на сьогодні
                    sql = @"
                        SELECT 
                            0 AS LessonNum,
                            s.SubjectName, 
                            c.ClassName, 
                            l.LessonTopic
                        FROM Lessons l
                        JOIN TeachingAssignments ta ON l.AssignmentID = ta.AssignmentID
                        JOIN Subjects s ON ta.SubjectID = s.SubjectID
                        JOIN Classes c ON ta.ClassID = c.ClassID
                        WHERE l.LessonDate = CAST(GETDATE() AS DATE)
                        ORDER BY c.ClassName";
                }

                SqlParameter[] parameters = { new SqlParameter("@TID", AppSession.CurrentUserId) };
                DataTable dt = DatabaseHelper.GetDataTable(sql, parameters);

                int counter = 1;
                foreach (DataRow row in dt.Rows)
                {
                    int num = (int)row["LessonNum"];
                    // Якщо в розкладі немає номера, ставимо просто порядковий номер
                    string displayNum = num > 0 ? num.ToString() : counter.ToString();

                    lessons.Add(new DailyLesson
                    {
                        LessonNumber = displayNum,
                        SubjectName = row["SubjectName"].ToString(),
                        ClassName = row["ClassName"].ToString() + " клас",
                        Topic = row["LessonTopic"] != DBNull.Value ? row["LessonTopic"].ToString() : "Тема не вказана"
                    });
                    counter++;
                }

                listTodayLessons.ItemsSource = lessons;

                // Показуємо текст "немає уроків", якщо список порожній
                if (lessons.Count == 0)
                    txtNoLessons.Visibility = Visibility.Visible;
                else
                    txtNoLessons.Visibility = Visibility.Collapsed;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити деталі уроків: " + ex.Message);
            }
        }
    }

    public static class DatabaseHelper
    {
        // =========================================================
        // АСИНХРОННІ МЕТОДИ (Використовуйте їх, щоб програма не зависала)
        // =========================================================

        // 1. Асинхронне отримання таблиці (SELECT)
        public static async Task<DataTable> GetDataTableAsync(string query, SqlParameter[] parameters = null)
        {
            // Запускаємо роботу в окремому потоці
            return await Task.Run(() =>
            {
                return GetDataTable(query, parameters);
            });
        }

        // 2. Асинхронне виконання команди (INSERT, UPDATE, DELETE)
        public static async Task ExecuteQueryAsync(string query, SqlParameter[] parameters = null)
        {
            // Запускаємо роботу в окремому потоці
            await Task.Run(() =>
            {
                ExecuteQuery(query, parameters);
            });
        }

        // =========================================================
        // СИНХРОННІ МЕТОДИ (Старі, залишаємо для сумісності)
        // =========================================================

        public static DataTable GetDataTable(string query, SqlParameter[] parameters = null)
        {
            // Створюємо нове з'єднання для кожного запиту (це безпечно для потоків)
            using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    if (parameters != null)
                    {
                        // Копіюємо параметри, щоб уникнути конфліктів, якщо масив використовується повторно
                        foreach (var p in parameters)
                        {
                            // Клонуємо параметр, бо SqlParameter не може належати двом командам одночасно
                            cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value ?? DBNull.Value));
                        }
                    }

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static void ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    if (parameters != null)
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value ?? DBNull.Value));
                        }
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }



}

    public class Announcement
    {
        public int ID { get; set; }
        public string Content { get; set; }
    }



// Допоміжний клас для відображення
public class DailyLesson
{
    public string LessonNumber { get; set; }
    public string SubjectName { get; set; }
    public string ClassName { get; set; }
    public string Topic { get; set; }
}
