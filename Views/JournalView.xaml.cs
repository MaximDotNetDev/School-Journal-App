using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents; // Для PDF документа
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Data.SqlClient;
using SchoolJournalApp.Services;

namespace SchoolJournalApp.Views
{
    public partial class JournalView : UserControl
    {
        private DataRowView? _activeRow;
        private string _activeColName = "";

        // Словники
        private Dictionary<string, int> _columnLessonMap = new Dictionary<string, int>();
        private Dictionary<string, string> _columnHeaderMap = new Dictionary<string, string>();

        // Змінні стану
        private int _currentLessonId = 0;
        private bool _isNewLesson = false;

        // Змінні для Meet
        private string _meetLink = "https://meet.google.com/";

        // Словник: Назва Галузі -> Список описів (ГР1, ГР2, ГР3, ГР4)
        private Dictionary<string, string[]> _nushDescriptions = new Dictionary<string, string[]>()
{
    { "Громадянська та історична", new string[] {
        "ГР1 Орієнтується в історичному часі та просторі",
        "ГР2 Працює з інформацією історичного та суспільствознавчого змісту",
        "ГР3 Виявляє здатність до співпраці, толерантність, громадянську позицію"
    }},
    { "Соціальна та здоров'язбережувальна", new string[] {
        "ГР1 Безпека. Уникання загроз для життя власного та інших осіб",
        "ГР2 Здоров’я. Турбота про особисте здоров’я",
        "ГР3 Добробут. Підприємливість та етична поведінка"
    }},
    { "Природнича", new string[] {
        "ГР1 Досліджує природу",
        "ГР2 Здійснює пошук та опрацьовує інформацію",
        "ГР3 Усвідомлює закономірності природи"
    }},
    { "Технологічна", new string[] {
        "ГР1 Проєктує та виготовляє вироби",
        "ГР2 Застосовує технології декоративно-ужиткового мистецтва",
        "ГР3 Ефективне використання техніки і матеріалів",
        "ГР4 Виявляє самозарадність у побуті/освітньому процесі"
    }},
    { "Інформатична", new string[] {
        "ГР1 Працює з інформацією, даними, моделями",
        "ГР2 Створює інформаційні продукти",
        "ГР3 Працює в цифровому середовищі",
        "ГР4 Безпечно та відповідально працює з інформ. технологіями"
    }},
    { "Математична", new string[] {
        "ГР1 Досліджує ситуації та створює математичні моделі",
        "ГР2 Розв’язує математичні задачі",
        "ГР3 Інтерпретує та критично аналізує результати"
    }},
    { "Мовно-літературна (Іноземні)", new string[] {
        "ГР1 Сприймає усну інформацію на слух / Аудіювання",
        "ГР2 Усно взаємодіє та висловлюється / Говоріння",
        "ГР3 Сприймає письмові тексти / Читання",
        "ГР4 Письмово взаємодіє та висловлюється / Письмо"
    }},
    { "Освітня галузь «Фізична культура»", new string[] {
        "ГР1 Розвиває особистісні якості в процесі фіз. виховання",
        "ГР2 Володіє технікою фізичних вправ",
        "ГР3 Здійснює фізкультурно-оздоровчу діяльність"
    }},
    { "Мовно-літературна (Укр/Літ)", new string[] {
        "ГР1 Усно взаємодіє",
        "ГР2 Працює з текстом",
        "ГР3 Письмово взаємодіє",
        "ГР4 Досліджує мовлення"
    }},
    { "Мистецька", new string[] {
        "ГР1 Пізнання мистецтва, художнє мислення",
        "ГР2 Художньо-творча діяльність, мистецька комунікація",
        "ГР3 Емоційний досвід, художньо-естетичне ставлення"
    }},
    { "Загальна", new string[] {
        "ГР1 Група результатів 1",
        "ГР2 Група результатів 2",
        "ГР3 Група результатів 3",
        "ГР4 Група результатів 4"
    }}
};











        public JournalView()
        {
            InitializeComponent();
            datePicker.SelectedDate = DateTime.Today;
            LoadComboBoxes();
            LoadLessonTypes();
        }


        // Метод для визначення галузі за назвою предмету
        private string GetBranchForSubject(string subjectName)
        {
            subjectName = subjectName.ToLower();

            if (subjectName.Contains("історія") || subjectName.Contains("право")) return "Громадянська та історична";
            if (subjectName.Contains("здоров") || subjectName.Contains("етика")) return "Соціальна та здоров'язбережувальна";
            if (subjectName.Contains("природ") || subjectName.Contains("біологія") || subjectName.Contains("хімія") || subjectName.Contains("фізика") || subjectName.Contains("географія")) return "Природнича";
            if (subjectName.Contains("технології") || subjectName.Contains("трудове")) return "Технологічна";
            if (subjectName.Contains("інформатика")) return "Інформатична";
            if (subjectName.Contains("математика") || subjectName.Contains("алгебра") || subjectName.Contains("геометрія")) return "Математична";
            if (subjectName.Contains("англійська") || subjectName.Contains("німецька") || subjectName.Contains("французька")) return "Мовно-літературна (Іноземні)";
            if (subjectName.Contains("фізична культура") || subjectName.Contains("фізкультура")) return "Освітня галузь «Фізична культура»";
            if (subjectName.Contains("українська") || subjectName.Contains("література") || subjectName.Contains("читання")) return "Мовно-літературна (Укр/Літ)";
            if (subjectName.Contains("мистецтво") || subjectName.Contains("музика") || subjectName.Contains("образотворче")) return "Мистецька";

            return "Загальна";
        }

        // 1. Завантаження списків (Класи)
        private void LoadComboBoxes()
        {
            try
            {
                string sql;
                SqlParameter[] parameters = null; // За замовчуванням параметрів немає

                // АДМІНИ (1, 2) - Бачать усі класи
                if (AppSession.CurrentRoleID == 1 || AppSession.CurrentRoleID == 2)
                {
                    sql = "SELECT ClassID, ClassName FROM Classes ORDER BY GradeLevel, ClassName";
                }
                // ВЧИТЕЛЬ (3)
                else
                {
                    sql = @"SELECT DISTINCT c.ClassID, c.ClassName 
                    FROM Classes c
                    LEFT JOIN TeachingAssignments ta ON c.ClassID = ta.ClassID
                    WHERE ta.TeacherID = @TID OR c.HomeroomTeacherID = @TID
                    ORDER BY c.ClassName";
                    parameters = new SqlParameter[] { new SqlParameter("@TID", AppSession.CurrentUserId) };
                }

                DataTable dtClass = DatabaseHelper.GetDataTable(sql, parameters);
                cmbClasses.ItemsSource = dtClass.DefaultView;

                if (dtClass.Rows.Count > 0) cmbClasses.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження класів: " + ex.Message);
            }
        }

        // 2. Завантаження предметів для конкретного класу
        private void LoadSubjectsForClass(int classId)
        {
            try
            {
                string sql;
                bool showAllSubjects = false;

                // Перевіряємо права
                if (AppSession.CurrentRoleID == 1 || AppSession.CurrentRoleID == 2)
                {
                    showAllSubjects = true;
                }
                else
                {
                    // Перевіряємо, чи це класний керівник
                    string sqlCheck = "SELECT COUNT(*) FROM Classes WHERE ClassID = @CID AND HomeroomTeacherID = @TID";
                    SqlParameter[] paramsCheck = {
                new SqlParameter("@CID", classId),
                new SqlParameter("@TID", AppSession.CurrentUserId)
            };

                    DataTable dtCheck = DatabaseHelper.GetDataTable(sqlCheck, paramsCheck);
                    if (dtCheck.Rows.Count > 0 && (int)dtCheck.Rows[0][0] > 0)
                        showAllSubjects = true;
                }

                SqlParameter[] parameters;

                if (showAllSubjects)
                {
                    sql = @"SELECT DISTINCT s.SubjectID, s.SubjectName 
                    FROM Subjects s
                    JOIN TeachingAssignments ta ON s.SubjectID = ta.SubjectID
                    WHERE ta.ClassID = @CID
                    ORDER BY s.SubjectName";
                    parameters = new SqlParameter[] { new SqlParameter("@CID", classId) };
                }
                else
                {
                    sql = @"SELECT DISTINCT s.SubjectID, s.SubjectName 
                    FROM Subjects s
                    JOIN TeachingAssignments ta ON s.SubjectID = ta.SubjectID
                    WHERE ta.ClassID = @CID AND ta.TeacherID = @TID
                    ORDER BY s.SubjectName";
                    parameters = new SqlParameter[] {
                new SqlParameter("@CID", classId),
                new SqlParameter("@TID", AppSession.CurrentUserId)
            };
                }

                DataTable dt = DatabaseHelper.GetDataTable(sql, parameters);
                cmbSubjects.ItemsSource = dt.DefaultView;

                if (dt.Rows.Count > 0) cmbSubjects.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show("Помилка предметів: " + ex.Message); }
        }

        // 3. Завантаження типів уроків
        // Клас для відображення в ComboBox
        public class LessonTypeItem
        {
            public int ID { get; set; }
            public string Name { get; set; }
        }

        private void LoadLessonTypes()
        {
            try
            {
                // 1. Завантажуємо всі типи з БД
                DataTable dt = DatabaseHelper.GetDataTable("SELECT GradeTypeID, TypeName FROM GradeTypes");

                List<LessonTypeItem> standardTypes = new List<LessonTypeItem>();
                standardTypes.Add(new LessonTypeItem { ID = 1, Name = "Поточна" }); // Додаємо дефолтний, якщо його немає

                foreach (DataRow row in dt.Rows)
                {
                    int id = (int)row["GradeTypeID"];
                    string name = row["TypeName"].ToString();

                    // Фільтруємо: ID < 13 - це стандартні типи, ID >= 13 - це ГР (НУШ)
                    // (згідно з вашим SQL файлом: 13=ГР1, 14=ГР2, 15=ГР3, 16=ГР4)
                    if (id < 13)
                    {
                        // Не додаємо "Поточна" двічі, якщо вона є в БД під ID 1
                        if (id == 1 && standardTypes.Count > 0) continue;
                        standardTypes.Add(new LessonTypeItem { ID = id, Name = name });
                    }
                }

                cmbStandardType.ItemsSource = standardTypes;
                cmbStandardType.DisplayMemberPath = "Name";
                cmbStandardType.SelectedValuePath = "ID";

                // За замовчуванням
                cmbStandardType.SelectedValue = 1;
            }
            catch (Exception ex) { MessageBox.Show("Помилка типів: " + ex.Message); }
        }

        // Коли змінюється предмет, оновлюємо список ГР (НУШ)
        private void UpdateNushCombo()
        {
            if (cmbSubjects.SelectedItem == null) return;

            // Отримуємо назву предмету (з DataRowView або напряму, залежно від прив'язки)
            string subjectName = cmbSubjects.Text;
            string branch = GetBranchForSubject(subjectName);

            txtBranchName.Text = $"Галузь: {branch}"; // Покажемо користувачу, яка галузь визначилась

            string[] descriptions = _nushDescriptions[branch];
            List<LessonTypeItem> nushTypes = new List<LessonTypeItem>();

            // Прив'язуємо описи до ID в базі (13 -> ГР1, 14 -> ГР2 ...)
            // Важливо: descriptions[0] це ГР1 (ID 13), descriptions[1] це ГР2 (ID 14)
            for (int i = 0; i < descriptions.Length; i++)
            {
                nushTypes.Add(new LessonTypeItem { ID = 13 + i, Name = descriptions[i] });
            }

            cmbNushType.ItemsSource = nushTypes;
            cmbNushType.DisplayMemberPath = "Name";
            cmbNushType.SelectedValuePath = "ID";
        }

        // Логіка: Якщо вибрали Стандартний -> очистити НУШ
        private void CmbStandardType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbStandardType.SelectedIndex != -1)
            {
                // Вимикаємо обробник подій тимчасово, щоб не зациклитись
                cmbNushType.SelectionChanged -= CmbNushType_SelectionChanged;
                cmbNushType.SelectedIndex = -1;
                cmbNushType.SelectionChanged += CmbNushType_SelectionChanged;
            }
        }

        // Логіка: Якщо вибрали НУШ -> очистити Стандартний
        private void CmbNushType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbNushType.SelectedIndex != -1)
            {
                cmbStandardType.SelectionChanged -= CmbStandardType_SelectionChanged;
                cmbStandardType.SelectedIndex = -1;
                cmbStandardType.SelectionChanged += CmbStandardType_SelectionChanged;
            }
        }


        // 4. ГОЛОВНИЙ МЕТОД: Завантаження даних журналу
        private void LoadJournalData(int classId, int subjectId)
        {
            try
            {
                _columnLessonMap.Clear();
                _columnHeaderMap.Clear();

                // Підготовка параметрів (вони однакові для всіх трьох запитів)
                SqlParameter[] parameters = {
            new SqlParameter("@C", classId),
            new SqlParameter("@S", subjectId)
        };

                // --- ЗАПИТ 1: УЧНІ ---
                // Важливо: створюємо нові параметри для кожного виклику, або передаємо масив
                // Оскільки DatabaseHelper використовує AddRange, краще створити параметри окремо для кожного виклику, 
                // або просто передати масив, якщо Helper коректно працює. 
                // Але наш фікс був "new SqlParameter[]" у виклику. Давайте так і робитимемо.

                DataTable dtStudents = DatabaseHelper.GetDataTable(
                    "SELECT StudentID, LastName + ' ' + FirstName AS FullName FROM Students WHERE ClassID = @C ORDER BY LastName",
                    new SqlParameter[] { new SqlParameter("@C", classId) }
                );

                // --- ЗАПИТ 2: УРОКИ ---
                string sqlLess = @"
            SELECT l.LessonID, l.LessonDate, l.LessonTopic, l.Homework, gt.TypeName 
            FROM Lessons l 
            JOIN TeachingAssignments ta ON l.AssignmentID = ta.AssignmentID
            LEFT JOIN GradeTypes gt ON l.LessonTypeID = gt.GradeTypeID
            WHERE ta.ClassID = @C AND ta.SubjectID = @S 
            ORDER BY l.LessonDate";

                DataTable dtLessons = DatabaseHelper.GetDataTable(sqlLess, new SqlParameter[] {
            new SqlParameter("@C", classId),
            new SqlParameter("@S", subjectId)
        });

                // --- ЗАПИТ 3: ОЦІНКИ ---
                string sqlGrades = @"
            SELECT g.StudentID, g.LessonID, g.GradeValue 
            FROM Grades g 
            JOIN Lessons l ON g.LessonID = l.LessonID 
            JOIN TeachingAssignments ta ON l.AssignmentID = ta.AssignmentID 
            WHERE ta.ClassID = @C AND ta.SubjectID = @S";

                DataTable dtGrades = DatabaseHelper.GetDataTable(sqlGrades, new SqlParameter[] {
            new SqlParameter("@C", classId),
            new SqlParameter("@S", subjectId)
        });

                // === ДАЛІ ЙДЕ ЛОГІКА ПОБУДОВИ ТАБЛИЦІ (Вона залишається без змін, тільки копіюємо її) ===
                DataTable dtView = new DataTable();
                dtView.Columns.Add("StudentID", typeof(int));
                dtView.Columns.Add("№", typeof(int));
                dtView.Columns.Add("Учень", typeof(string));

                DataTable dtTopics = new DataTable();
                dtTopics.Columns.Add("Date", typeof(string));
                dtTopics.Columns.Add("LessonNumber", typeof(string));
                dtTopics.Columns.Add("Type", typeof(string));
                dtTopics.Columns.Add("Topic", typeof(string));
                dtTopics.Columns.Add("Homework", typeof(string));

                int lessonCounter = 1;
                int previousMonth = -1;

                foreach (DataRow lesson in dtLessons.Rows)
                {
                    int lessonID = (int)lesson["LessonID"];
                    DateTime dateObj = Convert.ToDateTime(lesson["LessonDate"]);
                    string dateStr;

                    if (dateObj.Month != previousMonth)
                    {
                        dateStr = dateObj.ToString("dd.MM");
                        previousMonth = dateObj.Month;
                    }
                    else
                    {
                        dateStr = dateObj.ToString("dd");
                    }

                    string type = lesson["TypeName"].ToString();
                    string topic = lesson["LessonTopic"].ToString();
                    string homework = lesson["Homework"].ToString();

                    string finalHeader = dateStr;
                    // (Логіка скорочення назв типів уроків залишається такою ж, як була у вашому коді)
                    if (!string.IsNullOrEmpty(type) && type != "Поточна")
                    {
                        string label = type;

                        // Перевірка на ГР (НУШ)
                        if (type.Contains("Група результатів 1") || type.Contains("ГР1")) label = "ГР 1";
                        else if (type.Contains("Група результатів 2") || type.Contains("ГР2")) label = "ГР 2";
                        else if (type.Contains("Група результатів 3") || type.Contains("ГР3")) label = "ГР 3";
                        else if (type.Contains("Група результатів 4") || type.Contains("ГР4")) label = "ГР 4";

                        // Стандартні скорочення
                        else if (type.Contains("Контрольна")) label = "(К.Р.)";
                        else if (type.Contains("Самостійна")) label = "(С.Р.)";
                        else if (type.Contains("Лабораторна")) label = "(Л.Р.)";
                        else if (type.Contains("Практична")) label = "(Пр.)";
                        else if (type.Contains("Діагностувальна")) label = "(Діаг.)";
                        else if (type.Contains("Зошит")) label = "Зошит";
                        else if (type.Contains("Тематична")) label = "(Тем)";
                        else if (type.Contains("Загальна")) label = "Загальна";
                        else if (type.Contains("Семестрова")) label = "Сем.";
                        else if (type.Contains("Річна")) label = "Річна";
                        else if (type.Contains("Скоригована")) label = "Скор.";
                        else if (type.Contains("Підсумкова")) label = "Підс.";

                        // ФОРМАТУВАННЯ: Дата \n Тип
                        // Це забезпечить те, що ви просили: дата зверху, тип знизу
                        finalHeader = $"{dateStr}\n{label}";
                    }
                    string safeColName = $"Col_{lessonID}";
                    int i = 1;
                    string originalSafe = safeColName;
                    while (dtView.Columns.Contains(safeColName)) safeColName = $"{originalSafe}_{i++}";

                    dtView.Columns.Add(safeColName);
                    _columnLessonMap[safeColName] = lessonID;
                    _columnHeaderMap[safeColName] = finalHeader;

                    DataRow topicRow = dtTopics.NewRow();
                    topicRow["Date"] = dateObj.ToString("dd.MM.yyyy");
                    topicRow["LessonNumber"] = lessonCounter++;
                    topicRow["Type"] = type == "Поточна" ? "" : type;
                    topicRow["Topic"] = topic;
                    topicRow["Homework"] = homework;
                    dtTopics.Rows.Add(topicRow);
                }

                int studentNum = 1;
                foreach (DataRow student in dtStudents.Rows)
                {
                    DataRow newRow = dtView.NewRow();
                    int sId = (int)student["StudentID"];
                    newRow["StudentID"] = sId;
                    newRow["№"] = studentNum++;
                    newRow["Учень"] = student["FullName"];

                    foreach (DataRow lesson in dtLessons.Rows)
                    {
                        int lId = (int)lesson["LessonID"];
                        // Шукаємо оцінку в завантаженій таблиці dtGrades (в пам'яті) замість запиту в БД
                        DataRow[] grades = dtGrades.Select($"StudentID={sId} AND LessonID={lId}");
                        if (grades.Length > 0)
                        {
                            foreach (var kvp in _columnLessonMap)
                            {
                                if (kvp.Value == lId)
                                {
                                    newRow[kvp.Key] = grades[0]["GradeValue"];
                                    break;
                                }
                            }
                        }
                    }
                    dtView.Rows.Add(newRow);
                }

                gridJournal.ItemsSource = null;
                gridJournal.ItemsSource = dtView.DefaultView;
                if (gridJournal.Columns.Count > 0) gridJournal.Columns[0].Visibility = Visibility.Collapsed;

                gridTopics.ItemsSource = null;
                gridTopics.ItemsSource = dtTopics.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Помилка завантаження журналу: " + ex.Message); }
        }
        
        // 2. ПОДІЯ, ЯКОЇ НЕ ВИСТАЧАЛО (Вставте цей метод!)
        private void CmbClasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbClasses.SelectedValue == null) return;

            // Отримуємо ID вибраного класу і вантажимо предмети
            int classId = (int)cmbClasses.SelectedValue;
            LoadSubjectsForClass(classId);
        }

        // Додаємо async
        // Цей метод має бути у файлі ОДИН раз
        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                await RefreshJournalAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка: " + ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private async Task RefreshJournalAsync()
        {
            if (cmbClasses.SelectedValue == null || cmbSubjects.SelectedValue == null) return;

            int classId = (int)cmbClasses.SelectedValue;
            int subjectId = (int)cmbSubjects.SelectedValue;

            // Викликаємо LoadJournalDataAsync (який ми зараз створимо)
            await LoadJournalDataAsync(classId, subjectId);
        }

        // Зверніть увагу на: async Task
        private async Task LoadJournalDataAsync(int classId, int subjectId)
        {
            try
            {
                _columnLessonMap.Clear();
                _columnHeaderMap.Clear();

                // 1. Асинхронне отримання даних (не блокує інтерфейс)

                // Учні
                DataTable dtStudents = await DatabaseHelper.GetDataTableAsync(
                    "SELECT StudentID, LastName + ' ' + FirstName AS FullName FROM Students WHERE ClassID = @C ORDER BY LastName",
                    new SqlParameter[] { new SqlParameter("@C", classId) }
                );

                // Уроки
                string sqlLess = @"
            SELECT l.LessonID, l.LessonDate, l.LessonTopic, l.Homework, gt.TypeName 
            FROM Lessons l 
            JOIN TeachingAssignments ta ON l.AssignmentID = ta.AssignmentID
            LEFT JOIN GradeTypes gt ON l.LessonTypeID = gt.GradeTypeID
            WHERE ta.ClassID = @C AND ta.SubjectID = @S 
            ORDER BY l.LessonDate";

                DataTable dtLessons = await DatabaseHelper.GetDataTableAsync(sqlLess, new SqlParameter[] {
            new SqlParameter("@C", classId),
            new SqlParameter("@S", subjectId)
        });

                // Оцінки
                string sqlGrades = @"
            SELECT g.StudentID, g.LessonID, g.GradeValue 
            FROM Grades g 
            JOIN Lessons l ON g.LessonID = l.LessonID 
            JOIN TeachingAssignments ta ON l.AssignmentID = ta.AssignmentID 
            WHERE ta.ClassID = @C AND ta.SubjectID = @S";

                DataTable dtGrades = await DatabaseHelper.GetDataTableAsync(sqlGrades, new SqlParameter[] {
            new SqlParameter("@C", classId),
            new SqlParameter("@S", subjectId)
        });

                // 2. Обробка даних (створення таблиць для відображення)

                // Створюємо dtView (Основна таблиця оцінок)
                DataTable dtView = new DataTable();
                dtView.Columns.Add("StudentID", typeof(int));
                dtView.Columns.Add("№", typeof(int));
                dtView.Columns.Add("Учень", typeof(string));

                // Створюємо dtTopics (Нижня таблиця тем)
                DataTable dtTopics = new DataTable();
                dtTopics.Columns.Add("Date", typeof(string));
                dtTopics.Columns.Add("LessonNumber", typeof(string));
                dtTopics.Columns.Add("Type", typeof(string));
                dtTopics.Columns.Add("Topic", typeof(string));
                dtTopics.Columns.Add("Homework", typeof(string));

                int lessonCounter = 1;
                int previousMonth = -1;

                // Проходимо по уроках, створюємо колонки
                foreach (DataRow lesson in dtLessons.Rows)
                {
                    int lessonID = (int)lesson["LessonID"];
                    DateTime dateObj = Convert.ToDateTime(lesson["LessonDate"]);
                    string dateStr;

                    if (dateObj.Month != previousMonth)
                    {
                        dateStr = dateObj.ToString("dd.MM");
                        previousMonth = dateObj.Month;
                    }
                    else
                    {
                        dateStr = dateObj.ToString("dd");
                    }

                    string type = lesson["TypeName"].ToString();
                    string topic = lesson["LessonTopic"].ToString();
                    string homework = lesson["Homework"].ToString();

                    // Логіка заголовків (Дата зверху, тип знизу)
                    string finalHeader = dateStr;
                    if (!string.IsNullOrEmpty(type) && type != "Поточна")
                    {
                        string label = type;

                        if (type.Contains("Група результатів 1") || type.Contains("ГР1")) label = "ГР 1";
                        else if (type.Contains("Група результатів 2") || type.Contains("ГР2")) label = "ГР 2";
                        else if (type.Contains("Група результатів 3") || type.Contains("ГР3")) label = "ГР 3";
                        else if (type.Contains("Група результатів 4") || type.Contains("ГР4")) label = "ГР 4";
                        else if (type.Contains("Загальна")) label = "Загальна";
                        else if (type.Contains("Контрольна")) label = "(К.Р.)";
                        else if (type.Contains("Самостійна")) label = "(С.Р.)";
                        else if (type.Contains("Тематична")) label = "(Тем)";
                        else if (type.Contains("Семестрова")) label = "Сем.";
                        else if (type.Contains("Річна")) label = "Річна";

                        finalHeader = $"{dateStr}\n{label}";
                    }

                    // Створюємо унікальне ім'я колонки
                    string safeColName = $"Col_{lessonID}";
                    int i = 1;
                    string originalSafe = safeColName;
                    while (dtView.Columns.Contains(safeColName)) safeColName = $"{originalSafe}_{i++}";

                    dtView.Columns.Add(safeColName);
                    _columnLessonMap[safeColName] = lessonID;
                    _columnHeaderMap[safeColName] = finalHeader;

                    // Додаємо рядок у таблицю тем
                    DataRow topicRow = dtTopics.NewRow();
                    topicRow["Date"] = dateObj.ToString("dd.MM.yyyy");
                    topicRow["LessonNumber"] = lessonCounter++;
                    topicRow["Type"] = type == "Поточна" ? "" : type;
                    topicRow["Topic"] = topic;
                    topicRow["Homework"] = homework;
                    dtTopics.Rows.Add(topicRow);
                }

                // Заповнюємо оцінки
                int studentNum = 1;
                foreach (DataRow student in dtStudents.Rows)
                {
                    DataRow newRow = dtView.NewRow();
                    int sId = (int)student["StudentID"];
                    newRow["StudentID"] = sId;
                    newRow["№"] = studentNum++;
                    newRow["Учень"] = student["FullName"];

                    // Шукаємо оцінки в пам'яті (швидко)
                    foreach (DataRow lesson in dtLessons.Rows)
                    {
                        int lId = (int)lesson["LessonID"];

                        // Фільтруємо dtGrades локально
                        DataRow[] grades = dtGrades.Select($"StudentID={sId} AND LessonID={lId}");

                        if (grades.Length > 0)
                        {
                            // Знаходимо, якій колонці відповідає цей урок
                            foreach (var kvp in _columnLessonMap)
                            {
                                if (kvp.Value == lId)
                                {
                                    newRow[kvp.Key] = grades[0]["GradeValue"];
                                    break;
                                }
                            }
                        }
                    }
                    dtView.Rows.Add(newRow);
                }

                // 3. Прив'язка до інтерфейсу
                gridJournal.ItemsSource = null;
                gridJournal.ItemsSource = dtView.DefaultView;

                // Ховаємо службову колонку ID
                if (gridJournal.Columns.Count > 0) gridJournal.Columns[0].Visibility = Visibility.Collapsed;

                gridTopics.ItemsSource = null;
                gridTopics.ItemsSource = dtTopics.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження журналу: " + ex.Message);
            }
        }


        private void RefreshJournal()
        {
            if (cmbClasses.SelectedValue == null || cmbSubjects.SelectedValue == null) return;
            LoadJournalData((int)cmbClasses.SelectedValue, (int)cmbSubjects.SelectedValue);
        }

        // 4. Налаштування вигляду колонок
        private void GridJournal_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.CanUserSort = false;

            // === 1. СТВОРЮЄМО ГЛОБАЛЬНИЙ СТИЛЬ ДЛЯ ВСІХ ЗАГОЛОВКІВ (ЖИРНИЙ + ПО ЦЕНТРУ) ===
            Style headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HeightProperty, 45.0)); // Трохи вища шапка для двох рядків
                                                                                            // ГОЛОВНЕ: Центрування вмісту заголовка по горизонталі та вертикалі
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            // ГОЛОВНЕ: Жирний шрифт для всієї шапки
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontSizeProperty, 12.0));

            // Кольори фону та рамок
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E1F5FE"))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B3E5FC"))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));

            // Застосовуємо цей стиль до поточної колонки
            e.Column.HeaderStyle = headerStyle;

            // === 2. ОБРОБКА КОЛОНОК З ДАТАМИ (УРОКИ) ===
            if (_columnHeaderMap.ContainsKey(e.PropertyName))
            {
                // ВСТАНОВЛЮЄМО ФІКСОВАНУ ШИРИНУ ДЛЯ ВСІХ УРОКІВ
                e.Column.Width = new DataGridLength(40); // 55px - достатньо для "21.11" і "(К.Р.)"

                string rawHeader = _columnHeaderMap[e.PropertyName];

                // Створюємо TextBlock для красивого відображення дати (синій місяць)
                TextBlock headerBlock = new TextBlock();
                headerBlock.TextAlignment = TextAlignment.Center;
                headerBlock.HorizontalAlignment = HorizontalAlignment.Center;
                headerBlock.VerticalAlignment = VerticalAlignment.Center;
                headerBlock.TextWrapping = TextWrapping.Wrap; // Дозволяємо перенос слів

                var parts = rawHeader.Split('\n');
                string datePart = parts[0];

                if (datePart.Contains("."))
                {
                    int dotIndex = datePart.IndexOf('.');
                    string day = datePart.Substring(0, dotIndex);
                    string month = datePart.Substring(dotIndex);

                    // День чорним
                    headerBlock.Inlines.Add(new Run(day));
                    // Місяць СИНІМ (стиль жирний вже заданий у HeaderStyle, але тут дублюємо колір)
                    headerBlock.Inlines.Add(new Run(month) { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0")) });
                }
                else
                {
                    headerBlock.Inlines.Add(new Run(datePart));
                }

                if (parts.Length > 1)
                {
                    headerBlock.Inlines.Add(new LineBreak());
                    // Тип уроку (наприклад, К.Р.)
                    headerBlock.Inlines.Add(new Run(parts[1]) { FontSize = 11, Foreground = Brushes.DarkSlateGray, FontWeight = FontWeights.Normal });
                }

                e.Column.Header = headerBlock;
            }

            // === 3. КОЛОНКА "№" ===
            if (e.PropertyName == "№")
            {
                e.Column.DisplayIndex = 1;
                e.Column.Width = new DataGridLength(35); // Фіксована ширина
                e.Column.Header = "№";

                // Вміст клітинок (цифри) по центру
                var style = new Style(typeof(TextBlock));
                style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                (e.Column as DataGridTextColumn).ElementStyle = style;
                return;
            }

            // === 4. КОЛОНКА "УЧЕНЬ" ===
            if (e.PropertyName == "Учень")
            {
                e.Column.DisplayIndex = 2;
                e.Column.Width = new DataGridLength(220);
                e.Column.Header = "Учень"; // Заголовок буде по центру (завдяки headerStyle вище)

                // Вміст клітинок (імена) - ЗЛІВА
                Style studentStyle = new Style(typeof(TextBlock));
                studentStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left)); // Ім'я зліва
                studentStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)); // По вертикалі центр
                studentStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(10, 0, 0, 0))); // Відступ від краю
                studentStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Medium));

                (e.Column as DataGridTextColumn).ElementStyle = studentStyle;
                return;
            }

            // === 5. СТИЛЬ ОЦІНОК (ВМІСТ КЛІТИНОК) ===
            if (e.Column is DataGridTextColumn textCol && e.PropertyName != "StudentID")
            {
                Style textStyle = new Style(typeof(TextBlock));
                textStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                textStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                textStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));

                Binding colorBinding = new Binding(e.PropertyName);
                colorBinding.Converter = new GradeColorConverter();
                textStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, colorBinding));

                DataTrigger selectionTrigger = new DataTrigger();
                selectionTrigger.Binding = new Binding("IsSelected") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridCell), 1) };
                selectionTrigger.Value = true;
                selectionTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.White));
                textStyle.Triggers.Add(selectionTrigger);

                textCol.ElementStyle = textStyle;
            }
        }

        // 5. Обробка кліків (Відкриття меню)
        private void GridJournal_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (LessonInfoPopup.Visibility == Visibility.Visible) return;
            if (GradePopup.IsOpen) return;
            if (gridJournal.IsHitTestVisible == false) return;

            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader))
                dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);

            if (dep == null) return;

            // Клік по заголовку (Дата)
            if (dep is DataGridColumnHeader header)
            {
                string sortPath = header.Column?.SortMemberPath;
                if (!string.IsNullOrEmpty(sortPath) && _columnLessonMap.ContainsKey(sortPath))
                {
                    int lessonId = _columnLessonMap[sortPath];
                    ShowLessonInfo(lessonId, header.Content.ToString());
                }
            }
            // Клік по клітинці (Оцінка)
            else if (dep is DataGridCell cell)
            {
                string colName = "";
                if (cell.Column is DataGridBoundColumn boundCol)
                {
                    var binding = boundCol.Binding as System.Windows.Data.Binding;
                    if (binding != null) colName = binding.Path.Path;
                }

                if (colName == "Учень" || colName == "StudentID" || string.IsNullOrEmpty(colName)) return;

                cell.IsSelected = true;
                _activeRow = cell.DataContext as DataRowView;
                _activeColName = colName;

                if (_activeRow != null) OpenPopup();
            }
        }

        // 6. Робота з вікнами
        private void OpenPopup()
        {
            UIBlocker.Visibility = Visibility.Visible;
            gridJournal.IsHitTestVisible = false;
            TopPanel.IsEnabled = false;
            GradePopup.IsOpen = true;
        }

        private void ClosePopup()
        {
            GradePopup.IsOpen = false;
            if (LessonInfoPopup.Visibility != Visibility.Visible)
            {
                UIBlocker.Visibility = Visibility.Collapsed;
                gridJournal.IsHitTestVisible = true;
                TopPanel.IsEnabled = true;
                gridJournal.UnselectAll();
            }
        }

        private void UIBlocker_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClosePopup();
            LessonInfoPopup.Visibility = Visibility.Collapsed;
        }

        // 7. Обробка вибору оцінки
        private void GradeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                ClosePopup();
                SetGradeToCell(btn.Content.ToString());
            }
        }


        // 9. Робота з Meet-посиланням

        private void BtnMeet_Click(object sender, RoutedEventArgs e)
        {
            // Якщо користувач клікає, і посилання встановлено - відкриваємо його
            if (_meetLink.StartsWith("http"))
            {
                try
                {
                    // Відкриває посилання у браузері за замовчуванням
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_meetLink) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Неможливо відкрити посилання: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, встановіть посилання на Meet (правий клік або 'Налаштування' у попапі).", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                OpenMeetLinkPopup();
            }
        }

        private void BtnMeet_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Запобігаємо спрацьовуванню звичайного кліку
            OpenMeetLinkPopup();
        }

        private void OpenMeetLinkPopup()
        {
            UIBlocker.Visibility = Visibility.Visible;
            MeetLinkPopup.Visibility = Visibility.Visible;
            gridJournal.IsHitTestVisible = false;
            TopPanel.IsEnabled = false;

            txtMeetLink.Text = _meetLink == "https://meet.google.com/" ? "" : _meetLink;
            txtMeetLink.Focus();
        }

        private void CloseMeetLinkPopup_Click(object sender, RoutedEventArgs e)
        {
            MeetLinkPopup.Visibility = Visibility.Collapsed;
            UIBlocker.Visibility = Visibility.Collapsed;
            gridJournal.IsHitTestVisible = true;
            TopPanel.IsEnabled = true;
        }

        private void BtnSaveMeetLink_Click(object sender, RoutedEventArgs e)
        {
            string newLink = txtMeetLink.Text.Trim();

            if (string.IsNullOrEmpty(newLink))
            {
                _meetLink = "https://meet.google.com/";
            }
            else if (!newLink.StartsWith("http"))
            {
                MessageBox.Show("Посилання повинно починатися з http:// або https://", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                _meetLink = newLink;
            }

            // Оновлення ToolTip для інформування користувача
            BtnMeet.ToolTip = $"Перейти до відеозв'язку: {_meetLink}";

            // ТУТ МАЄ БУТИ ЛОГІКА ЗБЕРІГАННЯ ЗВ'ЯЗКУ В БАЗІ ДАНИХ АБО КОНФІГУРАЦІЇ

            MessageBox.Show("Посилання Meet збережено!", "Збереження");
            CloseMeetLinkPopup_Click(null, null);
        }




        // === ЕКСПОРТ ЖУРНАЛУ В PDF ===
        // === ОНОВЛЕНИЙ ДРУК В PDF (З ВИБОРОМ КОЛЬОРУ) ===
        // === ДРУК У PDF (ОНОВЛЕНИЙ РОЗМІР) ===
        // === ЕКСПОРТ У PDF (ВИПРАВЛЕНИЙ) ===
        private void BtnExportJournalPDF_Click(object sender, RoutedEventArgs e)
        {
            if (gridJournal.Items.Count == 0) { MessageBox.Show("Журнал порожній.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            MessageBoxResult result = MessageBox.Show(
                "Зберегти у кольорі?\n\n[Так] - Кольоровий\n[Ні] - Чорно-білий",
                "Експорт PDF", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel) return;
            bool isColor = (result == MessageBoxResult.Yes);

            PrintDialog printDialog = new PrintDialog();

            // Спробуємо запропонувати альбомну орієнтацію в діалозі (якщо драйвер підтримує)
            if (printDialog.PrintTicket != null)
            {
                try { printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape; } catch { }
            }

            if (printDialog.ShowDialog() == true)
            {
                try
                {
                    // 1. Отримуємо дані
                    DataTable dtGrades = ((DataView)gridJournal.ItemsSource).ToTable();
                    DataTable dtLessons = ((DataView)gridTopics.ItemsSource).ToTable();

                    // 2. Створюємо документ
                    FlowDocument doc = CreateFlowDocument(dtGrades, dtLessons, isColor);

                    // 3. НАЛАШТУВАННЯ АЛЬБОМНОЇ ОРІЄНТАЦІЇ (ГОРИЗОНТАЛЬНО)
                    // Отримуємо розміри, які дає принтер
                    double paperW = printDialog.PrintableAreaWidth;
                    double paperH = printDialog.PrintableAreaHeight;

                    // Якщо принтер дає "портретні" розміри (ширина менша за висоту), 
                    // ми міняємо їх місцями, щоб документ будувався горизонтально
                    if (paperW < paperH)
                    {
                        doc.PageWidth = paperH;  // Ширина стає великою
                        doc.PageHeight = paperW; // Висота стає меншою
                    }
                    else
                    {
                        doc.PageWidth = paperW;
                        doc.PageHeight = paperH;
                    }

                    doc.PagePadding = new Thickness(40);
                    doc.ColumnGap = 0;
                    doc.ColumnWidth = double.PositiveInfinity; // Запобігає розбиттю на колонки газети

                    // 4. Друк
                    IDocumentPaginatorSource idpSource = doc;
                    printDialog.PrintDocument(idpSource.DocumentPaginator, $"Журнал {cmbClasses.Text}");
                }
                catch (Exception ex) { MessageBox.Show("Помилка друку: " + ex.Message); }
            }
        }
        // === ГЕНЕРАЦІЯ ДОКУМЕНТА ===
        // Метод створення красивого документу
        // Цей код замінює існуючий метод CreateFlowDocument
        private FlowDocument CreateFlowDocument(DataTable dtGrades, DataTable dtLessons, bool isColor)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Segoe UI");
            doc.FontSize = 12;

            // --- ЗАГОЛОВКИ ---
            Paragraph title = new Paragraph(new Run($"ЖУРНАЛ ОБЛІКУ УСПІШНОСТІ"));
            title.FontSize = 24;
            title.FontWeight = FontWeights.Bold;
            title.TextAlignment = TextAlignment.Center;
            title.Foreground = isColor ? new SolidColorBrush(Color.FromRgb(16, 42, 67)) : Brushes.Black;
            doc.Blocks.Add(title);

            string info = $"Клас: {cmbClasses.Text}   |   Предмет: {cmbSubjects.Text}   |   Дата друку: {DateTime.Now:dd.MM.yyyy}";
            Paragraph subTitle = new Paragraph(new Run(info));
            subTitle.FontSize = 14;
            subTitle.TextAlignment = TextAlignment.Center;
            subTitle.Margin = new Thickness(0, 0, 0, 20);
            doc.Blocks.Add(subTitle);

            // --- ЛОГІКА РОЗБИТТЯ ЖУРНАЛУ НА КІЛЬКА ТАБЛИЦЬ ---

            // 1. Фіксовані колонки (№, Учень)
            List<DataColumn> fixedCols = new List<DataColumn>()
            {
                dtGrades.Columns["№"],
                dtGrades.Columns["Учень"]
            };

            // 2. Колонки уроків (починаємо з індексу 3, оскільки 0-StudentID, 1-№, 2-Учень)
            List<DataColumn> lessonCols = new List<DataColumn>();
            for (int i = 3; i < dtGrades.Columns.Count; i++)
            {
                lessonCols.Add(dtGrades.Columns[i]);
            }

            // Розрахунок, скільки колонок поміститься (приблизні значення для альбомного А4)
            const double FIXED_WIDTH = 30 + 200;
            const double LESSON_COL_WIDTH = 45;
            const double MAX_USABLE_WIDTH = 1000;

            int maxLessonsPerTable = (int)Math.Floor((MAX_USABLE_WIDTH - FIXED_WIDTH) / LESSON_COL_WIDTH);
            if (maxLessonsPerTable < 1) maxLessonsPerTable = 1;

            int lessonIndex = 0;
            int tableCounter = 1;

            // Цикл для створення таблиць-частин
            while (lessonIndex < lessonCols.Count)
            {
                Table tableGrades = new Table();
                tableGrades.CellSpacing = 0;
                tableGrades.BorderThickness = new Thickness(1);
                tableGrades.BorderBrush = Brushes.Black;

                if (tableCounter > 1)
                {
                    // Розрив сторінки для наступної частини
                    Paragraph breakPar = new Paragraph();
                    breakPar.BreakPageBefore = true;
                    doc.Blocks.Add(breakPar);

                    Paragraph partTitle = new Paragraph(new Run($"ЖУРНАЛ ОБЛІКУ УСПІШНОСТІ (Продовження - Частина {tableCounter})"));
                    partTitle.FontSize = 16;
                    partTitle.FontWeight = FontWeights.Bold;
                    partTitle.TextAlignment = TextAlignment.Center;
                    partTitle.Margin = new Thickness(0, 10, 0, 10);
                    partTitle.Foreground = isColor ? new SolidColorBrush(Color.FromRgb(16, 42, 67)) : Brushes.Black;
                    doc.Blocks.Add(partTitle);
                }

                // --- НАЛАШТУВАННЯ ШИРИНИ ---
                tableGrades.Columns.Add(new TableColumn() { Width = new GridLength(30) });  // №
                tableGrades.Columns.Add(new TableColumn() { Width = new GridLength(200) }); // Учень

                int currentLessons = 0;
                for (int i = lessonIndex; i < lessonCols.Count && currentLessons < maxLessonsPerTable; i++)
                {
                    tableGrades.Columns.Add(new TableColumn() { Width = new GridLength(LESSON_COL_WIDTH) });
                    currentLessons++;
                }

                // --- ШАПКА ---
                TableRowGroup headerGroup = new TableRowGroup();
                TableRow headerRow = new TableRow();
                if (isColor) headerRow.Background = new SolidColorBrush(Color.FromRgb(225, 245, 254));

                // 1. Фіксовані заголовки
                foreach (DataColumn col in fixedCols)
                {
                    TableCell cell = CreateHeaderCell(col.ColumnName, isColor, false);
                    headerRow.Cells.Add(cell);
                }

                // 2. Заголовки уроків
                int currentLessonHeader = 0;
                for (int i = lessonIndex; i < lessonCols.Count && currentLessonHeader < maxLessonsPerTable; i++)
                {
                    string headerText = lessonCols[i].ColumnName;
                    if (_columnHeaderMap.ContainsKey(headerText)) headerText = _columnHeaderMap[headerText];
                    TableCell cell = CreateHeaderCell(headerText, isColor, headerText.Length > 6);
                    headerRow.Cells.Add(cell);
                    currentLessonHeader++;
                }
                headerGroup.Rows.Add(headerRow);
                tableGrades.RowGroups.Add(headerGroup);

                // --- ДАНІ ---
                TableRowGroup dataGroup = new TableRowGroup();
                foreach (DataRow row in dtGrades.Rows)
                {
                    TableRow dataRow = new TableRow();

                    // 1. Фіксовані дані
                    AddCellToRow(dataRow, row["№"].ToString(), TextAlignment.Center, isColor, "№");
                    AddCellToRow(dataRow, row["Учень"].ToString(), TextAlignment.Left, isColor, "Учень");

                    // 2. Дані уроків
                    for (int i = lessonIndex; i < lessonCols.Count && (i - lessonIndex) < maxLessonsPerTable; i++)
                    {
                        DataColumn col = lessonCols[i];
                        AddCellToRow(dataRow, row[col.ColumnName].ToString(), TextAlignment.Center, isColor, col.ColumnName);
                    }
                    dataGroup.Rows.Add(dataRow);
                }
                tableGrades.RowGroups.Add(dataGroup);
                doc.Blocks.Add(tableGrades);

                // Переходимо до наступного блоку уроків
                lessonIndex += maxLessonsPerTable;
                tableCounter++;
            }


            // --- ЗМІСТ УРОКІВ ТА ДЗ (ЗАВЖДИ З НОВОЇ СТОРІНКИ З ПОВТОРЕННЯМ ШАПКИ) ---



            Paragraph pageBreak = new Paragraph();
            pageBreak.BreakPageBefore = true;
            doc.Blocks.Add(pageBreak);

            Paragraph title2 = new Paragraph(new Run("ЗМІСТ УРОКІВ ТА ДОМАШНЄ ЗАВДАННЯ"));
            title2.FontSize = 20;
            title2.FontWeight = FontWeights.Bold;
            title2.TextAlignment = TextAlignment.Center;
            title2.Margin = new Thickness(0, 0, 0, 20);
            title2.KeepWithNext = true;
            doc.Blocks.Add(title2);

            Table tableLessons = new Table();
            tableLessons.CellSpacing = 0;
            tableLessons.BorderThickness = new Thickness(1);
            tableLessons.BorderBrush = Brushes.Black;


            // *** ОСТАТОЧНЕ ВИПРАВЛЕННЯ: ФІКСОВАНА ШИРИНА (АБСОЛЮТНІ ЗНАЧЕННЯ) ***
            // Сумарна ширина: 100 + 450 + 450 = 1000 (повинна поміститися на A4 landscape)
            tableLessons.Columns.Add(new TableColumn() { Width = new GridLength(100) }); // 1. Дата
            tableLessons.Columns.Add(new TableColumn() { Width = new GridLength(450) }); // 2. Тема уроку
            tableLessons.Columns.Add(new TableColumn() { Width = new GridLength(450) }); // 3. Домашнє завдання
            // **********************************

            TableRowGroup lhg = new TableRowGroup();
            lhg.Name = "HeaderGroupLessons";


            TableRow lhr = new TableRow();
            if (isColor) lhr.Background = new SolidColorBrush(Color.FromRgb(238, 238, 238));
            string[] hds = { "Дата", "Тема уроку", "Домашнє завдання" };

            // Ми вже видалили дублюючий AddCellToRow і встановили GridLength.Star

            for (int i = 0; i < hds.Length; i++)
            {
                // *** ВИПРАВЛЕННЯ 1: Створюємо Paragraph, а не TextBlock, і використовуємо Run ***

                Paragraph headerParagraph = new Paragraph(new Run(hds[i]));
                headerParagraph.TextAlignment = TextAlignment.Center;

                // Це властивість FlowDocument, яка дозволяє тексту переноситися
                headerParagraph.TextAlignment = TextAlignment.Center;
                headerParagraph.FontWeight = FontWeights.Bold;


                TableCell cell = new TableCell(headerParagraph);

                cell.Padding = new Thickness(10);
                cell.BorderThickness = new Thickness(0, 0, 1, 1);
                cell.BorderBrush = Brushes.Black;
                cell.TextAlignment = TextAlignment.Center;

                // *** ВИДАЛЕНО ВЛАСТИВОСТІ MinHeight та MinWidth (викликали помилки 2 та 3) ***

                lhr.Cells.Add(cell);
            }
            // *************************************************************************************

            lhg.Rows.Add(lhr);
            tableLessons.RowGroups.Add(lhg);

            // ВИПРАВЛЕННЯ 2: Оголошуємо ldg
            TableRowGroup ldg = new TableRowGroup();
            foreach (DataRow row in dtLessons.Rows)
            {
                TableRow r = new TableRow();

                // *** ВИПРАВЛЕННЯ: Тепер використовуємо стандартизований метод AddCellToRow з 5 аргументами ***
                // 1. Дата: (Не використовує логіку кольору)
                AddCellToRow(r, row["Date"].ToString(), TextAlignment.Center, false, "Date");
                // 2. Тема: (Не використовує логіку кольору)
                AddCellToRow(r, row["Topic"].ToString(), TextAlignment.Left, false, "Topic");
                // 3. Домашнє завдання: (Не використовує логіку кольору)
                AddCellToRow(r, row["Homework"].ToString(), TextAlignment.Left, false, "Homework");
                // ****************************************************************************************
                ldg.Rows.Add(r);
            }
            tableLessons.RowGroups.Add(ldg); // Дані додаються ДРУГИМИ
            doc.Blocks.Add(tableLessons);

            return doc;
        }

        // Нові/Модифіковані допоміжні методи для роботи логіки розбиття PDF

        private TableCell CreateHeaderCell(string headerText, bool isColor, bool isSmallFont)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(headerText)));
            cell.Padding = new Thickness(2);
            cell.BorderThickness = new Thickness(0, 0, 1, 1);
            cell.BorderBrush = Brushes.Black;
            cell.FontWeight = FontWeights.Bold;
            cell.TextAlignment = TextAlignment.Center;
            if (isSmallFont) cell.FontSize = 10;
            return cell;
        }

        private void AddCellToRow(TableRow row, string text, TextAlignment align, bool isColor, string colName)
        {
            Brush foreColor = Brushes.Black;
            if (isColor && colName != "Учень" && colName != "№")
            {
                foreColor = new GradeColorConverter().GetColor(text);
            }

            TableCell cell = new TableCell(new Paragraph(new Run(text) { Foreground = foreColor }));
            cell.Padding = new Thickness(4);
            cell.BorderThickness = new Thickness(0, 0, 1, 1);
            cell.BorderBrush = Brushes.Black;
            cell.TextAlignment = align;
            row.Cells.Add(cell);
        }

        // Це оригінальний допоміжний метод для другої таблиці (тем), 
        // який я перейменував, щоб уникнути конфлікту з новим AddCellToRow
        private void AddCellToRowOriginal(TableRow row, string text, TextAlignment align)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text)));
            cell.Padding = new Thickness(8); // Більші відступи для читабельності
            cell.BorderThickness = new Thickness(0, 0, 1, 1);
            cell.BorderBrush = Brushes.Black;
            cell.TextAlignment = align;
            row.Cells.Add(cell);
        }

        private void AddCellToRow(TableRow row, string text, TextAlignment align)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text)));
            cell.Padding = new Thickness(8); // Більші відступи для читабельності
            cell.BorderThickness = new Thickness(0, 0, 1, 1);
            cell.BorderBrush = Brushes.Black;
            cell.TextAlignment = align;
            row.Cells.Add(cell);
        }



        // === ІМПОРТ ПЛАНУ З CSV (EXCEL) ===
        // === ІМПОРТ ПЛАНУ З CSV (EXCEL) ===
        private void BtnImportPlan_Click(object sender, RoutedEventArgs e)
        {
            if (cmbClasses.SelectedValue == null || cmbSubjects.SelectedValue == null)
            {
                MessageBox.Show("Спочатку оберіть клас і предмет!");
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV файл (Excel)|*.csv",
                Title = "Оберіть файл календарного плану"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var lines = File.ReadAllLines(openFileDialog.FileName, Encoding.Default);

                    int classId = (int)cmbClasses.SelectedValue;
                    int subjId = (int)cmbSubjects.SelectedValue;
                    int importedCount = 0;

                    // 1. Отримуємо AssignmentID
                    string sqlAss = "SELECT AssignmentID FROM TeachingAssignments WHERE ClassID=@C AND SubjectID=@S";
                    SqlParameter[] paramsAss = {
                new SqlParameter("@C", classId),
                new SqlParameter("@S", subjId)
            };
                    DataTable dtAss = DatabaseHelper.GetDataTable(sqlAss, paramsAss);

                    if (dtAss.Rows.Count == 0) { MessageBox.Show("Навантаження не знайдено!"); return; }
                    int assignId = (int)dtAss.Rows[0]["AssignmentID"];

                    // 2. Проходимо по файлу
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var parts = lines[i].Split(';');
                        if (parts.Length < 2) continue;

                        if (DateTime.TryParse(parts[0], out DateTime date))
                        {
                            string topic = "";
                            string hw = "";

                            if (parts.Length >= 4) { topic = parts[2]; hw = parts[3]; }
                            else if (parts.Length == 3) { topic = parts[1]; hw = parts[2]; }

                            topic = topic.Trim();
                            hw = hw.Trim();

                            // Перевіряємо існування уроку
                            string sqlCheck = "SELECT COUNT(*) FROM Lessons WHERE AssignmentID=@A AND LessonDate=@D";
                            SqlParameter[] paramsCheck = {
                        new SqlParameter("@A", assignId),
                        new SqlParameter("@D", date)
                    };
                            DataTable dtExists = DatabaseHelper.GetDataTable(sqlCheck, paramsCheck);
                            int exists = (int)dtExists.Rows[0][0];

                            if (exists > 0)
                            {
                                // UPDATE
                                string sqlUpd = "UPDATE Lessons SET LessonTopic=@T, Homework=@H WHERE AssignmentID=@A AND LessonDate=@D";
                                SqlParameter[] paramsUpd = {
                            new SqlParameter("@T", topic),
                            new SqlParameter("@H", hw),
                            new SqlParameter("@A", assignId),
                            new SqlParameter("@D", date)
                        };
                                DatabaseHelper.ExecuteQuery(sqlUpd, paramsUpd);
                            }
                            else
                            {
                                // INSERT
                                string sqlIns = "INSERT INTO Lessons (AssignmentID, LessonDate, LessonTopic, Homework, LessonTypeID) VALUES (@A, @D, @T, @H, 1)";
                                SqlParameter[] paramsIns = {
                            new SqlParameter("@A", assignId),
                            new SqlParameter("@D", date),
                            new SqlParameter("@T", topic),
                            new SqlParameter("@H", hw)
                        };
                                DatabaseHelper.ExecuteQuery(sqlIns, paramsIns);
                            }
                            importedCount++;
                        }
                    }
                    MessageBox.Show($"Успішно імпортовано {importedCount} уроків!", "Імпорт", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshJournal();
                }
                catch (Exception ex) { MessageBox.Show("Помилка імпорту: " + ex.Message); }
            }
        }
        // === ЕКСПОРТ ТЕМ У CSV (EXCEL) ===
        private void BtnExportTopics_Click(object sender, RoutedEventArgs e)
        {
            if (gridTopics.Items.Count == 0) { MessageBox.Show("Немає даних для експорту."); return; }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV файл (*.csv)|*.csv",
                FileName = $"Теми_{cmbClasses.Text}_{cmbSubjects.Text}_{DateTime.Now:dd.MM}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Дата;№;Тема уроку;Домашнє завдання");

                    // Беремо дані прямо з таблиці, яку бачимо
                    foreach (DataRowView row in gridTopics.Items)
                    {
                        string date = row["Date"].ToString();
                        string num = row["LessonNumber"].ToString();
                        string topic = row["Topic"].ToString().Replace(";", ","); // Заміна ; на , щоб не ламати CSV
                        string hw = row["Homework"].ToString().Replace(";", ",");

                        sb.AppendLine($"{date};{num};{topic};{hw}");
                    }

                    File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Файл успішно збережено!", "Експорт", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
            }
        }


        private void GradeButton_Clear_Click(object sender, RoutedEventArgs e)
        {
            ClosePopup();
            SetGradeToCell("");
        }

        private void SetGradeToCell(string value)
        {
            if (_activeRow == null || !_columnLessonMap.ContainsKey(_activeColName)) return;
            int studentId = (int)_activeRow["StudentID"];
            int lessonId = _columnLessonMap[_activeColName];

            try
            {
                // 1. Видалення
                string sqlDel = "DELETE FROM Grades WHERE StudentID=@S AND LessonID=@L";
                SqlParameter[] paramsDel = {
                    new SqlParameter("@S", studentId),
                    new SqlParameter("@L", lessonId)
                };
                DatabaseHelper.ExecuteQuery(sqlDel, paramsDel);

                // 2. Вставка (якщо є оцінка)
                if (!string.IsNullOrEmpty(value) && value != "X")
                {
                    string sqlIns = "INSERT INTO Grades (LessonID, StudentID, GradeValue) VALUES (@L, @S, @V)";
                    SqlParameter[] paramsIns = {
                        new SqlParameter("@S", studentId),
                        new SqlParameter("@L", lessonId),
                        new SqlParameter("@V", value)
                    };
                    DatabaseHelper.ExecuteQuery(sqlIns, paramsIns);
                }

                RefreshJournal();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        // 8. Створення та Редагування Уроку
        private void BtnAddColumn_Click(object sender, RoutedEventArgs e)
        {
            if (cmbClasses.SelectedValue == null || cmbSubjects.SelectedValue == null || datePicker.SelectedDate == null)
            {
                MessageBox.Show("Оберіть дату!");
                return;
            }

            _isNewLesson = true;
            _currentLessonId = 0;
            txtLessonDate.Text = datePicker.SelectedDate.Value.ToString("dd.MM.yyyy");
            txtLessonTopic.Text = "";
            txtLessonHW.Text = "";

            // Оновлюємо список НУШ під обраний предмет
            UpdateNushCombo();

            // Скидаємо вибір: Стандартний = Поточна (1), НУШ = Пусто
            cmbStandardType.SelectedValue = 1;
            cmbNushType.SelectedIndex = -1;

            OpenLessonWindow();
        }
        private void ShowLessonInfo(int lessonId, string dateStr)
        {
            // Скидаємо стани
            GradePopup.IsOpen = false;
            _isNewLesson = false;
            _currentLessonId = lessonId;

            try
            {
                // 1. Оновлюємо список НУШ під поточний предмет (щоб там були правильні ГР)
                UpdateNushCombo();

                using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SELECT LessonTopic, Homework, LessonTypeID, LessonDate FROM Lessons WHERE LessonID = @ID", con);
                    cmd.Parameters.AddWithValue("@ID", lessonId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtLessonDate.Text = Convert.ToDateTime(reader["LessonDate"]).ToString("dd.MM.yyyy");
                            txtLessonTopic.Text = reader["LessonTopic"] != DBNull.Value ? reader["LessonTopic"].ToString() : "";
                            txtLessonHW.Text = reader["Homework"] != DBNull.Value ? reader["Homework"].ToString() : "";

                            // Отримуємо ID типу
                            int typeId = reader["LessonTypeID"] != DBNull.Value ? (int)reader["LessonTypeID"] : 1;

                            // Логіка: Якщо ID < 13 — це звичайний урок, якщо >= 13 — це ГР (НУШ)
                            if (typeId < 13)
                            {
                                cmbStandardType.SelectedValue = typeId;
                                cmbNushType.SelectedIndex = -1; // Очищаємо вибір НУШ
                            }
                            else
                            {
                                cmbNushType.SelectedValue = typeId;
                                cmbStandardType.SelectedIndex = -1; // Очищаємо стандартний вибір
                            }

                            OpenLessonWindow();
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
        }


        private void OpenLessonWindow()
        {
            UIBlocker.Visibility = Visibility.Visible;
            LessonInfoPopup.Visibility = Visibility.Visible;
            gridJournal.IsHitTestVisible = false;
            TopPanel.IsEnabled = false;
        }

        private void CloseLessonInfo_Click(object sender, RoutedEventArgs e)
        {
            LessonInfoPopup.Visibility = Visibility.Collapsed;
            UIBlocker.Visibility = Visibility.Collapsed;
            gridJournal.IsHitTestVisible = true;
            TopPanel.IsEnabled = true;
            gridJournal.UnselectAll();
        }

        private void BtnSaveLesson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Визначаємо, який тип уроку обрано (Стандартний чи НУШ)
                int finalTypeId = 1; // За замовчуванням "Поточна"

                if (cmbNushType.SelectedValue != null)
                {
                    finalTypeId = (int)cmbNushType.SelectedValue;
                }
                else if (cmbStandardType.SelectedValue != null)
                {
                    finalTypeId = (int)cmbStandardType.SelectedValue;
                }

                if (_isNewLesson)
                {
                    // ОГОЛОШУЄМО assignId ТУТ, щоб її було видно у всьому блоці
                    int assignId = 0;

                    // Шукаємо існуючий зв'язок (AssignmentID)
                    string sqlFind = "SELECT AssignmentID FROM TeachingAssignments WHERE ClassID=@C AND SubjectID=@S";
                    SqlParameter[] paramsFind = {
                new SqlParameter("@C", cmbClasses.SelectedValue),
                new SqlParameter("@S", cmbSubjects.SelectedValue)
            };
                    DataTable dtAssign = DatabaseHelper.GetDataTable(sqlFind, paramsFind);

                    if (dtAssign.Rows.Count > 0)
                    {
                        assignId = (int)dtAssign.Rows[0]["AssignmentID"];
                    }
                    else
                    {
                        // Якщо зв'язку немає — створюємо його
                        string sqlCreate = "INSERT INTO TeachingAssignments (TeacherID, ClassID, SubjectID) VALUES (@TID, @C, @S); SELECT SCOPE_IDENTITY();";
                        SqlParameter[] paramsCreate = {
                    new SqlParameter("@TID", AppSession.CurrentUserId),
                    new SqlParameter("@C", cmbClasses.SelectedValue),
                    new SqlParameter("@S", cmbSubjects.SelectedValue)
                };

                        DataTable dtNewId = DatabaseHelper.GetDataTable(sqlCreate, paramsCreate);
                        if (dtNewId.Rows.Count > 0)
                            assignId = Convert.ToInt32(dtNewId.Rows[0][0]);
                    }

                    // Створюємо Урок (використовуємо assignId та finalTypeId)
                    string sqlIns = "INSERT INTO Lessons (AssignmentID, LessonDate, LessonTopic, Homework, LessonTypeID) VALUES (@AID, @Date, @T, @H, @Type)";
                    SqlParameter[] paramsIns = {
                new SqlParameter("@AID", assignId),
                new SqlParameter("@Date", datePicker.SelectedDate.Value),
                new SqlParameter("@T", txtLessonTopic.Text),
                new SqlParameter("@H", txtLessonHW.Text),
                new SqlParameter("@Type", finalTypeId)
            };
                    DatabaseHelper.ExecuteQuery(sqlIns, paramsIns);
                }
                else
                {
                    // Редагування існуючого уроку (використовуємо finalTypeId)
                    string sqlUpd = "UPDATE Lessons SET LessonTopic = @T, Homework = @H, LessonTypeID = @Type WHERE LessonID = @ID";
                    SqlParameter[] paramsUpd = {
                new SqlParameter("@T", txtLessonTopic.Text),
                new SqlParameter("@H", txtLessonHW.Text),
                new SqlParameter("@Type", finalTypeId),
                new SqlParameter("@ID", _currentLessonId)
            };
                    DatabaseHelper.ExecuteQuery(sqlUpd, paramsUpd);
                }

                MessageBox.Show("Збережено!");
                CloseLessonInfo_Click(null, null);
                RefreshJournal();
            }
            catch (Exception ex) { MessageBox.Show("Помилка збереження: " + ex.Message); }
        }
        private void BtnDeleteLesson_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLessonId == 0) return;
            if (MessageBox.Show("Видалити урок?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;

            try
            {
                // 1. Перевіряємо, чи є оцінки (не можна видаляти урок з оцінками)
                string sqlCheck = "SELECT COUNT(*) FROM Grades WHERE LessonID = @ID";
                SqlParameter[] paramsCheck = { new SqlParameter("@ID", _currentLessonId) };

                DataTable dtCheck = DatabaseHelper.GetDataTable(sqlCheck, paramsCheck);
                int count = 0;
                if (dtCheck.Rows.Count > 0) count = (int)dtCheck.Rows[0][0];

                if (count > 0)
                {
                    MessageBox.Show("Неможливо видалити урок з оцінками!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 2. Видаляємо
                string sqlDel = "DELETE FROM Lessons WHERE LessonID = @ID";
                // Створюємо новий параметр, бо попередній вже використаний
                SqlParameter[] paramsDel = { new SqlParameter("@ID", _currentLessonId) };
                DatabaseHelper.ExecuteQuery(sqlDel, paramsDel);

                MessageBox.Show("Урок видалено.");
                CloseLessonInfo_Click(null, null);
                RefreshJournal();
            }
            catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
        }

        // === ВИПРАВЛЕННЯ ПРОКРУТКИ (ПОВІЛЬНІША) ===
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;

            // Ділимо e.Delta на 3 (або 2, або 4), щоб зменшити швидкість
            // Чим більше число, тим повільніша прокрутка
            double scrollAmount = e.Delta / 3.0;

            scv.ScrollToVerticalOffset(scv.VerticalOffset - scrollAmount);
            e.Handled = true;
        }



        // Словник НУШ: [Назва предмету] -> [Список назв для ГР1, ГР2, ГР3, ГР4]
        private Dictionary<string, string[]> _nushTopics = new Dictionary<string, string[]>()
        {
            // МОВНО-ЛІТЕРАТУРНА ГАЛУЗЬ (Укр мова, Англійська, Література)
            { "Українська мова", new string[] { "Аудіювання (Усна взаємодія)", "Говоріння", "Читання", "Письмо" } },
            { "Англійська мова", new string[] { "Сприймання на слух (Аудіювання)", "Усна взаємодія (Говоріння)", "Сприймання тексту (Читання)", "Письмова взаємодія (Письмо)" } },
            { "Зарубіжна література", new string[] { "Взаємодія з текстом", "Усна взаємодія", "Письмова взаємодія", "Дослідження мовлення" } },

            // МАТЕМАТИЧНА ГАЛУЗЬ
            { "Математика", new string[] { "Числа, дії, вирази", "Геометричні фігури", "Робота з даними (Моделювання)", "Математичні задачі" } },
            { "Алгебра", new string[] { "Опрацювання даних", "Моделювання", "Критичне мислення", "Математична комунікація" } },

            // ПРИРОДНИЧА ГАЛУЗЬ
            { "Пізнаємо природу", new string[] { "Пізнання світу природи", "Робота з інформацією", "Дослідження природи", "Природа і я" } },
            { "Біологія", new string[] { "Знання про природу", "Дослідження", "Робота з даними", "Наукове мислення" } },

            // ГРОМАДЯНСЬКА ТА ІСТОРИЧНА
            { "Історія України", new string[] { "Орієнтування в історичному часі", "Орієнтування в просторі", "Робота з джерелами", "Суспільство і держава" } },

            // МИСТЕЦЬКА
            { "Мистецтво", new string[] { "Художньо-творча діяльність", "Сприймання та інтерпретація", "Комунікація через мистецтво", "Аналіз мистецтва" } },
            
            // ФІЗИЧНА КУЛЬТУРА
            { "Фізична культура", new string[] { "Розвиток фізичних якостей", "Рухова компетентність", "Ігрова діяльність", "Здоровий спосіб життя" } }
        };






    }













    // Конвертер кольорів
    // === ВИПРАВЛЕНИЙ КОНВЕРТЕР КОЛЬОРІВ ===
    public class GradeColorConverter : IValueConverter
    {
        // Цей метод тепер ПУБЛІЧНИЙ, щоб його можна було викликати і з коду для PDF
        public Brush GetColor(string input)
        {
            if (string.IsNullOrEmpty(input)) return Brushes.Black;
            input = input.Trim().ToUpper(); // Робимо великі літери

            switch (input)
            {
                // === НУШ (Рівні) ===
                case "В": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32")); // Високий (Темно-зелений)
                case "Д": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")); // Достатній (Зелений)
                case "С": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBC02D")); // Середній (Жовтий)
                case "П": return Brushes.Red; // Початковий (Червоний)

                // === СТАНДАРТНІ ОЦІНКИ ===
                case "1": case "2": return Brushes.Red;
                case "3": case "4": case "5": return Brushes.Orange;
                case "6": case "7": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBC02D"));
                case "8": case "9": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AFB42B"));
                case "10": case "11": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                case "12": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));

                // Відвідуваність
                case "Н": case "ХВ": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0"));

                default: return Brushes.Black;
            }
        }
        // Метод для XAML (викликає нашу логіку)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Brushes.Black;
            return GetColor(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


}