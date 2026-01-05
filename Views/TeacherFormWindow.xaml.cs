using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using SchoolJournalApp.Services;
using SchoolJournalApp.Views;

namespace SchoolJournalApp
{
    public partial class TeacherFormWindow : Window
    {
        // Якщо 0 - режим додавання (INSERT); Якщо > 0 - режим редагування (UPDATE)
        private int _teacherId;

        // Конструктор: приймає ID вчителя (0 для нового)
        public TeacherFormWindow(int teacherId = 0)
        {
            InitializeComponent();
            _teacherId = teacherId;
            LoadDefaultComboBoxData(); // Завантажуємо довідники

            if (_teacherId > 0)
            {
                // Режим редагування
                LoadTeacherData(_teacherId);
                txtTitle.Text = "✏️ Редагування даних педагога";
                btnSave.Content = "Зберегти Зміни";
                btnDelete.Visibility = Visibility.Visible;
            }
            else
            {
                // Режим додавання
                txtTitle.Text = "➕ Додавання нового педагога";
                btnSave.Content = "Зберегти та Додати";
                btnDelete.Visibility = Visibility.Collapsed;
                // Встановлюємо дефолтні значення для ComboBoxes
                cmbGender.SelectedIndex = 2; // "-"
                txtWorkload.Text = "1.00";
            }
        }

        // Завантаження даних для ComboBox (довідників)
        private void LoadDefaultComboBoxData()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();

                    // 1. Посади
                    SqlDataAdapter daPos = new SqlDataAdapter("SELECT PositionID, PositionName FROM Positions", con);
                    DataTable dtPos = new DataTable(); daPos.Fill(dtPos);
                    cmbPosition.ItemsSource = dtPos.DefaultView;
                    if (dtPos.Rows.Count > 0 && _teacherId == 0) cmbPosition.SelectedIndex = 0;

                    // 2. Кваліфікації
                    SqlDataAdapter daQual = new SqlDataAdapter("SELECT QualificationID, QualificationName FROM Qualifications", con);
                    DataTable dtQual = new DataTable(); daQual.Fill(dtQual);
                    cmbCategory.ItemsSource = dtQual.DefaultView;

                    // 3. Звання
                    SqlDataAdapter daTitle = new SqlDataAdapter("SELECT TitleID, TitleName FROM PedagogicalTitles", con);
                    DataTable dtTitle = new DataTable(); daTitle.Fill(dtTitle);
                    cmbTitle.ItemsSource = dtTitle.DefaultView;

                    // 4. Класи (для класного керівника)
                    DataTable dtClasses = new DataTable();
                    dtClasses.Columns.Add("ClassID", typeof(int));
                    dtClasses.Columns.Add("ClassName", typeof(string));
                    dtClasses.Rows.Add(0, "Немає");

                    SqlDataAdapter daClasses = new SqlDataAdapter("SELECT ClassID, ClassName FROM Classes ORDER BY GradeLevel", con);
                    daClasses.Fill(dtClasses);
                    cmbHomeroomClass.ItemsSource = dtClasses.DefaultView;
                    cmbHomeroomClass.SelectedValue = 0; // Дефолт "Немає"
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження довідників: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Завантаження існуючих даних вчителя (Тільки для режиму редагування)
        private void LoadTeacherData(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    string sql = "SELECT * FROM Teachers WHERE TeacherID = @ID";
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@ID", id);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            string midName = r["MiddleName"] != DBNull.Value ? r["MiddleName"].ToString() : "";
                            txtFullName.Text = $"{r["LastName"]} {r["FirstName"]} {midName}";

                            dpDOB.SelectedDate = r["DateOfBirth"] != DBNull.Value ? (DateTime)r["DateOfBirth"] : (DateTime?)null;

                            string gender = r["Gender"] != DBNull.Value ? r["Gender"].ToString() : "-";
                            foreach (ComboBoxItem item in cmbGender.Items)
                            {
                                if (item.Content.ToString() == gender)
                                {
                                    cmbGender.SelectedItem = item;
                                    break;
                                }
                            }

                            txtSpecialization.Text = r["Specialization"] != DBNull.Value ? r["Specialization"].ToString() : "";
                            txtPhone.Text = r["Phone"] != DBNull.Value ? r["Phone"].ToString() : "";
                            txtEmail.Text = r["Email"] != DBNull.Value ? r["Email"].ToString() : "";

                            if (r["PositionID"] != DBNull.Value) cmbPosition.SelectedValue = (int)r["PositionID"];
                            if (r["QualificationID"] != DBNull.Value) cmbCategory.SelectedValue = (int)r["QualificationID"];
                            if (r["PedagogicalTitleID"] != DBNull.Value) cmbTitle.SelectedValue = (int)r["PedagogicalTitleID"];


                            // --- НОВЕ: Логін ---
                            txtLogin.Text = r["Login"] != DBNull.Value ? r["Login"].ToString() : "";

                            txtWorkload.Text = r["Workload"] != DBNull.Value ? r["Workload"].ToString() : "";
                            txtEducation.Text = r["EducationInfo"] != DBNull.Value ? r["EducationInfo"].ToString() : "";
                        }
                    }

                    // НОВИЙ ВИКЛИК
                    LoadTeacherAssignments(id);

                    // Класний керівник
                    SqlCommand cmdHomeroom = new SqlCommand("SELECT ClassID FROM Classes WHERE HomeroomTeacherID = @ID", con);
                    cmdHomeroom.Parameters.AddWithValue("@ID", id);
                    object hrClassID = cmdHomeroom.ExecuteScalar();

                    if (hrClassID != null)
                        cmbHomeroomClass.SelectedValue = (int)hrClassID;
                    else
                        cmbHomeroomClass.SelectedValue = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження даних вчителя: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = false;
                this.Close();
            }
        }


        // НОВИЙ МЕТОД: Завантаження поточного навантаження вчителя для відображення
        private void LoadTeacherAssignments(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();

                    string sqlSubj = @"SELECT s.SubjectName, c.ClassName 
                                       FROM TeachingAssignments ta
                                       JOIN Subjects s ON ta.SubjectID = s.SubjectID
                                       JOIN Classes c ON ta.ClassID = c.ClassID
                                       WHERE ta.TeacherID = @ID
                                       ORDER BY c.ClassName, s.SubjectName";

                    SqlDataAdapter daS = new SqlDataAdapter(sqlSubj, con);
                    daS.SelectCommand.Parameters.AddWithValue("@ID", id);
                    DataTable dtSubj = new DataTable();
                    daS.Fill(dtSubj);

                    // gridAssignments - це x:Name для DataGrid в TeacherFormWindow.xaml
                    gridAssignments.ItemsSource = dtSubj.DefaultView;
                }
            }
            catch (Exception ex)
            {
                // Ігноруємо помилки, якщо gridAssignments ще не існує (наприклад, у вікні дизайнера)
                // Але в бойовому коді може бути корисно логувати.
            }
        }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (SaveOrUpdateTeacherData())
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


        // НОВИЙ МЕТОД: Обробник для керування навантаженням
        // Новий МЕТОД: Обробник для керування навантаженням
        private void BtnManageAssignments_Click(object sender, RoutedEventArgs e)
        {
            if (_teacherId == 0)
            {
                // Запобіжник, як у вашому прикладі
                MessageBox.Show("Будь ласка, спочатку збережіть основного вчителя, щоб керувати його навантаженням (предметами).", "Операція недоступна", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Отримуємо ПІБ для заголовка нового вікна
                string teacherName = txtFullName.Text;

                // СТВОРЕННЯ ТА ВІДКРИТТЯ НОВОГО ВІКНА
                AssignmentsWindow assignmentsWindow = new AssignmentsWindow(_teacherId, teacherName);
                bool? result = assignmentsWindow.ShowDialog();

                if (result == true)
                {
                    // Якщо в AssignmentsWindow були зміни (DialogResult = true), 
                    // оновлюємо DataGrid у TeacherFormWindow.
                    LoadTeacherAssignments(_teacherId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при відкритті форми навантаження: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Єдина логіка для INSERT або UPDATE
        private bool SaveOrUpdateTeacherData()
        {
            // 1. Валідація
            string[] names = txtFullName.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (names.Length < 2)
            {
                MessageBox.Show("Будь ласка, введіть ПІБ (Прізвище Ім'я По батькові).", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string lastName = names[0];
            string firstName = names[1];
            string middleName = names.Length > 2 ? names[2] : null;

            DateTime? dob = dpDOB.SelectedDate;
            if (dob == null)
            {
                MessageBox.Show("Будь ласка, оберіть дату народження.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string gender = cmbGender.SelectedItem != null ? ((ComboBoxItem)cmbGender.SelectedItem).Content.ToString() : "-";

            if (!decimal.TryParse(txtWorkload.Text, out decimal workload))
            {
                MessageBox.Show("Будь ласка, введіть коректне числове значення навантаження (ставки).", "Помилка формату", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            int homeroomClassId = cmbHomeroomClass.SelectedValue is int ? (int)cmbHomeroomClass.SelectedValue : 0;

            // --- НОВЕ: Логіка обробки пароля ---
            string newPassword = txtPassword.Password;
            string hashedPassword = null;
            if (!string.IsNullOrEmpty(newPassword))
            {
                hashedPassword = AppSession.HashPassword(newPassword);
            }
            try
            {
                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    SqlTransaction transaction = con.BeginTransaction();

                    try
                    {
                        string sql;
                        SqlCommand cmd;

                        // Формуємо SQL-запит динамічно, щоб не перезаписати пароль порожнім значенням
                        string passwordUpdateClause = hashedPassword != null ? ", Password = @Password" : "";


                        if (_teacherId > 0) // РЕЖИМ РЕДАГУВАННЯ (UPDATE)
                        {
                            sql = @"
                                UPDATE Teachers SET
                                LastName = @LastName, FirstName = @FirstName, MiddleName = @MiddleName, 
                                DateOfBirth = @DOB, Gender = @Gender, Phone = @Phone, Email = @Email, 
                                PositionID = @PositionID, QualificationID = @QualificationID, PedagogicalTitleID = @TitleID, 
                                Specialization = @Specialization, Workload = @Workload, EducationInfo = @Education
                                WHERE TeacherID = @ID";

                            cmd = new SqlCommand(sql, con, transaction);
                            cmd.Parameters.AddWithValue("@ID", _teacherId);
                        }
                        else // РЕЖИМ ДОДАВАННЯ (INSERT)
                        {
                            if (hashedPassword == null) // Пароль обов'язковий при додаванні нового
                            {
                                MessageBox.Show("Встановіть пароль для нового користувача.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return false;
                            }

                            sql = @"
                                INSERT INTO Teachers 
                                (LastName, FirstName, MiddleName, DateOfBirth, Gender, Phone, Email, 
                                 PositionID, QualificationID, PedagogicalTitleID, Specialization, Workload, EducationInfo)
                                OUTPUT INSERTED.TeacherID 
                                VALUES 
                                (@LastName, @FirstName, @MiddleName, @DOB, @Gender, @Phone, @Email, 
                                 @PositionID, @QualificationID, @TitleID, @Specialization, @Workload, @Education);";

                            cmd = new SqlCommand(sql, con, transaction);
                        }

                        // Спільні параметри
                        cmd.Parameters.AddWithValue("@LastName", lastName);
                        cmd.Parameters.AddWithValue("@FirstName", firstName);
                        cmd.Parameters.AddWithValue("@MiddleName", (object)middleName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOB", dob.Value);
                        cmd.Parameters.AddWithValue("@Gender", gender);
                        cmd.Parameters.AddWithValue("@Phone", (object)txtPhone.Text ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", (object)txtEmail.Text ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PositionID", cmbPosition.SelectedValue ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@QualificationID", cmbCategory.SelectedValue ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TitleID", cmbTitle.SelectedValue ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Specialization", (object)txtSpecialization.Text ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Workload", workload);
                        cmd.Parameters.AddWithValue("@Education", (object)txtEducation.Text ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Login", txtLogin.Text);
                        if (hashedPassword != null)
                        {
                            cmd.Parameters.AddWithValue("@Password", hashedPassword);
                        }

                        if (_teacherId == 0)
                        {
                            // Отримуємо новий ID після INSERT
                            _teacherId = (int)cmd.ExecuteScalar();
                        }
                        else
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // 2. ОНОВЛЕННЯ КЛАСНОГО КЕРІВНИЦТВА

                        // А) Знімаємо вчителя з будь-якого класу, де він був керівником
                        SqlCommand cmdClear = new SqlCommand("UPDATE Classes SET HomeroomTeacherID = NULL WHERE HomeroomTeacherID = @ID", con, transaction);
                        cmdClear.Parameters.AddWithValue("@ID", _teacherId);
                        cmdClear.ExecuteNonQuery();

                        // Б) Призначаємо його новим керівником (якщо обрано клас)
                        if (homeroomClassId > 0)
                        {
                            // Призначаємо новий клас. Якщо клас був зайнятий, то він став NULL на кроці А.
                            SqlCommand cmdAssign = new SqlCommand("UPDATE Classes SET HomeroomTeacherID = @ID WHERE ClassID = @ClassID", con, transaction);
                            cmdAssign.Parameters.AddWithValue("@ID", _teacherId);
                            cmdAssign.Parameters.AddWithValue("@ClassID", homeroomClassId);
                            cmdAssign.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Помилка бази даних: Перевірте унікальність даних (наприклад, Email) або обмеження ключів.\nДеталі: {ex.Message}", "Помилка SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Непередбачена помилка при збереженні даних: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка підключення до бази даних: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Логіка видалення (доступна лише в режимі редагування)
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_teacherId == 0) return; // Запобіжник

            string teacherName = txtFullName.Text;
            MessageBoxResult result = MessageBox.Show(
                $"Ви впевнені, що хочете видалити вчителя: {teacherName}? \n\nЦя дія є незворотною і ВИДАЛИТЬ усі його навантаження та зніме його з класних керівників.",
                "🔴 Увага! Підтвердіть видалення",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                    {
                        con.Open();
                        SqlTransaction transaction = con.BeginTransaction();

                        try
                        {
                            // 1. ВИДАЛЕННЯ ЗВ'ЯЗКІВ (TeachingAssignments)
                            string sqlDeleteAssignments = "DELETE FROM TeachingAssignments WHERE TeacherID = @ID";
                            SqlCommand cmdDeleteAssignments = new SqlCommand(sqlDeleteAssignments, con, transaction);
                            cmdDeleteAssignments.Parameters.AddWithValue("@ID", _teacherId);
                            cmdDeleteAssignments.ExecuteNonQuery();

                            // 2. СКИДАННЯ HomeroomTeacherID в таблиці Classes
                            string sqlUpdateHomeroom = "UPDATE Classes SET HomeroomTeacherID = NULL WHERE HomeroomTeacherID = @ID";
                            SqlCommand cmdUpdateHomeroom = new SqlCommand(sqlUpdateHomeroom, con, transaction);
                            cmdUpdateHomeroom.Parameters.AddWithValue("@ID", _teacherId);
                            cmdUpdateHomeroom.ExecuteNonQuery();

                            // 3. ВИДАЛЕННЯ САМОГО ВЧИТЕЛЯ (Teachers)
                            string sqlDeleteTeacher = "DELETE FROM Teachers WHERE TeacherID = @ID";
                            SqlCommand cmdDeleteTeacher = new SqlCommand(sqlDeleteTeacher, con, transaction);
                            cmdDeleteTeacher.Parameters.AddWithValue("@ID", _teacherId);
                            cmdDeleteTeacher.ExecuteNonQuery();

                            transaction.Commit();

                            MessageBox.Show($"Вчителя {teacherName} успішно видалено.", "Видалення успішне", MessageBoxButton.OK, MessageBoxImage.Information);
                            this.DialogResult = true;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
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
}