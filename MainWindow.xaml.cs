using System.Windows;
using System.Windows.Controls;
using SchoolJournalApp.Services;
using SchoolJournalApp.Views;

namespace SchoolJournalApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // При старті показуємо екран входу
            NavigateToLogin();
        }

        // Метод для перемикання на Екран входу
        public void NavigateToLogin()
        {
            SideMenu.Visibility = Visibility.Collapsed; // Ховаємо меню
            MainContent.Content = new LoginView(this);
        }

        // Метод для перемикання на Головну (після успішного входу)
        public void NavigateToDashboard()
        {
            SideMenu.Visibility = Visibility.Visible; // Показуємо меню
            MainContent.Content = new DashboardView();

            // !!! ВАЖЛИВО: Викликаємо перевірку прав при вході !!!
            ApplyPermissions();
        }

        // --- НОВИЙ МЕТОД: Налаштування видимості кнопок ---
        private void ApplyPermissions()
        {
            int roleId = AppSession.CurrentRoleID;

            // Скидаємо видимість (все показуємо, крім адмінських)
            btnAccessControl.Visibility = Visibility.Collapsed;

            if (btnTeachers != null) btnTeachers.Visibility = Visibility.Visible;
            if (btnStudents != null) btnStudents.Visibility = Visibility.Visible;
            if (btnClasses != null) btnClasses.Visibility = Visibility.Visible;
            if (btnSubjects != null) btnSubjects.Visibility = Visibility.Visible; // <--- Показуємо

            // АДМІНІСТРАТОРИ (1, 2)
            if (roleId == 1 || roleId == 2)
            {
                if (roleId == 1) btnAccessControl.Visibility = Visibility.Visible;
            }
            // ВЧИТЕЛЬ (3)
            else
            {
                // Ховаємо адміністративні розділи
                if (btnTeachers != null) btnTeachers.Visibility = Visibility.Collapsed;
                if (btnStudents != null) btnStudents.Visibility = Visibility.Collapsed;
                if (btnClasses != null) btnClasses.Visibility = Visibility.Collapsed;

                // !!! НОВЕ: ХОВАЄМО ПРЕДМЕТИ (Довідник) !!!
                if (btnSubjects != null) btnSubjects.Visibility = Visibility.Collapsed;
            }
        }
        // Навігація по кнопках меню
        private void NavToDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DashboardView();
        }

        private void NavToJournal_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new JournalView();
        }


        // Навігація до Учнів
        private void NavToStudents_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.StudentsView();
        }

        // Навігація до Вчителів
        private void NavToTeachers_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.TeachersView();
        }

        // НОВИЙ МЕТОД: Навігація до Класів
        private void NavToClasses_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.ClassesView();
        }

        private void NavToSubjects_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.SubjectsView();
        }


        private void NavToReports_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ReportsView();
        }

        // НОВИЙ ОБРОБНИК ДЛЯ КНОПКИ БЕЗПЕКИ
        private void NavToAccess_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.AccessControlView();
        }

        // Кнопка "Змінити БД" (Logout)
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            AppSession.ConnectionString = ""; // Очищаємо рядок
            AppSession.CurrentUserId = 0; // Скидаємо користувача
            AppSession.CurrentRoleID = 0; // Скидаємо роль
            NavigateToLogin();
        }

    }
}