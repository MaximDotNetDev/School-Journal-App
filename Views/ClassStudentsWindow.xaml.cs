using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SchoolJournalApp.Services;
using SchoolJournalApp.Views;

namespace SchoolJournalApp
{
    public partial class ClassStudentsWindow : Window
    {
        private int _classId;
        // Використовуємо ObservableCollection для автоматичного оновлення XAML
        public ObservableCollection<StudentDisplay> CurrentStudents { get; set; } = new ObservableCollection<StudentDisplay>();
        public ObservableCollection<StudentDisplay> AvailableStudents { get; set; } = new ObservableCollection<StudentDisplay>();

        // Внутрішній клас для зручного відображення (додано GroupName)
        public class StudentDisplay
        {
            public int StudentID { get; set; }
            public string FullName { get; set; }
            public string GroupName { get; set; } // Нове поле для назви групи/класу
            public int? ClassID { get; set; }

            public override string ToString() => FullName;
        }

        public ClassStudentsWindow(int classId, string className)
        {
            InitializeComponent();
            _classId = classId;
            txtTitle.Text = $"⚙️ Керування учнями у класі: {className}";

            // Встановлюємо DataContext для прив'язки колекцій до ListBox'ів (через CollectionViewSource)
            this.DataContext = this;

            LoadData();
        }

        private void LoadData()
        {
            CurrentStudents.Clear();
            AvailableStudents.Clear();

            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();

                    // --- 1. УЧНІ У ПОТОЧНОМУ КЛАСІ ---
                    string sqlCurrent = "SELECT StudentID, LastName + ' ' + FirstName + ' ' + ISNULL(MiddleName, '') AS FullName FROM Students WHERE ClassID = @ID ORDER BY LastName";
                    SqlCommand cmdCurrent = new SqlCommand(sqlCurrent, con);
                    cmdCurrent.Parameters.AddWithValue("@ID", _classId);

                    using (SqlDataReader r = cmdCurrent.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            CurrentStudents.Add(new StudentDisplay
                            {
                                StudentID = (int)r["StudentID"],
                                FullName = r["FullName"].ToString(),
                                ClassID = _classId,
                                GroupName = "Поточний клас"
                            });
                        }
                    }

                    // --- 2. ДОСТУПНІ УЧНІ (З ІНФОРМАЦІЄЮ ПРО ПОТОЧНИЙ КЛАС) ---
                    // Вибираємо всіх учнів, які не належать до поточного класу
                    string sqlAvailable = @"
                        SELECT s.StudentID, s.LastName + ' ' + s.FirstName + ' ' + ISNULL(s.MiddleName, '') AS FullName, 
                               s.ClassID, c.ClassName
                        FROM Students s
                        LEFT JOIN Classes c ON s.ClassID = c.ClassID
                        WHERE s.ClassID != @CurrentID OR s.ClassID IS NULL OR s.ClassID = 0 
                        ORDER BY c.GradeLevel, c.ClassName, s.LastName";

                    SqlCommand cmdAvailable = new SqlCommand(sqlAvailable, con);
                    cmdAvailable.Parameters.AddWithValue("@CurrentID", _classId);

                    using (SqlDataReader r = cmdAvailable.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int studentId = (int)r["StudentID"];
                            int? studentClassId = r["ClassID"] != DBNull.Value ? (int?)r["ClassID"] : null;
                            string className = r["ClassName"] != DBNull.Value ? r["ClassName"].ToString() : null;

                            // Групування: якщо ClassID = NULL/0, використовуємо "Учні без класу"
                            string groupName = (studentClassId == null || studentClassId == 0)
                                ? "Учні без класу"
                                : $"Клас: {className}";

                            // Фільтруємо на випадок, якщо запит випадково повернув учнів поточного класу
                            if (studentClassId != _classId)
                            {
                                AvailableStudents.Add(new StudentDisplay
                                {
                                    StudentID = studentId,
                                    FullName = r["FullName"].ToString(),
                                    ClassID = studentClassId,
                                    GroupName = groupName
                                });
                            }
                        }
                    }

                    // Присвоєння джерел даних ListBox'ам
                    listCurrentStudents.ItemsSource = CurrentStudents;
                    // AvailableStudents вже прив'язано через DataContext і CollectionViewSource
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження даних учнів: " + ex.Message, "Помилка");
            }
        }

        // --- Логіка переміщення ---

        // Додавання учнів до класу (Переміщення →)
        private void BtnAddStudents_Click(object sender, RoutedEventArgs e)
        {
            // Копіюємо вибрані елементи, щоб уникнути зміни колекції під час ітерації
            var selected = listAvailableStudents.SelectedItems.Cast<StudentDisplay>().ToList();
            if (selected.Count == 0) return;

            foreach (var student in selected)
            {
                // Видаляємо зі списку доступних
                AvailableStudents.Remove(student);

                // Змінюємо групу та клас для нового списку
                student.GroupName = "Поточний клас";
                student.ClassID = _classId;

                // Додаємо до списку поточних
                CurrentStudents.Add(student);
            }

            // Примусове оновлення (перегрупування) ListBox'ів після змін
            RefreshCollections();
        }

        // Видалення учнів з класу (Переміщення ←)
        private void BtnRemoveStudents_Click(object sender, RoutedEventArgs e)
        {
            var selected = listCurrentStudents.SelectedItems.Cast<StudentDisplay>().ToList();
            if (selected.Count == 0) return;

            foreach (var student in selected)
            {
                // Видаляємо зі списку поточних
                CurrentStudents.Remove(student);

                // Змінюємо групу та клас для нового списку: ClassID = NULL
                student.GroupName = "Учні без класу";
                student.ClassID = null;

                // Додаємо до списку доступних
                AvailableStudents.Add(student);
            }

            // Примусове оновлення (перегрупування) ListBox'ів після змін
            RefreshCollections();
        }

        private void RefreshCollections()
        {
            // Оскільки ObservableCollection використовується для прямої маніпуляції,
            // просто викликаємо оновлення для CollectionViewSource для групування
            var viewSource = this.FindResource("GroupedAvailableStudents") as ICollectionView;
            if (viewSource != null)
            {
                viewSource.Refresh();
            }

            // Сортування поточного списку
            var sortedCurrent = CurrentStudents.OrderBy(s => s.FullName).ToList();
            listCurrentStudents.ItemsSource = null;
            listCurrentStudents.ItemsSource = sortedCurrent;
        }


        // --- Збереження змін ---
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Отримуємо список ID, які мають бути у фінальному класі
                var finalClassStudents = listCurrentStudents.ItemsSource.Cast<StudentDisplay>()
                    .Select(s => s.StudentID).ToList();

                // Отримуємо початковий список ID (ті, що були завантажені на початку)
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    string sqlInitial = "SELECT StudentID FROM Students WHERE ClassID = @ID";
                    SqlCommand cmdInitial = new SqlCommand(sqlInitial, con);
                    cmdInitial.Parameters.AddWithValue("@ID", _classId);

                    var initialStudents = new List<int>();
                    using (SqlDataReader r = cmdInitial.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            initialStudents.Add((int)r["StudentID"]);
                        }
                    }

                    // --- Визначення змін ---
                    // Учні, яких треба додати до класу ( ClassID = @CurrentID )
                    var studentsToAssign = finalClassStudents.Except(initialStudents).ToList();

                    // Учні, яких треба зняти з класу ( ClassID = NULL )
                    var studentsToUnassign = initialStudents.Except(finalClassStudents).ToList();

                    if (studentsToAssign.Count == 0 && studentsToUnassign.Count == 0)
                    {
                        this.DialogResult = true;
                        this.Close();
                        return;
                    }

                    // --- Виконання транзакції ---
                    SqlTransaction transaction = con.BeginTransaction();
                    try
                    {
                        // 1. ПРИЗНАЧЕННЯ
                        if (studentsToAssign.Any())
                        {
                            string sqlUpdate = $"UPDATE Students SET ClassID = @NewClassID WHERE StudentID IN ({string.Join(",", studentsToAssign)})";
                            SqlCommand cmd = new SqlCommand(sqlUpdate, con, transaction);
                            cmd.Parameters.AddWithValue("@NewClassID", _classId);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. ЗНЯТТЯ
                        if (studentsToUnassign.Any())
                        {
                            string sqlUpdate = $"UPDATE Students SET ClassID = NULL WHERE StudentID IN ({string.Join(",", studentsToUnassign)})";
                            SqlCommand cmd = new SqlCommand(sqlUpdate, con, transaction);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        this.DialogResult = true;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Помилка при оновленні бази даних: " + ex.Message, "Помилка SQL");
                        this.DialogResult = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Непередбачена помилка: " + ex.Message, "Помилка");
                this.DialogResult = false;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}