using System;
using System.IO;

namespace SchoolJournalApp.Services
{
    public static class LocalSettings
    {
        // Шлях до файлу, де буде лежати логін (поруч із exe файлом)
        private static string FilePath = "saved_login.txt";

        // Метод для збереження
        public static void SaveLogin(string login)
        {
            try
            {
                File.WriteAllText(FilePath, login);
            }
            catch { /* Ігноруємо помилки доступу */ }
        }

        // Метод для зчитування
        public static string GetSavedLogin()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return File.ReadAllText(FilePath);
                }
            }
            catch { /* Ігноруємо помилки */ }

            return ""; // Якщо файлу немає - повертаємо порожній рядок
        }

        // Метод для видалення (якщо зняли галочку)
        public static void ClearLogin()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch { }
        }
    }
}