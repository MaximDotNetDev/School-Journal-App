using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using SchoolJournalApp.Services;
using SchoolJournalApp.Views;

namespace SchoolJournalApp
{
    public partial class StudentFormWindow : Window
    {
        private int _studentId;

        public StudentFormWindow(int studentId = 0)
        {
            InitializeComponent();
            _studentId = studentId;
            LoadClasses();

            if (_studentId > 0)
            {
                LoadStudentData(_studentId);
                txtTitle.Text = "✏️ Редагування особової справи учня";
                btnSave.Content = "Зберегти Зміни";
                btnDelete.Visibility = Visibility.Visible;
            }
            else
            {
                txtTitle.Text = "➕ Додавання нового учня";
                btnSave.Content = "Зберегти та Додати";
                btnDelete.Visibility = Visibility.Collapsed;
                cmbGender.SelectedIndex = 2; // "-"
                dpEnrollmentDate.SelectedDate = DateTime.Today;
            }
        }

        private void LoadClasses()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    SqlDataAdapter daClasses = new SqlDataAdapter("SELECT ClassID, ClassName FROM Classes ORDER BY GradeLevel", con);
                    DataTable dtClasses = new DataTable();
                    daClasses.Fill(dtClasses);
                    cmbClasses.ItemsSource = dtClasses.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження класів: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ОНОВЛЕНИЙ МЕТОД: тепер він викликає LoadParents
        private void LoadStudentData(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    string sql = "SELECT * FROM Students WHERE StudentID = @ID";
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@ID", id);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            // ... (існуючий код завантаження полів ПІБ, дати народження, класу, статі, документів, зарахування) ...
                            txtLastName.Text = r["LastName"].ToString();
                            txtFirstName.Text = r["FirstName"].ToString();
                            txtMiddleName.Text = r["MiddleName"] != DBNull.Value ? r["MiddleName"].ToString() : "";

                            dpDOB.SelectedDate = r["DateOfBirth"] != DBNull.Value ? (DateTime)r["DateOfBirth"] : (DateTime?)null;

                            if (r["ClassID"] != DBNull.Value) cmbClasses.SelectedValue = (int)r["ClassID"];

                            string gender = r["Gender"] != DBNull.Value ? r["Gender"].ToString() : "-";
                            foreach (ComboBoxItem item in cmbGender.Items)
                            {
                                if (item.Content.ToString() == gender)
                                {
                                    cmbGender.SelectedItem = item;
                                    break;
                                }
                            }

                            txtSchoolName.Text = r["SchoolName"] != DBNull.Value ? r["SchoolName"].ToString() : "";
                            txtDocumentType.Text = r["DocumentType"] != DBNull.Value ? r["DocumentType"].ToString() : "";
                            txtDocumentNumber.Text = r["DocumentNumber"] != DBNull.Value ? r["DocumentNumber"].ToString() : "";

                            dpEnrollmentDate.SelectedDate = r["EnrollmentDate"] != DBNull.Value ? (DateTime)r["EnrollmentDate"] : (DateTime?)null;
                            txtEnrollmentReason.Text = r["EnrollmentReason"] != DBNull.Value ? r["EnrollmentReason"].ToString() : "";

                            txtParentContactPhone.Text = r["ParentContactPhone"] != DBNull.Value ? r["ParentContactPhone"].ToString() : "";
                        }
                        else
                        {
                            MessageBox.Show("Дані учня не знайдено.");
                            this.DialogResult = false;
                            this.Close();
                        }
                    }

                    // НОВИЙ ВИКЛИК: Завантажуємо дані батьків у DataGrid
                    LoadParents(id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження даних учня: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = false;
                this.Close();
            }
        }

        // НОВИЙ МЕТОД: Завантаження списку батьків для відображення
        private void LoadParents(int studentId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();

                    string sqlParents = @"SELECT 
                                            Role, 
                                            LastName + ' ' + FirstName + ' ' + ISNULL(MiddleName, '') AS FullName, 
                                            Phone 
                                          FROM Parents 
                                          WHERE StudentID = @ID";

                    SqlDataAdapter daP = new SqlDataAdapter(sqlParents, con);
                    daP.SelectCommand.Parameters.AddWithValue("@ID", studentId);
                    DataTable dtParents = new DataTable();
                    daP.Fill(dtParents);

                    gridParents.ItemsSource = dtParents.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження списку батьків: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // НОВИЙ МЕТОД: Обробник для керування списком батьків
        private void BtnManageParents_Click(object sender, RoutedEventArgs e)
        {
            if (_studentId == 0)
            {
                MessageBox.Show("Будь ласка, спочатку збережіть основного учня, щоб керувати його батьками/опікунами. Натисніть 'Зберегти та Додати'.", "Операція недоступна", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string studentName = $"{txtLastName.Text} {txtFirstName.Text}";

                // !!! ТУТ ПОТРІБНО СТВОРИТИ ВІКНО ParentsWindow !!!
                // Приклад:
                // ParentsWindow parentsWindow = new ParentsWindow(_studentId, studentName);
                // bool? result = parentsWindow.ShowDialog();

                // Тимчасова заглушка:
                MessageBoxResult mbResult = MessageBox.Show($"Тут має відкритися нове вікно для керування батьками для учня: {studentName} (ID: {_studentId}). Вам потрібно створити клас ParentsWindow.",
                                                          "TODO: Створити ParentsWindow",
                                                          MessageBoxButton.OK,
                                                          MessageBoxImage.Information);

                // Після успішного редагування/додавання в ParentsWindow:
                // if (result == true)
                // {
                //     LoadParents(_studentId); // Оновлюємо DataGrid у цій формі
                // }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при відкритті форми керування батьками: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (SaveOrUpdateStudentData())
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private bool SaveOrUpdateStudentData()
        {
            // 1. Валідація
            if (string.IsNullOrWhiteSpace(txtLastName.Text) || string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                MessageBox.Show("Будь ласка, введіть Прізвище та Ім'я.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (dpDOB.SelectedDate == null)
            {
                MessageBox.Show("Будь ласка, оберіть дату народження.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbClasses.SelectedValue == null)
            {
                MessageBox.Show("Будь ласка, оберіть клас.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string lastName = txtLastName.Text.Trim();
            string firstName = txtFirstName.Text.Trim();
            string middleName = string.IsNullOrWhiteSpace(txtMiddleName.Text) ? null : txtMiddleName.Text.Trim();
            DateTime dob = dpDOB.SelectedDate.Value;
            int classId = (int)cmbClasses.SelectedValue;
            string gender = cmbGender.SelectedItem != null ? ((ComboBoxItem)cmbGender.SelectedItem).Content.ToString() : "-";
            DateTime? enrollmentDate = dpEnrollmentDate.SelectedDate;

            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    string sql;
                    SqlCommand cmd;

                    if (_studentId > 0) // РЕЖИМ РЕДАГУВАННЯ (UPDATE)
                    {
                        sql = @"
                            UPDATE Students SET
                            LastName = @LastName, FirstName = @FirstName, MiddleName = @MiddleName, 
                            DateOfBirth = @DOB, ClassID = @ClassID, Gender = @Gender, 
                            SchoolName = @SchoolName, DocumentType = @DocType, DocumentNumber = @DocNum, 
                            EnrollmentDate = @EnrollDate, EnrollmentReason = @EnrollReason, 
                            ParentContactPhone = @ParentContact
                            WHERE StudentID = @ID";

                        cmd = new SqlCommand(sql, con);
                        cmd.Parameters.AddWithValue("@ID", _studentId);
                    }
                    else // РЕЖИМ ДОДАВАННЯ (INSERT)
                    {
                        sql = @"
                            INSERT INTO Students 
                            (LastName, FirstName, MiddleName, DateOfBirth, ClassID, Gender, 
                             SchoolName, DocumentType, DocumentNumber, EnrollmentDate, EnrollmentReason, ParentContactPhone)
                            OUTPUT INSERTED.StudentID 
                            VALUES 
                            (@LastName, @FirstName, @MiddleName, @DOB, @ClassID, @Gender, 
                             @SchoolName, @DocType, @DocNum, @EnrollDate, @EnrollReason, @ParentContact);";

                        cmd = new SqlCommand(sql, con);
                    }

                    // Спільні параметри
                    cmd.Parameters.AddWithValue("@LastName", lastName);
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@MiddleName", (object)middleName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DOB", dob);
                    cmd.Parameters.AddWithValue("@ClassID", classId);
                    cmd.Parameters.AddWithValue("@Gender", gender);

                    cmd.Parameters.AddWithValue("@SchoolName", (object)txtSchoolName.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DocType", (object)txtDocumentType.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DocNum", (object)txtDocumentNumber.Text.Trim() ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@EnrollDate", (object)enrollmentDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EnrollReason", (object)txtEnrollmentReason.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ParentContact", (object)txtParentContactPhone.Text.Trim() ?? DBNull.Value);

                    if (_studentId == 0)
                    {
                        _studentId = (int)cmd.ExecuteScalar();
                    }
                    else
                    {
                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Помилка бази даних при збереженні даних учня.\nДеталі: {ex.Message}", "Помилка SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Непередбачена помилка при збереженні даних: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_studentId == 0) return;

            string studentName = $"{txtLastName.Text} {txtFirstName.Text}";
            MessageBoxResult result = MessageBox.Show(
                $"Ви впевнені, що хочете видалити учня: {studentName}? \n\nЦя дія є незворотною і ВИДАЛИТЬ усі його пов'язані дані (батьків, оцінки тощо).",
                "🔴 Увага! Підтвердіть видалення",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                // ... (логіка видалення з транзакцією, як у попередньому коді) ...
                {
                    using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                    {
                        con.Open();
                        SqlTransaction transaction = con.BeginTransaction();

                        try
                        {
                            // 1. ВИДАЛЕННЯ БАТЬКІВ (Parents)
                            string sqlDeleteParents = "DELETE FROM Parents WHERE StudentID = @ID";
                            SqlCommand cmdDeleteParents = new SqlCommand(sqlDeleteParents, con, transaction);
                            cmdDeleteParents.Parameters.AddWithValue("@ID", _studentId);
                            cmdDeleteParents.ExecuteNonQuery();

                            // 2. ВИДАЛЕННЯ САМОГО УЧНЯ (Students)
                            string sqlDeleteStudent = "DELETE FROM Students WHERE StudentID = @ID";
                            SqlCommand cmdDeleteStudent = new SqlCommand(sqlDeleteStudent, con, transaction);
                            cmdDeleteStudent.Parameters.AddWithValue("@ID", _studentId);
                            cmdDeleteStudent.ExecuteNonQuery();

                            transaction.Commit();

                            MessageBox.Show($"Учня {studentName} успішно видалено.", "Видалення успішне", MessageBoxButton.OK, MessageBoxImage.Information);
                            this.DialogResult = true;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show("Помилка при видаленні учня: " + ex.Message, "Помилка БД", MessageBoxButton.OK, MessageBoxImage.Error);
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
}