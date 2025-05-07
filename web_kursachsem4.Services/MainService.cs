using System;
using web_kursachsem4.Data.Models;
using web_kursachsem4.Data; // Переконайтесь, що тут правильний namespace для MainDbContext
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
//using Microsoft.Data.SqlClient; // Ймовірно, не потрібен при використанні EF Core абстракції
//using Npgsql; // Ймовірно, не потрібен при використанні EF Core абстракції
using web_kursachsem4.Exceptions; // Додайте using для вашого кастомного Exception
using BCrypt.Net;
//using static System.Runtime.InteropServices.JavaScript.JSType; // Цей using часто не потрібен у C# бекенді
using Microsoft.Extensions.Logging; // Додайте using для ILogger


namespace web_kursachsem4.Services
{
    // Інтерфейс з асинхронними методами
    public interface IMainService
    {
        Task<string?> GetUsernameAsync(int userId); // Може повернути null, якщо не знайдено
        Task<string?> GetEmailAsync(int userId);
        Task<User> AddUserAsync(User user); // Повертаємо доданого користувача (з ID)
        Task DeleteUserAsync(int userId);

        Task<int?> GetScoreAsync(int userId); // Може повернути null
        // Змінюємо сигнатуру: тепер повертаємо Task<EditScoreResult>
        Task<EditScoreResult> EditScoreAsync(int userId, int score);


        Task<Score[]> GetLeaderBoard();

        Task<bool[]?> GetLevelAsync(int userId); // Може повернути null
        Task EditLevelAsync(int userId, string lvl);
        Task<User?> AuthenticateUserAsync(string username, string password); // Повертає User при успіху, null при невдачі
    }

    // Реалізація сервісу з async/await та обробкою помилок
    public class MainService : IMainService
    {
        private readonly mainDBcontext _db;
        private readonly ILogger<MainService> _logger; // Додаємо логер

        // Оновлюємо primary constructor для інжекції логера
        public MainService(mainDBcontext db, ILogger<MainService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // --- User Methods ---
        public async Task<User?> AuthenticateUserAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("AuthenticateUserAsync called with empty username or password.");
                return null; // Неправильні вхідні дані
            }

            // Знаходимо користувача за ім'ям (регистрозалежне порівняння за замовчуванням)
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == username);

            if (user == null)
            {
                _logger.LogInformation("Authentication failed: User '{Username}' not found.", username);
                return null;
            }

            // user.Password - це збережений хеш з бази даних
            // password - це простий пароль, введений користувачем під час логіну
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);

            if (isPasswordValid)
            {
                _logger.LogInformation("User '{Username}' authenticated successfully.", username);
                return user;
            }
            else
            {
                _logger.LogInformation("Authentication failed: Invalid password for user '{Username}'.", username);
                return null;
            }
        }

        public async Task<User> AddUserAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.Password))
            {
                throw new ArgumentException("Username and password cannot be empty.", nameof(user));
            }


            // перевірка на унікальність UserName перед додаванням
            if (await _db.Users.AnyAsync(u => u.UserName == user.UserName))
            {
                _logger.LogWarning("Attempted to add user with existing username: '{Username}'.", user.UserName);
                // Краще кидати більш специфічний виняток, наприклад, CustomValidationException або повернути статус помилки
                // Але InvalidOperationException теж робочий варіант.
                throw new InvalidOperationException($"User with username '{user.UserName}' already exists.");
            }

            // --- Хешування Пароля ---
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _db.Users.Add(user);
            // _db.SaveChanges(); // Sync SaveChanges в асинхронному методі - погана практика. Використовуйте SaveChangesAsync.

            // Додаємо початкові записи Score та Levels одразу після додавання користувача
            // Переконайтесь, що User.UserId встановлюється EF Core ПЕРЕД цим,
            // або збережіть користувача спочатку, щоб отримати його ID, а потім додайте Score/Levels
            // Надійніше спочатку зберегти користувача, щоб отримати user.UserId
            await _db.SaveChangesAsync(); // Зберігаємо користувача, EF Core присвоїть ID

            // Тепер user.UserId має значення, можна створювати залежні записи
            var initialScore = new Score { UserId = user.UserId, UserName = user.UserName, ScoreValue = 0 };
            var initialLevels = new Levels { UserId = user.UserId, CompletedLevels = "f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f" }; // Припускаємо, User = user не потрібен, якщо UserId є FK


            _db.Scores.Add(initialScore);
            _db.Levels.Add(initialLevels);

            await _db.SaveChangesAsync(); // Зберігаємо залежні записи

            _logger.LogInformation("User '{Username}' (ID: {UserId}) and initial score/levels created successfully.", user.UserName, user.UserId);

            return user; // Повертаємо доданого користувача
        }

        public async Task DeleteUserAsync(int userId)
        {
            _logger.LogInformation("Attempting to delete user with ID: {UserId}.", userId);
            var userToDelete = await _db.Users.FindAsync(userId);

            if (userToDelete == null)
            {
                _logger.LogWarning("Deletion failed: User with ID {UserId} not found.", userId);
                throw new NotFoundException(nameof(User), userId);
            }

            // Якщо каскадне видалення налаштоване в моделі/контексті БД для зв'язку User -> Score/Levels,
            // то видалення User автоматично видалить пов'язані записи.
            // Якщо ні, потрібно видалити їх вручну ПЕРЕД видаленням User.
            // Приклад ручного видалення (якщо каскадне видалення НЕ налаштоване):
            // var scoreToDelete = await _db.Scores.FirstOrDefaultAsync(s => s.UserId == userId);
            // if (scoreToDelete != null) _db.Scores.Remove(scoreToDelete);
            // var lvlToDelete = await _db.Levels.FirstOrDefaultAsync(l => l.UserId == userId);
            // if (lvlToDelete != null) _db.Levels.Remove(lvlToDelete);


            _db.Users.Remove(userToDelete); // Видаляємо користувача
            await _db.SaveChangesAsync(); // Зберігаємо зміни (включно з каскадними, якщо налаштовані)
            _logger.LogInformation("User with ID {UserId} deleted successfully.", userId);
        }

        // Оптимізовано: вибираємо тільки UserName
        public async Task<string?> GetUsernameAsync(int userId)
        {
            _logger.LogInformation("Attempting to get username for user ID: {UserId}.", userId);
            // Використовуйте Select та FirstOrDefaultAsync для отримання тільки потрібного поля
            var username = await _db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync();

            if (username == null)
            {
                _logger.LogInformation("Username not found for user ID: {UserId}.", userId);
            }
            else
            {
                _logger.LogInformation("Username '{Username}' found for user ID: {UserId}.", username, userId);
            }

            // FirstOrDefaultAsync поверне null, якщо користувача не знайдено
            return username;
        }
        public async Task<string?> GetEmailAsync(int userId)
        {
            _logger.LogInformation("Attempting to get email for user ID: {UserId}.", userId);
            // Використовуйте Select та FirstOrDefaultAsync для отримання тільки потрібного поля
            var email = await _db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            if (email == null)
            {
                _logger.LogInformation("Email not found for user ID: {UserId}.", userId);
            }
            else
            {
                _logger.LogInformation("Email '{Email}' found for user ID: {UserId}.", email, userId);
            }

            return email;
        }

        // --- Score Methods ---

        // Змінюємо тип повернення на Task<EditScoreResult>
        public async Task<EditScoreResult> EditScoreAsync(int userId, int score)
        {
            _logger.LogInformation("Attempting to edit score for user ID: {UserId} with value: {AttemptedScore}.", userId, score);

            // Валідація від'ємного рахунку
            if (score < 0)
            {
                _logger.LogWarning("Attempted to set negative score {AttemptedScore} for user ID: {UserId}.", score, userId);
                // Кидаємо виняток, який контролер може обробити як BadRequest
                throw new ArgumentException("Score cannot be negative.", nameof(score));
            }

            // Шукаємо запис рахунку для користувача
            // Припускаємо UserId є первинним ключем для таблиці Scores
            var scoreRecord = await _db.Scores.FindAsync(userId);

            if (scoreRecord == null)
            {
                // Запису рахунку для цього користувача ще немає.
                // Оскільки AddUserAsync створює початковий запис Score,
                // цей сценарій *може* статися, якщо початковий запис не був створений
                // або був видалений окремо.
                // Ми можемо створити його тут, якщо користувач існує.

                // Перевіряємо існування користувача (хоча зазвичай UserID з токена гарантує це)
                // Якщо у вас UserID з токена завжди відповідає існуючому користувачу,
                // можна припустити, що User завжди існує, і не кидати NotFoundException(nameof(User)).
                // Замість цього, ми просто створюємо запис Score.
                var userExists = await _db.Users.AnyAsync(u => u.UserId == userId);
                if (!userExists)
                {
                    _logger.LogWarning("Cannot create score record: User with ID {UserId} not found in Users table.", userId);
                    throw new NotFoundException(nameof(User), userId); // Користувача не існує взагалі
                }


                // Користувач існує, але запису Score немає. Створюємо новий.
                var newScoreRecord = new Score // Припускаємо Score має властивості UserId та ScoreValue
                {
                    UserId = userId,
                    ScoreValue = score,
                    // Якщо у Score є навігаційна властивість User, і вам потрібно її встановити:
                    // User = await _db.Users.FindAsync(userId) // Це може бути зайвим запитом, якщо User вже відстежується
                };

                _db.Scores.Add(newScoreRecord);
                await _db.SaveChangesAsync(); // Зберігаємо новий запис

                _logger.LogInformation("New score record created for user ID: {UserId} with value: {AttemptedScore}.", userId, score);

                // Повертаємо результат, що вказує на створення
                return new EditScoreResult
                {
                    UpdatedOrCreated = true,
                    FinalScore = newScoreRecord.ScoreValue,
                    StatusMessage = "Рахунок успішно збережено (створено новий запис).",
                    PreviousScore = null, // Null означає, що попереднього рахунку не було
                    AttemptedScore = score
                };
            }
            else
            {
                // Запис рахунку для користувача вже існує. Порівнюємо рахунки.
                if (score > scoreRecord.ScoreValue)
                {
                    // Новий рахунок більший за існуючий. Оновлюємо.
                    var previousScoreValue = scoreRecord.ScoreValue; // Запам'ятовуємо старий рахунок
                    scoreRecord.ScoreValue = score; // Встановлюємо новий рахунок

                    // EF Core відстежує зміни в scoreRecord, тому SaveChangesAsync оновить запис в БД
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Score updated for user ID: {UserId} from {PreviousScore} to {FinalScore}.", userId, previousScoreValue, score);

                    // Повертаємо результат, що вказує на оновлення
                    return new EditScoreResult
                    {
                        UpdatedOrCreated = true,
                        FinalScore = scoreRecord.ScoreValue, // Це вже оновлене значення
                        StatusMessage = "Рахунок успішно оновлено.",
                        PreviousScore = previousScoreValue, // Повертаємо старий рахунок
                        AttemptedScore = score
                    };
                }
                else
                {
                    // Новий рахунок не більший (менший або дорівнює) за існуючий.
                    // Не виконуємо оновлення в базі даних.
                    _logger.LogInformation($"User {userId} attempted to set score {score}, which is not higher than current score {scoreRecord.ScoreValue}. No database update performed.");

                    // Повертаємо результат, що вказує на відсутність оновлення
                    return new EditScoreResult
                    {
                        UpdatedOrCreated = false, // Не було оновлення/створення
                        FinalScore = scoreRecord.ScoreValue, // Повертаємо поточний (не змінений) рахунок
                        StatusMessage = $"Новий рахунок ({score}) не більший за поточний ({scoreRecord.ScoreValue}). Оновлення не виконано.",
                        PreviousScore = scoreRecord.ScoreValue,
                        AttemptedScore = score
                    };
                }
            }
        }


        public async Task<int?> GetScoreAsync(int userId)
        {
            _logger.LogInformation("Attempting to get score for user ID: {UserId}.", userId);
            // Оптимізація через Select, якщо потрібне тільки значення
            var scoreValue = await _db.Scores
                .Where(s => s.UserId == userId) // Припускаючи userId - це ключ або FK
                .Select(s => (int?)s.ScoreValue) // Проектуємо в nullable int
                .FirstOrDefaultAsync();

            if (scoreValue == null)
            {
                _logger.LogInformation("Score record not found for user ID: {UserId}.", userId);
            }
            else
            {
                _logger.LogInformation("Score {ScoreValue} found for user ID: {UserId}.", scoreValue.Value, userId);
            }

            // Поверне null, якщо запис Score не знайдено
            return scoreValue;
        }

        public async Task<Score[]> GetLeaderBoard()
        {
            _logger.LogInformation("Fetching leaderboard data.");
            // Вибираємо топ-10 рахунків, сортуємо за спаданням
            var leaderBord = await _db.Scores
                // .Select(s => s) // Явно вибираємо Score об'єкт (або його проекцію, якщо потрібні не всі поля)
                .OrderByDescending(s => s.ScoreValue)
                .Take(10)
                .ToArrayAsync(); // Використовуйте ToArrayAsync або ToListAsync для виконання запиту

            _logger.LogInformation("Fetched {Count} leaderboard entries.", leaderBord.Length);
            return leaderBord;
        }

        // --- Level Methods ---

        public async Task EditLevelAsync(int userId, string lvl)
        {
            _logger.LogInformation("Attempting to edit level for user ID: {UserId} with value: {LevelValue}.", userId, lvl);
            var lvlToUpdate = await _db.Levels.FindAsync(userId); // Припускаємо userId є PK для Level

            if (lvlToUpdate == null)
            {
                // Аналогічно до EditScore, перевіряємо User або кидаємо виняток
                var userExists = await _db.Users.AnyAsync(u => u.UserId == userId);
                if (!userExists)
                {
                    _logger.LogWarning("Cannot create level record: User with ID {UserId} not found in Users table.", userId);
                    throw new NotFoundException(nameof(User), userId);
                }
                _logger.LogWarning("Level record not found for user ID: {UserId}.", userId);
                throw new NotFoundException(nameof(Levels), userId); // Запис рівня не знайдено, хоча користувач існує
            }

            if (string.IsNullOrEmpty(lvl)) // Перевірка на null або порожній рядок
            {
                _logger.LogWarning("Attempted to set empty or null level string for user ID: {UserId}.", userId);
                throw new ArgumentNullException(nameof(lvl));
            }

            // Припускаємо, що логіка гри гарантує коректний формат рядка `lvl`
            // Якщо формат може бути неправильним, тут варто додати валідацію рядка `lvl`

            lvlToUpdate.CompletedLevels = lvl; // Припускаємо, що поле називається CompletedLevels
            await _db.SaveChangesAsync();
            _logger.LogInformation("Level updated for user ID: {UserId} to {LevelValue}.", userId, lvl);
        }

        public async Task<bool[]?> GetLevelAsync(int userId)
        {
            _logger.LogInformation("Attempting to get levels for user ID: {UserId}.", userId);
            // Оптимізація через Select
            string? levelsString = await _db.Levels
                .Where(l => l.UserId == userId) // Припускаючи userId - це ключ або FK
                .Select(l => l.CompletedLevels)
                .FirstOrDefaultAsync(); // Використовуйте FirstOrDefaultAsync для nullable результату

            // Поверне null, якщо запис Level не знайдено
            if (levelsString == null)
            {
                _logger.LogInformation("Level record not found for user ID: {UserId}. Returning null.", userId);
                return null;
            }

            // Обробка отриманого рядка
            // Перевірка на null додана вище, тому levelsString точно не null тут
            try
            {
                // Ваша поточна логіка обробки рядка
                levelsString = levelsString.TrimEnd('}').TrimStart('{').Replace("f", "false").Replace("t", "true");
                var boolArray = levelsString.Split(',').Select(n => Convert.ToBoolean(n)).ToArray();
                _logger.LogInformation("Successfully parsed levels for user ID: {UserId}. Found {Count} levels.", userId, boolArray.Length);
                return boolArray;
            }
            catch (Exception ex)
            {
                // Обробка помилок парсингу, якщо формат рядка CompletedLevels неочікуваний
                _logger.LogError(ex, "Error parsing levels string '{LevelsString}' for user ID: {UserId}.", levelsString, userId);
                // Залежно від вимог, можна кинути виняток, повернути null або порожній масив
                throw new InvalidOperationException($"Failed to parse levels string for user ID {userId}.", ex);
            }
        }
    }
}