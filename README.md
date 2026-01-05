# 🎓 Teacher Hub

A comprehensive School Management System built with **C# WPF** and **Microsoft SQL Server**. This application acts as an autonomous digital journal for schools, focusing on reliability and data security.

> **📄 [Read Full Technical Documentation (Coursework PDF)](Documentation_Coursework.pdf.pdf)**
> *Note: The documentation describes the target architecture (MVVM), while the current implementation uses a classic Code-Behind approach for stability.*

## 📸 Dashboard Preview

![Teacher Hub Dashboard](screenshots/Admin%20Dashboard.png)

## ✨ Key Technical Features

* **🗄️ Advanced Database:** Normalized MS SQL Server database (3NF) with 15 tables, utilizing stored procedures and transactions for data integrity.
* **🔐 Security:** User passwords are secured using **SHA-256 hashing**.
* **🇺🇦 N.U.S.H. Support:** Fully supports the "New Ukrainian School" grading standards (Competency-based evaluation / Group Results).
* **⚡ Asynchronous Operations:** Uses `async/await` to ensure a responsive UI during heavy database queries.
* **📄 Export Capabilities:** Generates PDF reports and exports data to Excel/CSV.
* **🎨 Modern UI:** Custom XAML styles and ControlTemplates for a clean user interface.

## 🔮 Future Improvements (Roadmap)

* **Refactoring to MVVM:** Transitioning the codebase from Code-Behind to a full Model-View-ViewModel pattern to improve testability.
* **Cloud Integration:** Adding synchronization with Azure/AWS for remote access.
* **Mobile App:** Developing a companion app for parents using MAUI.

## 🚀 How to Run

1.  **Database Setup:**
    * Install **Microsoft SQL Server** (Express or LocalDB).
    * Run the script `Database/DB_SchoolJournal.sql` to create the database.
2.  **Configuration:**
    * Update the connection string in `App.config` if necessary.
3.  **Login:**
    * Launch the application.
    * Use the default admin credentials created by the SQL script.

## 💻 Technologies

* **Language:** C# .NET 8.0
* **UI Framework:** WPF (Windows Presentation Foundation)
* **Database:** MS SQL Server (T-SQL)
* **Tools:** Visual Studio 2022
