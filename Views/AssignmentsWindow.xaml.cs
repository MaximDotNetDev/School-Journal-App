using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace SchoolJournalApp
{
    public partial class AssignmentsWindow : Window
    {
        private readonly int _teacherId;

        public AssignmentsWindow(int teacherId, string teacherName)
        {
            InitializeComponent();
            _teacherId = teacherId;
            txtTitle.Text = $"📚 Навантаження для Вчителя: {teacherName}";

            LoadDefaultComboBoxData();
            LoadTeacherAssignments();
        }

        // 1. Завантаження довідників
        private void LoadDefaultComboBoxData()
        {
            try
            {
                // Предмети
                DataTable dtSubj = DatabaseHelper.GetDataTable("SELECT SubjectID, SubjectName FROM Subjects ORDER BY SubjectName");
                cmbSubjects.ItemsSource = dtSubj.DefaultView;

                // Класи
                DataTable dtClasses = DatabaseHelper.GetDataTable("SELECT ClassID, ClassName FROM Classes ORDER BY GradeLevel, ClassName");
                cmbClasses.ItemsSource = dtClasses.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження довідників: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 2. Завантаження навантаження
        private void LoadTeacherAssignments()
        {
            try
            {
                string sqlSubj = @"SELECT ta.AssignmentID, s.SubjectName, c.ClassName
                                   FROM TeachingAssignments ta
                                   JOIN Subjects s ON ta.SubjectID = s.SubjectID
                                   JOIN Classes c ON ta.ClassID = c.ClassID
                                   WHERE ta.TeacherID = @ID
                                   ORDER BY c.ClassName, s.SubjectName";

                SqlParameter[] parameters = { new SqlParameter("@ID", _teacherId) };
                DataTable dtSubj = DatabaseHelper.GetDataTable(sqlSubj, parameters);

                gridAssignments.ItemsSource = dtSubj.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження навантаження: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 3. ДОДАВАННЯ призначення
        private void BtnAddAssignment_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSubjects.SelectedValue == null || cmbClasses.SelectedValue == null)
            {
                MessageBox.Show("Оберіть Предмет і Клас.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int subjectId = (int)cmbSubjects.SelectedValue;
            int classId = (int)cmbClasses.SelectedValue;

            try
            {
                // А. Перевірка на дублікат
                string sqlCheck = "SELECT COUNT(*) FROM TeachingAssignments WHERE TeacherID = @TID AND SubjectID = @SID AND ClassID = @CID";
                SqlParameter[] paramsCheck = {
                    new SqlParameter("@TID", _teacherId),
                    new SqlParameter("@SID", subjectId),
                    new SqlParameter("@CID", classId)
                };

                DataTable dtCheck = DatabaseHelper.GetDataTable(sqlCheck, paramsCheck);
                if (dtCheck.Rows.Count > 0 && (int)dtCheck.Rows[0][0] > 0)
                {
                    MessageBox.Show("Це призначення вже існує.", "Дублікат", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Б. Додавання (INSERT)
                string sqlInsert = "INSERT INTO TeachingAssignments (TeacherID, SubjectID, ClassID) VALUES (@TID, @SID, @CID)";
                // Створюємо нові параметри, бо попередні вже використані
                SqlParameter[] paramsInsert = {
                    new SqlParameter("@TID", _teacherId),
                    new SqlParameter("@SID", subjectId),
                    new SqlParameter("@CID", classId)
                };

                DatabaseHelper.ExecuteQuery(sqlInsert, paramsInsert);

                LoadTeacherAssignments();
                this.DialogResult = true; // Сповіщаємо про зміни
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка додавання: " + ex.Message, "Помилка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 4. ВИДАЛЕННЯ призначення
        private void BtnDeleteAssignment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int assignmentId)
            {
                if (MessageBox.Show("Видалити це призначення?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        string sqlDelete = "DELETE FROM TeachingAssignments WHERE AssignmentID = @ID"; // У вас в базі AssignmentID, а не TeachingAssignmentID
                        SqlParameter[] parameters = { new SqlParameter("@ID", assignmentId) };

                        DatabaseHelper.ExecuteQuery(sqlDelete, parameters);

                        LoadTeacherAssignments();
                        this.DialogResult = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Помилка видалення: " + ex.Message, "Помилка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}