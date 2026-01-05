using System;
using System.Collections.Generic; // Потрібно для List<SqlParameter>
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks; // Потрібно для async/await
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input; // Для курсорів
using System.Windows.Media;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using SchoolJournalApp.Services;

namespace SchoolJournalApp.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            // Завантаження даних теж робимо асинхронно, але у конструкторі викликаємо без await
            // (це нормально для ініціалізації, або можна перенести в подію Loaded)
            _ = LoadInitialDataAsync();

            // Дати за замовчуванням
            dateFrom.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dateTo.SelectedDate = DateTime.Now;
        }

        // --- 1. Завантаження початкових даних (Асинхронно) ---
        private async Task LoadInitialDataAsync()
        {
            try
            {
                string sqlClasses;
                List<SqlParameter> parameters = new List<SqlParameter>();

                if (AppSession.CurrentRoleID == 1 || AppSession.CurrentRoleID == 2)
                {
                    sqlClasses = "SELECT ClassID, ClassName FROM Classes ORDER BY GradeLevel, ClassName";
                }
                else
                {
                    sqlClasses = @"SELECT DISTINCT c.ClassID, c.ClassName 
                                   FROM Classes c
                                   LEFT JOIN TeachingAssignments ta ON c.ClassID = ta.ClassID
                                   WHERE ta.TeacherID = @TID OR c.HomeroomTeacherID = @TID
                                   ORDER BY c.ClassName";
                    parameters.Add(new SqlParameter("@TID", AppSession.CurrentUserId));
                }

                // Використовуємо наш швидкий Helper
                DataTable dtClass = await DatabaseHelper.GetDataTableAsync(sqlClasses, parameters.ToArray());

                cmbClasses.ItemsSource = dtClass.DefaultView;
                if (dtClass.Rows.Count > 0) cmbClasses.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show("Помилка ініціалізації: " + ex.Message); }
        }

        // --- 2. Вибір класу (Асинхронно) ---
        private async void CmbClasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbClasses.SelectedValue == null) return;
            int classId = (int)cmbClasses.SelectedValue;

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                // А. Учні
                string sqlStudents = "SELECT StudentID, LastName + ' ' + FirstName AS FullName FROM Students WHERE ClassID = @ID ORDER BY LastName";
                DataTable dtStudents = await DatabaseHelper.GetDataTableAsync(sqlStudents, new SqlParameter[] { new SqlParameter("@ID", classId) });

                cmbStudents.ItemsSource = dtStudents.DefaultView;
                cmbStudents.IsEnabled = true;

                // Б. Предмети
                await LoadSubjectsForReportAsync(classId);
            }
            catch { }
            finally { Mouse.OverrideCursor = null; }
        }

        private async Task LoadSubjectsForReportAsync(int classId)
        {
            try
            {
                string sql;
                List<SqlParameter> parameters = new List<SqlParameter>();
                bool showAll = (AppSession.CurrentRoleID == 1 || AppSession.CurrentRoleID == 2);

                if (!showAll)
                {
                    // Перевірка на класне керівництво (теж через Helper, але синхронно для простоти перевірки, або окремим запитом)
                    // Для швидкості зробимо припущення або окремий запит.
                    // Зробимо це правильно через Helper:
                    string sqlCheck = "SELECT COUNT(*) FROM Classes WHERE ClassID = @CID AND HomeroomTeacherID = @TID";
                    DataTable dtCheck = await DatabaseHelper.GetDataTableAsync(sqlCheck, new SqlParameter[] {
                        new SqlParameter("@CID", classId),
                        new SqlParameter("@TID", AppSession.CurrentUserId)
                    });

                    if (dtCheck.Rows.Count > 0 && (int)dtCheck.Rows[0][0] > 0) showAll = true;
                }

                if (showAll)
                {
                    sql = @"SELECT DISTINCT s.SubjectID, s.SubjectName FROM Subjects s 
                            JOIN TeachingAssignments ta ON s.SubjectID = ta.SubjectID 
                            WHERE ta.ClassID = @CID ORDER BY s.SubjectName";
                    parameters.Add(new SqlParameter("@CID", classId));
                }
                else
                {
                    sql = @"SELECT DISTINCT s.SubjectID, s.SubjectName FROM Subjects s 
                            JOIN TeachingAssignments ta ON s.SubjectID = ta.SubjectID 
                            WHERE ta.ClassID = @CID AND ta.TeacherID = @TID ORDER BY s.SubjectName";
                    parameters.Add(new SqlParameter("@CID", classId));
                    parameters.Add(new SqlParameter("@TID", AppSession.CurrentUserId));
                }

                DataTable dt = await DatabaseHelper.GetDataTableAsync(sql, parameters.ToArray());
                cmbSubjects.ItemsSource = dt.DefaultView;
                if (dt.Rows.Count > 0) cmbSubjects.SelectedIndex = 0;
            }
            catch { }
        }

        // ======================= ЗВІТИ (ASYNC + НОВІ ЗАПИТИ) =======================

        // 1. Рейтинг класу (Без кількості, тільки середній бал)
        private async void BtnReportClassRating_Click(object sender, RoutedEventArgs e)
        {
            if (cmbClasses.SelectedValue == null || cmbSubjects.SelectedValue == null) { MessageBox.Show("Оберіть Клас та Предмет!"); return; }

            string sql = @"SELECT 
                                s.LastName + ' ' + s.FirstName AS [Учень], 
                                CAST(AVG(TRY_CAST(g.GradeValue AS DECIMAL(4,2))) AS DECIMAL(4,1)) AS [Середній Бал] 
                           FROM Students s 
                           LEFT JOIN Grades g ON s.StudentID = g.StudentID 
                           LEFT JOIN Lessons l ON g.LessonID = l.LessonID 
                           LEFT JOIN TeachingAssignments ta ON l.AssignmentID = ta.AssignmentID 
                           WHERE s.ClassID = @ClassID 
                             AND ta.SubjectID = @SubjID 
                             AND (l.LessonDate BETWEEN @D1 AND @D2) 
                           GROUP BY s.LastName, s.FirstName 
                           ORDER BY [Середній Бал] DESC";

            await LoadReportAsync(sql, true, false, true);
        }

        // 2. Пропуски
        private async void BtnReportClassAbsence_Click(object sender, RoutedEventArgs e)
        {
            if (cmbClasses.SelectedValue == null) { MessageBox.Show("Оберіть Клас!"); return; }
            string sql = @"SELECT s.LastName + ' ' + s.FirstName AS [Учень], COUNT(CASE WHEN g.GradeValue = 'н' OR g.GradeValue = 'Н' THEN 1 END) AS [Пропуски (Н)], COUNT(CASE WHEN g.GradeValue = 'хв' THEN 1 END) AS [Хвороба (ХВ)] FROM Students s LEFT JOIN Grades g ON s.StudentID = g.StudentID LEFT JOIN Lessons l ON g.LessonID = l.LessonID WHERE s.ClassID = @ClassID AND (l.LessonDate BETWEEN @D1 AND @D2) GROUP BY s.LastName, s.FirstName ORDER BY [Пропуски (Н)] DESC";
            await LoadReportAsync(sql, false, false, true);
        }

        // 3. Важливі оцінки (Тематичні, ГР, Семестрові) - замість "Контрольних"
        private async void BtnReportControlWorks_Click(object sender, RoutedEventArgs e)
        {
            if (cmbClasses.SelectedValue == null || cmbSubjects.SelectedValue == null) { MessageBox.Show("Оберіть Клас та Предмет!"); return; }

            string sql = @"SELECT 
                                l.LessonDate AS [Дата], 
                                gt.TypeName AS [Тип роботи], 
                                s.LastName + ' ' + s.FirstName AS [Учень], 
                                g.GradeValue AS [Оцінка] 
                           FROM Grades g 
                           JOIN Students s ON g.StudentID = s.StudentID 
                           JOIN Lessons l ON g.LessonID = l.LessonID 
                           JOIN GradeTypes gt ON l.LessonTypeID = gt.GradeTypeID
                           JOIN TeachingAssignments ta ON l.AssignmentID = ta.AssignmentID 
                           WHERE s.ClassID = @ClassID 
                             AND ta.SubjectID = @SubjID 
                             AND l.LessonTypeID >= 8 
                             AND (l.LessonDate BETWEEN @D1 AND @D2) 
                           ORDER BY l.LessonDate DESC, s.LastName";

            await LoadReportAsync(sql, true, false, true);
        }

        // 4. Табель учня (Без кількості)
        private async void BtnReportStudentAll_Click(object sender, RoutedEventArgs e)
        {
            if (cmbStudents.SelectedValue == null) { MessageBox.Show("Оберіть Учня!"); return; }

            string sql = @"SELECT 
                                sub.SubjectName AS [Предмет], 
                                CAST(AVG(TRY_CAST(g.GradeValue AS DECIMAL(4,2))) AS DECIMAL(4,1)) AS [Середній Бал] 
                           FROM Grades g 
                           JOIN Lessons l ON g.LessonID = l.LessonID 
                           JOIN TeachingAssignments ta ON l.AssignmentID = ta.AssignmentID 
                           JOIN Subjects sub ON ta.SubjectID = sub.SubjectID 
                           WHERE g.StudentID = @StudID 
                             AND (l.LessonDate BETWEEN @D1 AND @D2) 
                             AND TRY_CAST(g.GradeValue AS DECIMAL(4,2)) IS NOT NULL 
                           GROUP BY sub.SubjectName 
                           ORDER BY sub.SubjectName";

            await LoadReportAsync(sql, false, true, true);
        }

        // 5. Детально по предмету
        private async void BtnReportStudentSubject_Click(object sender, RoutedEventArgs e)
        {
            if (cmbStudents.SelectedValue == null || cmbSubjects.SelectedValue == null) { MessageBox.Show("Оберіть Учня та Предмет!"); return; }
            string sql = @"SELECT l.LessonDate AS [Дата], l.LessonTopic AS [Тема], g.GradeValue AS [Оцінка] FROM Grades g JOIN Lessons l ON g.LessonID = l.LessonID JOIN TeachingAssignments ta ON l.AssignmentID = ta.AssignmentID WHERE g.StudentID = @StudID AND ta.SubjectID = @SubjID AND (l.LessonDate BETWEEN @D1 AND @D2) ORDER BY l.LessonDate DESC";
            await LoadReportAsync(sql, true, true, true);
        }

        // --- Універсальний асинхронний метод завантаження звіту ---
        private async Task LoadReportAsync(string query, bool needSubject = false, bool needStudent = false, bool needDate = false)
        {
            // Блокуємо інтерфейс візуально
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                List<SqlParameter> parameters = new List<SqlParameter>();

                if (cmbClasses.SelectedValue != null)
                    parameters.Add(new SqlParameter("@ClassID", cmbClasses.SelectedValue));

                if (needSubject)
                    parameters.Add(new SqlParameter("@SubjID", cmbSubjects.SelectedValue));

                if (needStudent)
                    parameters.Add(new SqlParameter("@StudID", cmbStudents.SelectedValue));

                if (needDate)
                {
                    if (dateFrom.SelectedDate == null || dateTo.SelectedDate == null) { MessageBox.Show("Оберіть період дат!"); return; }
                    parameters.Add(new SqlParameter("@D1", dateFrom.SelectedDate.Value));
                    parameters.Add(new SqlParameter("@D2", dateTo.SelectedDate.Value));
                }

                // Асинхронний запит
                DataTable dt = await DatabaseHelper.GetDataTableAsync(query, parameters.ToArray());

                gridReports.ItemsSource = null;
                gridReports.ItemsSource = dt.DefaultView;
                gridReports.AlternationCount = 2;

                if (dt.Rows.Count == 0) MessageBox.Show("Записів не знайдено за цей період.");
            }
            catch (Exception ex)
            {
                // Ігноруємо помилки про Announcements, якщо вони раптом виникають
                if (!ex.Message.Contains("Announcements")) MessageBox.Show("Помилка звіту: " + ex.Message);
            }
            finally
            {
                // Повертаємо курсор
                Mouse.OverrideCursor = null;
            }
        }

        // ======================= ЕКСПОРТ =======================
        // Експорт працює з уже завантаженими даними в DataGrid, тому тут async не критичний, 
        // але самі операції запису файлу швидкі.

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (gridReports.ItemsSource == null) { MessageBox.Show("Немає даних!"); return; }
            SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "CSV файл (Excel)|*.csv", FileName = $"Звіт_{DateTime.Now:yyyy-MM-dd}.csv" };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var dt = ((DataView)gridReports.ItemsSource).ToTable();
                    StringBuilder sb = new StringBuilder();

                    // Заголовки
                    foreach (DataColumn col in dt.Columns) sb.Append(col.ColumnName + ";");
                    sb.Remove(sb.Length - 1, 1); sb.AppendLine();

                    // Дані
                    foreach (DataRow row in dt.Rows)
                    {
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            string val = row[i].ToString();
                            if (row[i] is DateTime dateVal) val = dateVal.ToString("dd.MM.yyyy");
                            // Замінюємо крапки на коми для чисел, якщо Excel український, або навпаки
                            // Але для CSV краще просто записати як текст
                            sb.Append(val + ";");
                        }
                        sb.Remove(sb.Length - 1, 1); sb.AppendLine();
                    }
                    File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Успішно збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
            }
        }

        private void BtnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (gridReports.ItemsSource == null) { MessageBox.Show("Немає даних!"); return; }

            MessageBoxResult result = MessageBox.Show(
                "Зберегти у кольорі (як на екрані)?\n\n[Так] - Кольоровий\n[Ні] - Чорно-білий (офіційний)",
                "Вибір формату",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel) return;
            bool isColor = (result == MessageBoxResult.Yes);

            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                FlowDocument doc = CreateFlowDocument(((DataView)gridReports.ItemsSource).ToTable(), isColor);

                doc.PageHeight = printDialog.PrintableAreaHeight;
                doc.PageWidth = printDialog.PrintableAreaWidth;
                doc.PagePadding = new Thickness(50);
                doc.ColumnGap = 0;
                doc.ColumnWidth = printDialog.PrintableAreaWidth;

                IDocumentPaginatorSource idpSource = doc;
                printDialog.PrintDocument(idpSource.DocumentPaginator, "Звіт зі школи");
            }
        }

        // Генерація PDF
        private FlowDocument CreateFlowDocument(DataTable dt, bool isColor)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Segoe UI");

            Paragraph title = new Paragraph(new Run("ЗВІТ УСПІШНОСТІ"));
            title.FontSize = 24;
            title.FontWeight = FontWeights.Bold;
            title.TextAlignment = TextAlignment.Center;
            title.Foreground = isColor ? new SolidColorBrush(Color.FromRgb(16, 42, 67)) : Brushes.Black;
            doc.Blocks.Add(title);

            string info = "";
            if (cmbClasses.SelectedValue != null) info += $"Клас: {cmbClasses.Text}   ";
            if (cmbSubjects.SelectedValue != null) info += $"|   Предмет: {cmbSubjects.Text}   ";
            if (cmbStudents.SelectedValue != null && cmbStudents.IsEnabled) info += $"|   Учень: {cmbStudents.Text}   ";

            Paragraph subTitle = new Paragraph(new Run(info));
            subTitle.FontSize = 14;
            subTitle.TextAlignment = TextAlignment.Center;
            subTitle.Margin = new Thickness(0, 0, 0, 10);
            doc.Blocks.Add(subTitle);

            Paragraph period = new Paragraph(new Run($"Період: {dateFrom.SelectedDate:dd.MM.yyyy} — {dateTo.SelectedDate:dd.MM.yyyy}"));
            period.FontSize = 12;
            period.FontStyle = FontStyles.Italic;
            period.TextAlignment = TextAlignment.Center;
            period.Margin = new Thickness(0, 0, 0, 20);
            doc.Blocks.Add(period);

            Table table = new Table();
            table.CellSpacing = 0;
            table.BorderThickness = new Thickness(1);
            table.BorderBrush = Brushes.Black;

            int colCount = dt.Columns.Count;
            for (int i = 0; i < colCount; i++)
                table.Columns.Add(new TableColumn());

            TableRowGroup headerGroup = new TableRowGroup();
            TableRow headerRow = new TableRow();

            if (isColor) headerRow.Background = new SolidColorBrush(Color.FromRgb(225, 245, 254));

            foreach (DataColumn col in dt.Columns)
            {
                TableCell cell = new TableCell(new Paragraph(new Run(col.ColumnName)));
                cell.Padding = new Thickness(5);
                cell.BorderThickness = new Thickness(0, 0, 1, 1);
                cell.BorderBrush = Brushes.Black;
                cell.FontWeight = FontWeights.Bold;
                cell.TextAlignment = TextAlignment.Center;
                headerRow.Cells.Add(cell);
            }
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            TableRowGroup dataGroup = new TableRowGroup();
            foreach (DataRow row in dt.Rows)
            {
                TableRow dataRow = new TableRow();
                for (int i = 0; i < colCount; i++)
                {
                    string text = row[i].ToString();
                    if (row[i] is DateTime d) text = d.ToString("dd.MM.yyyy");

                    Brush foreColor = Brushes.Black;
                    if (isColor) foreColor = GetColorForGrade(text);

                    TableCell cell = new TableCell(new Paragraph(new Run(text) { Foreground = foreColor }));
                    cell.Padding = new Thickness(5);
                    cell.BorderThickness = new Thickness(0, 0, 1, 1);
                    cell.BorderBrush = Brushes.Black;
                    cell.TextAlignment = TextAlignment.Center;

                    dataRow.Cells.Add(cell);
                }
                dataGroup.Rows.Add(dataRow);
            }
            table.RowGroups.Add(dataGroup);
            doc.Blocks.Add(table);

            return doc;
        }

        private Brush GetColorForGrade(string grade)
        {
            grade = grade.Trim();
            switch (grade)
            {
                case "1": case "2": return Brushes.Red;
                case "3": case "4": case "5": return Brushes.Orange;
                case "6": case "7": return new SolidColorBrush(Color.FromRgb(251, 192, 45));
                case "8": case "9": return new SolidColorBrush(Color.FromRgb(175, 180, 43));
                case "10": case "11": return Brushes.Green;
                case "12": return new SolidColorBrush(Color.FromRgb(46, 125, 50));
                case "н": case "хв": case "Н": return new SolidColorBrush(Color.FromRgb(21, 101, 192));
                default: return Brushes.Black;
            }
        }
    }
}