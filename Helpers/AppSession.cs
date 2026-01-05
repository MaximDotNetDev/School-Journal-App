using System;
using System.Security.Cryptography; // Додаємо для хешування
using System.Text;

// AppSession.cs
namespace SchoolJournalApp.Views
{
    public static class AppSession
    {
        // Рядок підключення, який ми вже використовуємо
        public static string ConnectionString = "";

        public static int CurrentUserId = 0;
        public static string CurrentUserName = "";

        // --- ДОДАЙТЕ ЦЕЙ РЯДОК: ---
        public static int CurrentRoleID = 0;
        // --------------------------

        // Допоміжний метод для хешування (залиште як є)
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}