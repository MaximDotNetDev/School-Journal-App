USE master;
GO

-- 1. ОЧИЩЕННЯ ТА СТВОРЕННЯ БД
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'DB_SchoolJournal')
BEGIN
    ALTER DATABASE DB_SchoolJournal SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE DB_SchoolJournal;
END
GO

CREATE DATABASE DB_SchoolJournal;
GO

USE DB_SchoolJournal;
GO

PRINT '=== 1. Створення структури таблиць... ===';

CREATE TABLE AccessRoles (
    RoleID INT PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255)
);

CREATE TABLE Positions (
    PositionID INT PRIMARY KEY IDENTITY(1,1),
    PositionName NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE Qualifications (
    QualificationID INT PRIMARY KEY IDENTITY(1,1),
    QualificationName NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE PedagogicalTitles (
    TitleID INT PRIMARY KEY IDENTITY(1,1),
    TitleName NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE GradeTypes (
    GradeTypeID INT PRIMARY KEY IDENTITY(1,1),
    TypeName NVARCHAR(50) NOT NULL
);

CREATE TABLE Announcements (
    AnnouncementID INT PRIMARY KEY IDENTITY(1,1),
    Content NVARCHAR(255) NOT NULL,
    DateCreated DATETIME DEFAULT GETDATE()
);

-- ТАБЛИЦЯ TEACHERS
CREATE TABLE Teachers (
    TeacherID INT PRIMARY KEY IDENTITY(1,1),
    LastName NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    MiddleName NVARCHAR(50),
    Email NVARCHAR(100) UNIQUE,
    Phone NVARCHAR(20),
    Specialization NVARCHAR(100),  
    DateOfBirth DATE,
    Gender NVARCHAR(10) NOT NULL,  
    Workload DECIMAL(5,2),             
    EducationInfo NVARCHAR(MAX),    
    PhotoPath NVARCHAR(MAX),
    
    -- Поля авторизації
    Login NVARCHAR(50) NULL,
    Password NVARCHAR(MAX) NULL,
    AccessRoleID INT DEFAULT 3, -- За замовчуванням Вчитель (3)
    
    PositionID INT NOT NULL,
    QualificationID INT,
    PedagogicalTitleID INT,
    FOREIGN KEY (PositionID) REFERENCES Positions(PositionID),
    FOREIGN KEY (QualificationID) REFERENCES Qualifications(QualificationID),
    FOREIGN KEY (PedagogicalTitleID) REFERENCES PedagogicalTitles(TitleID),
    FOREIGN KEY (AccessRoleID) REFERENCES AccessRoles(RoleID)
);

CREATE TABLE Subjects (
    SubjectID INT PRIMARY KEY IDENTITY(1,1),
    SubjectName NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE Classes (
    ClassID INT PRIMARY KEY IDENTITY(1,1),
    ClassName NVARCHAR(10) NOT NULL,
    GradeLevel INT NOT NULL,
    HomeroomTeacherID INT UNIQUE NOT NULL,
    FOREIGN KEY (HomeroomTeacherID) REFERENCES Teachers(TeacherID)
);

CREATE TABLE Students (
    StudentID INT PRIMARY KEY IDENTITY(1,1),
    LastName NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    MiddleName NVARCHAR(50),
    DateOfBirth DATE,
    ClassID INT NOT NULL,
    ParentContactPhone NVARCHAR(20),
    Gender NVARCHAR(10),
    DocumentType NVARCHAR(50),
    DocumentSeries NVARCHAR(10),
    DocumentNumber NVARCHAR(20),
    EnrollmentDate DATE,
    EnrollmentReason NVARCHAR(200),
    SchoolName NVARCHAR(200) DEFAULT N'Хрінівська філія Іллінецького ліцею №1',
    PhotoPath NVARCHAR(MAX),
    FOREIGN KEY (ClassID) REFERENCES Classes(ClassID)
);

CREATE TABLE Parents (
    ParentID INT PRIMARY KEY IDENTITY(1,1),
    StudentID INT NOT NULL,
    LastName NVARCHAR(50),
    FirstName NVARCHAR(50),
    MiddleName NVARCHAR(50),      
    Role NVARCHAR(50),            
    Phone NVARCHAR(20),           
    FOREIGN KEY (StudentID) REFERENCES Students(StudentID) ON DELETE CASCADE
);

CREATE TABLE TeachingAssignments (
    AssignmentID INT PRIMARY KEY IDENTITY(1,1),
    TeacherID INT NOT NULL,
    SubjectID INT NOT NULL,
    ClassID INT NOT NULL,
    FOREIGN KEY (TeacherID) REFERENCES Teachers(TeacherID),
    FOREIGN KEY (SubjectID) REFERENCES Subjects(SubjectID),
    FOREIGN KEY (ClassID) REFERENCES Classes(ClassID),
    UNIQUE (TeacherID, SubjectID, ClassID)
);

CREATE TABLE FixedSchedule (
    ScheduleID INT PRIMARY KEY IDENTITY(1,1),
    DayOfWeek INT,
    LessonNumber INT,
    AssignmentID INT NOT NULL,
    FOREIGN KEY (AssignmentID) REFERENCES TeachingAssignments(AssignmentID)
);

CREATE TABLE Lessons (
    LessonID INT PRIMARY KEY IDENTITY(1,1),
    AssignmentID INT NOT NULL,
    LessonDate DATE NOT NULL,
    LessonTopic NVARCHAR(255),
    Homework NVARCHAR(1000),
    LessonTypeID INT DEFAULT 1,
    FOREIGN KEY (AssignmentID) REFERENCES TeachingAssignments(AssignmentID),
    FOREIGN KEY (LessonTypeID) REFERENCES GradeTypes(GradeTypeID)
);

CREATE TABLE Grades (
    GradeID INT PRIMARY KEY IDENTITY(1,1),
    LessonID INT NOT NULL,
    StudentID INT NOT NULL,
    GradeValue NVARCHAR(3) NOT NULL,
    Comment NVARCHAR(255),
    FOREIGN KEY (LessonID) REFERENCES Lessons(LessonID),
    FOREIGN KEY (StudentID) REFERENCES Students(StudentID)
);
GO

PRINT '=== 2. Наповнення довідників (СПРОЩЕНО)... ===';

-- ТУТ МИ ЗАЛИШИЛИ ТІЛЬКИ 3 РОЛІ, ЯК ВИ ПРОСИЛИ
INSERT INTO AccessRoles (RoleID, RoleName, Description) VALUES
(1, N'Адміністратор БД', N'Повний доступ до системи та налаштувань'),
(2, N'Адміністратор Закладу', N'Директор/Завуч. Перегляд та редагування всього контенту'),
(3, N'Вчитель', N'Доступ до своїх предметів. Класні керівники бачать свій клас.');
-- Ролі 4 і 5 видалені

SET IDENTITY_INSERT Qualifications ON;
INSERT INTO Qualifications (QualificationID, QualificationName) VALUES
(1, N'Вища категорія'), (2, N'Перша категорія'), (3, N'Друга категорія'), (4, N'Спеціаліст');
SET IDENTITY_INSERT Qualifications OFF;

SET IDENTITY_INSERT PedagogicalTitles ON;
INSERT INTO PedagogicalTitles (TitleID, TitleName) VALUES
(1, N'Вчитель-методист'), (2, N'Старший вчитель');
SET IDENTITY_INSERT PedagogicalTitles OFF;

SET IDENTITY_INSERT Positions ON;
INSERT INTO Positions (PositionID, PositionName) VALUES
(1, N'Вчитель'), (2, N'Директор школи'), (3, N'Заступник директора'), (4, N'Практичний психолог'), (5, N'Вчитель/Адміністратор БД');
SET IDENTITY_INSERT Positions OFF;

SET IDENTITY_INSERT GradeTypes ON;

INSERT INTO GradeTypes (GradeTypeID, TypeName) VALUES 
(1, N'Поточна'), 
(2, N'Зошит'), 
(3, N'Самостійна робота'),
(4, N'Контрольна робота'), 
(5, N'Практична робота'), 
(6, N'Лабораторна робота'),
(7, N'Діагностувальна робота'), 
(8, N'Тематична'), 
(9, N'Семестрова'),
(10, N'Скоригована'), 
(11, N'Річна'), 
(12, N'Підсумкова'),
-- НОВІ ТИПИ ДЛЯ НУШ (Продовжуємо нумерацію)
(13, N'Група результатів 1 (ГР1)'),
(14, N'Група результатів 2 (ГР2)'),
(15, N'Група результатів 3 (ГР3)'),
(16, N'Група результатів 4 (ГР4)'),
(17, N'Загальна оцінка (Семестр)');

SET IDENTITY_INSERT GradeTypes OFF;
GO
SET IDENTITY_INSERT Subjects ON;
INSERT INTO Subjects (SubjectID, SubjectName) VALUES 
(1, N'Українська мова'), (2, N'Українська література'), (3, N'Зарубіжна література'), (4, N'Англійська мова'),
(5, N'Математика'), (6, N'Алгебра'), (7, N'Геометрія'), (8, N'Інформатика'),
(9, N'Пізнаємо природу'), (10, N'Біологія'), (11, N'Географія'), (12, N'Фізика'), (13, N'Хімія'),    
(14, N'Історія України'), (15, N'Всесвітня історія'), (16, N'Правознавство'), 
(17, N'Музичне мистецтво'), (18, N'Образотворче мистецтво'), (19, N'Мистецтво'), 
(20, N'Трудове навчання'), (21, N'Основи здоров''я'), (22, N'Фізична культура');
SET IDENTITY_INSERT Subjects OFF;

INSERT INTO Announcements (Content) VALUES (N'Зимові канікули з 29 грудня!'), (N'Батьківські збори 15.12');
GO

PRINT '=== 3. Персонал... ===';

DECLARE @Vyscha INT = 1; DECLARE @Persha INT = 2; DECLARE @Druha INT = 3; DECLARE @Specialist INT = 4;
DECLARE @Metodyst INT = 1; DECLARE @Starshyy INT = 2;
DECLARE @Vchytel INT = 1; DECLARE @Director INT = 2; DECLARE @Zastupnyk INT = 3; DECLARE @Psyholog INT = 4; DECLARE @Vch_Admin INT = 5;

SET IDENTITY_INSERT Teachers ON;

INSERT INTO Teachers (TeacherID, LastName, FirstName, MiddleName, Email, Phone, Specialization, DateOfBirth, Gender, Workload, EducationInfo, PositionID, QualificationID, PedagogicalTitleID) VALUES
(1, N'Коваленко', N'Ірина', N'Петрівна', 'kovalenko@school.ua', '(097) 111-22-33', N'Математика', '1975-03-12', N'Жіноча', 1.25, N'Вінницький ДПУ', @Vchytel, @Vyscha, @Metodyst), 
(2, N'Шевченко', N'Олег', N'Іванович', 'shevchenko@school.ua', '(093) 222-33-44', N'Фізкультура/Основи здоров''я', '1988-07-20', N'Чоловіча', 1.25, N'НУФВСУ', @Director, @Persha, NULL),
(3, N'Слово', N'Галина', N'Михайлівна', 'slovo@school.ua', '(095) 123-32-11', N'Українська мова/літ', '1970-05-05', N'Жіноча', 1.5, N'НПУ Драгомаманова', @Vchytel, @Vyscha, @Metodyst),
(4, N'Петренко', N'Ігор', N'Васильович', 'petrenko@school.ua', '(050) 111-22-00', N'Історія/Правознавство', '1979-09-19', N'Чоловіча', 0.75, N'ХНУВС', @Vchytel, @Vyscha, @Starshyy),
(5, N'Садовий', N'Михайло', N'Петрович', 'sadovyy@school.ua', '(067) 555-66-77', N'Біологія/Географія', '1985-07-07', N'Чоловіча', 0.75, N'УДПУ', @Vchytel, @Persha, NULL),
(6, N'Блек', N'Джессіка', N'Дмитрівна', 'black@school.ua', '(093) 444-55-66', N'Англійська мова', '1995-02-14', N'Жіноча', 0.50, N'ЛНУ Франка', @Vchytel, @Specialist, NULL),
-- НЬЮТОН (Admin)
(7, N'Ньютон', N'Василь', N'Ісаакович', 'newton@school.ua', '(050) 333-22-11', N'Фізика/Інформатика/Адміністратор БД', '1988-12-12', N'Чоловіча', 1.25, N'КНУ Фізфак/КПІ', @Vch_Admin, @Persha, NULL),
(8, N'Глухота', N'Марія', N'Анатоліївна', 'glukhota@school.ua', '(093) 123-45-67', N'Мистецтво/Технології', '1985-05-20', N'Жіноча', 1.25, N'Вінницький ДПУ', @Zastupnyk, @Persha, NULL),
(9, N'Васильчук', N'Оксана', N'Дмитрівна', 'vasylchuk@school.ua', '(050) 345-67-89', N'Психолог', '1991-12-05', N'Жіноча', 1.00, N'Університет Грінченка', @Psyholog, @Druha, NULL),
(10, N'Ткаченко', N'Олена', N'Василівна', 'tkachenko@school.ua', '(098) 555-66-77', N'Хімія/Математика/Заруб. літ.', '1990-02-28', N'Жіноча', 1.00, N'ЖДУ Франка', @Vchytel, @Druha, NULL);

SET IDENTITY_INSERT Teachers OFF;
GO

PRINT '=== 4. Налаштування АДМІНА... ===';
-- Оновлюємо Ньютона (Admin) ТІЛЬКИ ТЕПЕР, КОЛИ ВІН ВЖЕ Є
UPDATE Teachers
SET 
    Login = 'admin',
    Password = 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3',
    AccessRoleID = 1 -- Адміністратор (ПОВНИЙ ДОСТУП)
WHERE TeacherID = 7;

-- Налаштування ДИРЕКТОРА
UPDATE Teachers 
SET AccessRoleID = 2 -- Адміністратор закладу (ДИРЕКТОР)
WHERE TeacherID = 2;

-- Всі інші автоматично отримали AccessRoleID = 3 (Вчитель) при створенні.
GO

PRINT '=== 5. Класи... ===';
INSERT INTO Classes (ClassName, GradeLevel, HomeroomTeacherID) 
VALUES (N'5', 5, 4), (N'6', 6, 3), (N'7', 7, 5), (N'8', 8, 6), (N'9', 9, 1);   
GO

PRINT '=== 6. Генерація УЧНІВ (З батьком і матір''ю)... ===';

-- Створення тимчасових таблиць імен (щоб не засмічувати базу)
CREATE TABLE #Surnames (S NVARCHAR(50));
INSERT INTO #Surnames VALUES (N'Коваленко'), (N'Бондаренко'), (N'Ткаченко'), (N'Мельник'), (N'Шевченко'), (N'Бойко'), (N'Кравченко'), (N'Козак'), (N'Олійник'), (N'Лисенко'), (N'Гаврилюк'), (N'Поліщук'), (N'Іваненко'), (N'Мороз'), (N'Петренко'), (N'Павленко'), (N'Василенко'), (N'Сидоренко'), (N'Савченко'), (N'Кузьменко'),(N'Зінченко'), (N'Марченко'), (N'Демченко'), (N'Романенко'), (N'Литвин'), (N'Бабич'), (N'Гнатюк'), (N'Волошин'), (N'Даниленко'), (N'Терещенко');

CREATE TABLE #MaleNames (N NVARCHAR(50));
INSERT INTO #MaleNames VALUES (N'Олександр'), (N'Максим'), (N'Дмитро'), (N'Артем'), (N'Іван'), (N'Михайло'), (N'Богдан'), (N'Андрій'), (N'Єгор'), (N'Владислав'),(N'Назар'), (N'Данило'), (N'Роман'), (N'Володимир'), (N'Тимофій'), (N'Матвій'), (N'Сергій'), (N'Ярослав'), (N'Денис'), (N'Олексій');

CREATE TABLE #FemaleNames (N NVARCHAR(50));
INSERT INTO #FemaleNames VALUES (N'Анна'), (N'Софія'), (N'Марія'), (N'Вікторія'), (N'Дарина'), (N'Анастасія'), (N'Поліна'), (N'Вероніка'), (N'Єва'), (N'Злата'),(N'Мілана'), (N'Соломія'), (N'Олександра'), (N'Ольга'), (N'Юлія'), (N'Тетяна'), (N'Яна'), (N'Діана'), (N'Катерина'), (N'Аліса');

DECLARE @CurrentClassIter INT = 1;
WHILE @CurrentClassIter <= 5
BEGIN
    DECLARE @ClassID INT = @CurrentClassIter;
    DECLARE @GradeLevel INT = (SELECT GradeLevel FROM Classes WHERE ClassID = @ClassID);
    DECLARE @StudentCount INT = 1;
    DECLARE @TotalStudents INT = 18 + (ABS(CHECKSUM(NEWID())) % 8);

    WHILE @StudentCount <= @TotalStudents
    BEGIN
        DECLARE @IsMale BIT = CAST(ABS(CHECKSUM(NEWID())) % 2 AS BIT);
        DECLARE @Gen NVARCHAR(10);
        DECLARE @FirstName NVARCHAR(50); 
        DECLARE @Surname NVARCHAR(50);
        
        -- Випадкове прізвище
        SELECT TOP 1 @Surname = S FROM #Surnames ORDER BY NEWID();

        -- Визначаємо стать та ім'я
        IF @IsMale = 1 
        BEGIN 
            SET @Gen = N'Чоловіча'; 
            SELECT TOP 1 @FirstName = N FROM #MaleNames ORDER BY NEWID(); 
        END
        ELSE 
        BEGIN 
            SET @Gen = N'Жіноча'; 
            SELECT TOP 1 @FirstName = N FROM #FemaleNames ORDER BY NEWID(); 
            -- Для жіночих прізвищ (наприклад Іванов -> Іванова)
            IF RIGHT(@Surname, 2) IN (N'ов', N'єв', N'ін') SET @Surname = @Surname + N'а';
            IF RIGHT(@Surname, 2) = N'ий' SET @Surname = LEFT(@Surname, LEN(@Surname)-2) + N'а'; 
        END

        -- Генеруємо по батькові (на основі імені батька)
        DECLARE @DadNameBase NVARCHAR(50); 
        SELECT TOP 1 @DadNameBase = N FROM #MaleNames ORDER BY NEWID();
        
        DECLARE @MiddleName NVARCHAR(50);
        -- Проста логіка утворення по батькові
        SET @MiddleName = @DadNameBase + CASE WHEN @IsMale=1 THEN N'ович' ELSE N'івна' END;
        -- (Для ідеальної точності треба складнішу логіку, але для тесту цього достатньо)
        
        DECLARE @YearOfBirth INT = 2025 - 10 - (@GradeLevel - 5);
        DECLARE @DOB DATE = DATEFROMPARTS(@YearOfBirth, (ABS(CHECKSUM(NEWID())) % 12) + 1, (ABS(CHECKSUM(NEWID())) % 28) + 1);

        -- 1. Вставляємо Учня
        INSERT INTO Students (LastName, FirstName, MiddleName, DateOfBirth, ClassID, Gender, DocumentType, DocumentSeries, DocumentNumber, EnrollmentDate, EnrollmentReason, ParentContactPhone)
        VALUES (@Surname, @FirstName, @MiddleName, @DOB, @ClassID, @Gen, N'Свідоцтво', N'I-AM', CAST(100000+ABS(CHECKSUM(NEWID()))%900000 AS NVARCHAR), '2021-09-01', N'Заява', N'+380' + CAST(CAST(RAND()*1000000000 AS BIGINT) AS NVARCHAR));
        
        DECLARE @SID INT = SCOPE_IDENTITY();

        -- 2. Вставляємо БАТЬКА (Прізвище таке ж, Ім'я = DadNameBase)
        INSERT INTO Parents (StudentID, LastName, FirstName, MiddleName, Role, Phone) 
        VALUES (@SID, @Surname, @DadNameBase, N'Іванович', N'Батько', N'+38050' + CAST(1000000 + ABS(CHECKSUM(NEWID())) % 8999999 AS NVARCHAR));

        -- 3. Вставляємо МАТІР (Прізвище жіноче, Ім'я випадкове)
        DECLARE @MomName NVARCHAR(50); SELECT TOP 1 @MomName = N FROM #FemaleNames ORDER BY NEWID();
        DECLARE @MomSurname NVARCHAR(50) = @Surname; 
        -- Якщо учень хлопець, то прізвище мами треба "жіночим" зробити (Іванов -> Іванова)
        IF @IsMale = 1
        BEGIN
             IF RIGHT(@MomSurname, 2) IN (N'ов', N'єв', N'ін') SET @MomSurname = @MomSurname + N'а';
             IF RIGHT(@MomSurname, 2) = N'ий' SET @MomSurname = LEFT(@MomSurname, LEN(@MomSurname)-2) + N'а';
        END

        INSERT INTO Parents (StudentID, LastName, FirstName, MiddleName, Role, Phone) 
        VALUES (@SID, @MomSurname, @MomName, N'Петрівна', N'Мати', N'+38067' + CAST(1000000 + ABS(CHECKSUM(NEWID())) % 8999999 AS NVARCHAR));

        SET @StudentCount = @StudentCount + 1;
    END
    SET @CurrentClassIter = @CurrentClassIter + 1;
END
DROP TABLE #Surnames; DROP TABLE #MaleNames; DROP TABLE #FemaleNames;
GO
PRINT '=== 7. Навантаження... ===';
DECLARE @C5 INT = 1; DECLARE @C6 INT = 2; DECLARE @C7 INT = 3; DECLARE @C8 INT = 4; DECLARE @C9 INT = 5;
-- Клас 5 
INSERT INTO TeachingAssignments VALUES (10,5,@C5), (2,22,@C5), (3,1,@C5), (3,2,@C5), (6,4,@C5), (4,14,@C5), (5,9,@C5), (7,8,@C5), (8,17,@C5), (8,18,@C5), (8,19,@C5), (8,20,@C5), (2,21,@C5), (10, 3, @C5); 
-- Клас 6 
INSERT INTO TeachingAssignments VALUES (10,5,@C6), (3,1,@C6), (3,2,@C6), (6,4,@C6), (4,14,@C6), (4,15,@C6), (5,9,@C6), (5,11,@C6), (7,8,@C6), (8,20,@C6), (8,17,@C6), (2,21,@C6), (2,22,@C6), (10, 3, @C6); 
-- Клас 7 
INSERT INTO TeachingAssignments VALUES (1,6,@C7), (1,7,@C7), (10,13,@C7), (3,1,@C7), (3,2,@C7), (6,4,@C7), (7,12,@C7), (5,10,@C7), (5,11,@C7), (4,14,@C7), (4,15,@C7), (7,8,@C7), (8,20,@C7), (2,21,@C7), (2,22,@C7), (10, 3, @C7); 
-- Клас 8 
INSERT INTO TeachingAssignments VALUES (1,6,@C8), (1,7,@C8), (10,13,@C8), (3,1,@C8), (3,2,@C8), (6,4,@C8), (7,12,@C8), (5,10,@C8), (5,11,@C8), (4,14,@C8), (4,15,@C8), (7,8,@C8), (8,19,@C8), (8,20,@C8), (2,21,@C8), (2,22,@C8), (10, 3, @C8); 
-- Клас 9 
INSERT INTO TeachingAssignments VALUES (1,6,@C9), (1,7,@C9), (10,13,@C9), (3,1,@C9), (3,2,@C9), (6,4,@C9), (7,12,@C9), (5,10,@C9), (5,11,@C9), (4,14,@C9), (4,15,@C9), (4,16,@C9), (7,8,@C9), (8,19,@C9), (8,20,@C9), (2,21,@C9), (2,22,@C9), (10, 3, @C9); 
GO

PRINT '=== 8. Генерація фіксованого розкладу... ===';
DECLARE schedule_cursor CURSOR FOR SELECT ClassID, GradeLevel FROM Classes;
OPEN schedule_cursor;
DECLARE @S_ClassID_I INT, @S_GradeLevel_I INT;
FETCH NEXT FROM schedule_cursor INTO @S_ClassID_I, @S_GradeLevel_I;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @SubjectPool_I TABLE (AssignmentID INT, Freq INT);
    DELETE FROM @SubjectPool_I;
    INSERT INTO @SubjectPool_I (AssignmentID, Freq)
    SELECT ta.AssignmentID, CASE WHEN s.SubjectID IN (1,6,7) THEN 4 WHEN s.SubjectID IN (2,4,12,14,13,5,10,11, 3) THEN 2 ELSE 1 END
    FROM TeachingAssignments ta JOIN Subjects s ON ta.SubjectID = s.SubjectID WHERE ta.ClassID = @S_ClassID_I;

    DECLARE @Day_I INT = 2; DECLARE @LessonNum_I INT = 1;
    DECLARE @CurrentAssignment_I INT;
    
    WHILE EXISTS (SELECT 1 FROM @SubjectPool_I WHERE Freq > 0)
    BEGIN
        SELECT TOP 1 @CurrentAssignment_I = AssignmentID FROM @SubjectPool_I WHERE Freq > 0 ORDER BY Freq DESC, NEWID();
        INSERT INTO FixedSchedule (DayOfWeek, LessonNumber, AssignmentID) VALUES (@Day_I, @LessonNum_I, @CurrentAssignment_I);
        UPDATE @SubjectPool_I SET Freq = Freq - 1 WHERE AssignmentID = @CurrentAssignment_I;
        SET @LessonNum_I = @LessonNum_I + 1;
        DECLARE @Limit_I INT = CASE WHEN @S_GradeLevel_I < 7 THEN 6 WHEN @S_GradeLevel_I = 7 THEN 6 ELSE 7 END;
        IF @LessonNum_I > @Limit_I BEGIN SET @LessonNum_I = 1; SET @Day_I = @Day_I + 1; IF @Day_I > 6 SET @Day_I = 2; END
    END
    FETCH NEXT FROM schedule_cursor INTO @S_ClassID_I, @S_GradeLevel_I;
END
CLOSE schedule_cursor; DEALLOCATE schedule_cursor;
GO

PRINT '=== 9. Генерація Журналу та Оцінок (ФІНАЛ: ТЕМИ + ГР + ОЦІНКИ) ===';

-- 1. Створюємо "Базу знань" про групи результатів
DECLARE @NushDefinitions TABLE (
    SubjectIDList NVARCHAR(50), 
    GR_Num INT,                 
    TopicText NVARCHAR(255)     
);

-- === НАПОВНЕННЯ ГР (Як було) ===
INSERT INTO @NushDefinitions VALUES 
('5,6,7', 1, N'Досліджує ситуації та створює математичні моделі'),
('5,6,7', 2, N'Розв’язує математичні задачі'),
('5,6,7', 3, N'Інтерпретує та критично аналізує результати'),
('17,18,19', 1, N'Пізнання мистецтва, художнє мислення'),
('17,18,19', 2, N'Художньо-творча діяльність, мистецька комунікація'),
('17,18,19', 3, N'Емоційний досвід, художньо-естетичне ставлення'),
('4', 1, N'Сприймає усну інформацію на слух / Аудіювання'),
('4', 2, N'Усно взаємодіє та висловлюється / Говоріння'),
('4', 3, N'Сприймає письмові тексти / Читання'),
('4', 4, N'Письмово взаємодіє та висловлюється / Письмо'),
('20', 1, N'Проєктує та виготовляє вироби'),
('20', 2, N'Застосовує технології декоративно-ужиткового мистецтва'),
('20', 3, N'Ефективне використання техніки і матеріалів'),
('20', 4, N'Виявляє самозарадність у побуті/освітньому процесі'),
('14,15,16', 1, N'Орієнтується в історичному часі та просторі'),
('14,15,16', 2, N'Працює з інформацією історичного змісту'),
('14,15,16', 3, N'Виявляє здатність до співпраці, громадянську позицію'),
('21', 1, N'Безпека. Уникання загроз для життя'),
('21', 2, N'Здоров’я. Турбота про особисте здоров’я'),
('21', 3, N'Добробут. Підприємливість та етична поведінка'),
('22', 1, N'Розвиває особистісні якості в процесі фіз. виховання'),
('22', 2, N'Володіє технікою фізичних вправ'),
('22', 3, N'Здійснює фізкультурно-оздоровчу діяльність'),
('8', 1, N'Працює з інформацією, даними, моделями'),
('8', 2, N'Створює інформаційні продукти'),
('8', 3, N'Працює в цифровому середовищі'),
('8', 4, N'Безпечно та відповідально працює з технологіями'),
('9,10,11,12,13', 1, N'Досліджує природу'),
('9,10,11,12,13', 2, N'Здійснює пошук та опрацьовує інформацію'),
('9,10,11,12,13', 3, N'Усвідомлює закономірності природи'),
('1,2,3', 1, N'Усно взаємодіє'),
('1,2,3', 2, N'Працює з текстом'),
('1,2,3', 3, N'Письмово взаємодіє'),
('1,2,3', 4, N'Досліджує мовлення');

-- 2. Відновлюємо базу ТЕМ УРОКІВ (щоб було що писати в тему)
IF OBJECT_ID('tempdb..#Topics') IS NOT NULL DROP TABLE #Topics;
CREATE TABLE #Topics (SubjectID INT, TopicList NVARCHAR(MAX));

INSERT INTO #Topics VALUES 
(5, N'Натуральні числа;Дії з натуральними числами;Рівняння;Кути та їх міра;Трикутники;Площа прямокутника;Дроби;Десяткові дроби;Відсотки;Середнє арифметичне'), 
(6, N'Раціональні вирази;Степінь з цілим показником;Функції;Квадратні корені;Квадратні рівняння;Системи рівнянь;Числові послідовності;Нерівності'), 
(7, N'Найпростіші геометричні фігури;Трикутники;Паралельні прямі;Коло і круг;Геометричні побудови;Чотирикутники;Подібність фігур;Вектори;Координати на площині'), 
(1, N'Вступ;Лексикологія;Фразеологія;Будова слова;Словотвір;Іменник;Прикметник;Числівник;Займенник;Дієслово;Прислівник;Синтаксис'), 
(2, N'Усна народна творчість;Давня література;Творчість Т.Шевченка;Література ХХ ст.;Сучасна література;Поезія;Проза;Драматургія'), 
(4, N'My Family;My School;My Friends;Holidays;Travelling;Food and Drinks;London;Ukraine;Seasons and Weather;Sport;Music;Books'), 
(8, N'Інформація та повідомлення;Комп''ютерні пристрої;ОС Windows;Текстовий редактор;Графічний редактор;Презентації;Інтернет;Алгоритми;Програмування'), 
(12, N'Фізичні тіла;Будова речовини;Механічний рух;Сили в природі;Тиск;Робота і енергія;Теплові явища;Електричний струм;Світлові явища'), 
(13, N'Початкові хімічні поняття;Кисень;Вода;Розчини;Основні класи неорганічних сполук;Періодичний закон;Хімічний зв''язок'), 
(11, N'Земля у Всесвіті;План місцевості;Географічна карта;Літосфера;Атмосфера;Гідросфера;Біосфера;Населення Землі;Країни світу'), 
(14, N'Вступ до історії;Київська Русь;Козацька доба;Українські землі у складі імперій;Українська революція;Друга світова війна;Незалежна Україна'), 
(10, N'Клітина;Рослини;Гриби;Бактерії;Тварини;Людина;Розмноження;Спадковість;Еволюція;Екологія'), 
(22, N'Легка атлетика;Гімнастика;Волейбол;Баскетбол;Футбол;Рухливі ігри'), 
(17, N'Музичне мистецтво;Народна музика;Класична музика;Сучасна музика;Джаз;Рок;Поп-музика;Музичні інструменти'),
(20, N'Конструювання;Моделювання;Технологічні процеси;Обробка деревини;Обробка металів;Дизайн;Декоративно-ужиткове мистецтво'),
(3, N'Вступ;Ренесанс;Бароко;Класицизм;Романтизм;Реалізм;Модернізм;Постмодернізм;Літературні жанри;Теорія літератури');


-- Очищення
DELETE FROM Grades;
DELETE FROM Lessons;

DECLARE @StartDate DATE = '2025-09-01';
DECLARE @EndDate DATE = '2025-12-26';
DECLARE @TodaySimulated DATE = '2025-11-24'; 
DECLARE @GradesStart DATE = '2025-09-15';

DECLARE @ActiveSickness TABLE (StudentID INT, SickUntil DATE);
DECLARE @CurrentDate DATE = @StartDate;

-- === ГОЛОВНИЙ ЦИКЛ ===
WHILE @CurrentDate <= @EndDate
BEGIN
    DECLARE @DOW_L INT = DATEPART(WEEKDAY, @CurrentDate);
    
    IF @DOW_L BETWEEN 2 AND 6
    BEGIN
        DECLARE lessons_cursor_L CURSOR FOR
        SELECT fs.AssignmentID, s.SubjectName, s.SubjectID, ta.ClassID
        FROM FixedSchedule fs
        JOIN TeachingAssignments ta ON fs.AssignmentID = ta.AssignmentID
        JOIN Subjects s ON ta.SubjectID = s.SubjectID
        WHERE fs.DayOfWeek = @DOW_L
        ORDER BY fs.LessonNumber;

        OPEN lessons_cursor_L;
        DECLARE @L_AssID INT, @L_SubjName NVARCHAR(100), @L_SubjID INT, @L_ClassID INT;
        FETCH NEXT FROM lessons_cursor_L INTO @L_AssID, @L_SubjName, @L_SubjID, @L_ClassID;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @Cnt INT; 
            SELECT @Cnt = COUNT(*) FROM Lessons WHERE AssignmentID = @L_AssID;
            DECLARE @LessonNum INT = @Cnt + 1;

            DECLARE @Type INT;
            DECLARE @LessonTopicName NVARCHAR(255);
            DECLARE @HomeworkText NVARCHAR(255) = N'Опрацювати матеріал';
            
            -- === СПОЧАТКУ ВИЗНАЧАЄМО БАЗОВУ ТЕМУ УРОКУ ЗІ СПИСКУ ===
            DECLARE @BaseTopic NVARCHAR(255);
            DECLARE @TopicString NVARCHAR(MAX);
            SELECT @TopicString = TopicList FROM #Topics WHERE SubjectID = @L_SubjID;
            
            IF @TopicString IS NOT NULL
            BEGIN
                DECLARE @XML XML = CAST('<t>' + REPLACE(@TopicString, ';', '</t><t>') + '</t>' AS XML);
                DECLARE @TCount INT = @XML.value('count(/t)', 'int');
                DECLARE @TIndex INT = (@Cnt % @TCount) + 1;
                
                WITH T AS (SELECT ROW_NUMBER() OVER(ORDER BY (SELECT 1)) AS Num, n.value('.', 'NVARCHAR(255)') AS Topic FROM @XML.nodes('/t') AS T(n))
                SELECT @BaseTopic = Topic FROM T WHERE Num = @TIndex;
            END
            ELSE
            BEGIN
                SET @BaseTopic = N'Вивчення нової теми';
            END


            -- === ТЕПЕР ВИЗНАЧАЄМО ТИП І ФОРМУЄМО ФІНАЛЬНУ НАЗВУ ===

            -- 1. КОЖЕН 10-й УРОК -> ТЕМАТИЧНА
            IF @LessonNum % 10 = 0
            BEGIN
                SET @Type = 8; -- Тематична
                SET @LessonTopicName = N'Підсумкова тематична робота (' + @BaseTopic + N')';
            END
            ELSE
            -- 2. ВСІ ІНШІ -> ГРУПИ РЕЗУЛЬТАТІВ (НУШ)
            BEGIN
                DECLARE @MaxGRs INT = 0;
                SELECT @MaxGRs = MAX(GR_Num) 
                FROM @NushDefinitions 
                WHERE ',' + SubjectIDList + ',' LIKE '%,' + CAST(@L_SubjID AS NVARCHAR) + ',%';

                IF @MaxGRs > 0
                BEGIN
                    DECLARE @CycleNum INT = ((@LessonNum - 1) % @MaxGRs) + 1;
                    DECLARE @GR_Description NVARCHAR(255);
                    
                    SELECT @GR_Description = TopicText
                    FROM @NushDefinitions
                    WHERE GR_Num = @CycleNum 
                      AND ',' + SubjectIDList + ',' LIKE '%,' + CAST(@L_SubjID AS NVARCHAR) + ',%';

                    SET @Type = 12 + @CycleNum; -- 13=ГР1, 14=ГР2...
                    
                    -- ФОРМАТ: "Назва теми (Назва ГР)"
                    SET @LessonTopicName = @BaseTopic + N' (' + @GR_Description + N')';
                END
                ELSE
                BEGIN
                    -- Fallback
                    SET @Type = 1;
                    SET @LessonTopicName = @BaseTopic;
                END
            END

            INSERT INTO Lessons (AssignmentID, LessonDate, LessonTopic, Homework, LessonTypeID)
            VALUES (@L_AssID, @CurrentDate, @LessonTopicName, @HomeworkText, @Type);
            DECLARE @NewLID INT = SCOPE_IDENTITY();

            -- === ГЕНЕРАЦІЯ ОЦІНОК ===
            IF @CurrentDate <= @TodaySimulated
            BEGIN
                DECLARE st_cursor CURSOR FOR SELECT StudentID FROM Students WHERE ClassID = @L_ClassID;
                OPEN st_cursor;
                DECLARE @StID INT;
                FETCH NEXT FROM st_cursor INTO @StID;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    DECLARE @GradeToInsert NVARCHAR(3) = NULL;
                    DECLARE @Rand INT = ABS(CHECKSUM(NEWID())) % 100;

                    -- === ЛОГІКА ОЦІНЮВАННЯ ===
                    
                    -- А) ТЕМАТИЧНА (Тип 8) -> ВСІМ УЧНЯМ (100%)
                    IF @Type = 8 
                    BEGIN
                        SET @GradeToInsert = CAST((6 + ABS(CHECKSUM(NEWID())) % 7) AS NVARCHAR);
                    END
                    
                    -- Б) ГРУПИ РЕЗУЛЬТАТІВ та ЗВИЧАЙНІ -> ВИБІРКОВО (30%)
                    ELSE 
                    BEGIN
                        -- 1. Перевірка на хворобу
                        DECLARE @SickEnd DATE = (SELECT MAX(SickUntil) FROM @ActiveSickness WHERE StudentID = @StID);
                        
                        IF @SickEnd IS NOT NULL AND @SickEnd >= @CurrentDate
                        BEGIN
                            SET @GradeToInsert = N'хв'; -- Хворіє
                        END
                        ELSE
                        BEGIN
                            -- Шанс захворіти новому
                            IF ABS(CHECKSUM(NEWID())) % 1000 < 3
                            BEGIN
                                INSERT INTO @ActiveSickness (StudentID, SickUntil) VALUES (@StID, DATEADD(DAY, 5, @CurrentDate));
                                SET @GradeToInsert = N'хв';
                            END
                        END

                        -- 2. Якщо здоровий -> шанс 30% отримати оцінку
                        IF @GradeToInsert IS NULL AND @CurrentDate >= @GradesStart
                        BEGIN
                            IF @Rand < 60 
                            BEGIN
                                -- Оцінка 4-12
                                SET @GradeToInsert = CAST((4 + ABS(CHECKSUM(NEWID())) % 9) AS NVARCHAR);
                            END
                            ELSE IF @Rand > 98 
                            BEGIN
                                -- Рідкісний "Н"
                                SET @GradeToInsert = N'Н';
                            END
                        END
                    END

                    -- Вставка оцінки
                    IF @GradeToInsert IS NOT NULL
                        INSERT INTO Grades (LessonID, StudentID, GradeValue) VALUES (@NewLID, @StID, @GradeToInsert);
                    
                    FETCH NEXT FROM st_cursor INTO @StID;
                END
                CLOSE st_cursor; DEALLOCATE st_cursor;
            END
            FETCH NEXT FROM lessons_cursor_L INTO @L_AssID, @L_SubjName, @L_SubjID, @L_ClassID;
        END
        CLOSE lessons_cursor_L; DEALLOCATE lessons_cursor_L;
    END
    SET @CurrentDate = DATEADD(DAY, 1, @CurrentDate);
END
DROP TABLE #Topics;
GO


PRINT '=== ГОТОВО! ===';
-- Фінальна перевірка Адміна
SELECT 'ADMIN CHECK' AS Info, TeacherID, FullName = LastName+' '+FirstName, Login, AccessRoleID FROM Teachers WHERE Login = 'admin';