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
using static System.Runtime.InteropServices.JavaScript.JSType;


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
        Task EditScoreAsync(int userId, int score);


        Task<Score[]> GetLeaderBoard();

        Task<bool[]?> GetLevelAsync(int userId); // Може повернути null
        Task EditLevelAsync(int userId, string lvl);
        Task<User?> AuthenticateUserAsync(string username, string password); // Повертає User при успіху, null при невдачі
    }

    // Реалізація сервісу з async/await та обробкою помилок
    public class MainService : IMainService
    {
        // Використовуйте стандартне іменування PascalCase для класів
        private readonly mainDBcontext _db;

        // Використання primary constructor (C# 12 / .NET 8+)
        public MainService(mainDBcontext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // --- User Methods ---
        public async Task<User?> AuthenticateUserAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null; // Неправильні вхідні дані
            }

            // Знаходимо користувача за ім'ям (регистрозалежне порівняння за замовчуванням)
            // Якщо потрібне регістронезалежне: u.UserName.ToLower() == username.ToLower()
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == username);

            if (user == null)
            {
                return null;
            }

            //    user.Password - це збережений хеш з бази даних
            //    password - це простий пароль, введений користувачем під час логіну
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);

            if (isPasswordValid)
            {
                return user;
            }
            else
            {
                return null;
            }
        }

        public async Task<User> AddUserAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            // перевірка на унікальність UserName перед додаванням
            if (await _db.Users.AnyAsync(u => u.UserName == user.UserName))
            {
                throw new InvalidOperationException($"User with username '{user.UserName}' already exists.");
            }

            // --- Хешування Пароля ---
            // BCrypt.HashPassword автоматично генерує сіль (salt) і включає її в хеш
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _db.Users.Add(user); // EF Core автоматично відстежує зміни
            _db.SaveChanges();
            _db.Scores.Add(new Score { UserId = user.UserId, UserName = user.UserName, ScoreValue = 0, User = user });
            _db.Levels.Add(new Levels { UserId = user.UserId, CompletedLevels = "f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f", User = user });
            await _db.SaveChangesAsync();
            // Після SaveChanges, user матиме згенерований ID (якщо він генерується БД)
            return user;
        }

        public async Task DeleteUserAsync(int userId)
        {
            var userToDelete = await _db.Users.FindAsync(userId);

            if (userToDelete == null)
            {
                throw new NotFoundException(nameof(User), userId);
            }

            // Припущення: Score та Level мають UserId як PK або налаштоване каскадне видалення.
            // Якщо UserId НЕ є PK для Score/Level, потрібно знайти їх інакше:
            // var scoreToDelete = await _db.Scores.FirstOrDefaultAsync(s => s.UserId == userId);
            // var lvlToDelete = await _db.Levels.FirstOrDefaultAsync(l => l.UserId == userId);
            //
            // // Якщо каскадне видалення не налаштоване, видаляємо залежні сутності вручну:
            // if (scoreToDelete != null) _db.Scores.Remove(scoreToDelete);
            // if (lvlToDelete != null) _db.Levels.Remove(lvlToDelete);

            _db.Users.Remove(userToDelete); // Видаляємо користувача
            await _db.SaveChangesAsync(); // Зберігаємо зміни (включно з каскадними, якщо налаштовані)
        }

        // Оптимізовано: вибираємо тільки UserName
        public async Task<string?> GetUsernameAsync(int userId)
        {
            // FindAsync завантажує всю сутність. Якщо потрібне тільки одне поле, краще так:
            var username = await _db.Users
                .Where(u => u.UserId == userId) // Припускаючи, що поле називається UserId
                .Select(u => u.UserName)
                .FirstOrDefaultAsync();

            // FirstOrDefaultAsync поверне null, якщо користувача не знайдено
            return username;
            // Якщо потрібно кидати виняток, коли не знайдено:
            // if (username == null)
            // {
            //     throw new NotFoundException(nameof(User), userId);
            // }
            // return username;
        }
        public async Task<string?> GetEmailAsync(int userId)
        {
            // FindAsync завантажує всю сутність. Якщо потрібне тільки одне поле, краще так:
            var email = await _db.Users
                .Where(u => u.UserId == userId) // Припускаючи, що поле називається UserId
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            
            return email;
            
        }

        // --- Score Methods ---

        public async Task EditScoreAsync(int userId, int score)
        {
            var scoreToUpdate = await _db.Scores.FindAsync(userId); // Припускаємо userId є PK для Score

            if (scoreToUpdate == null)
            {
                // Якщо Score пов'язаний з User, можливо, краще перевірити існування User
                var userExists = await _db.Users.AnyAsync(u => u.UserId == userId);
                if (!userExists) throw new NotFoundException(nameof(User), userId);
                // Якщо юзер є, а Score немає - можливо, створити новий запис Score? Або кинути інший виняток.
                // Наразі кидаємо NotFoundException для Score
                throw new NotFoundException(nameof(Score), userId);
            }

            scoreToUpdate.ScoreValue = score; // Припускаємо, що поле називається ScoreValue
            // _db.Update(scoreToUpdate); // Явно викликати Update зазвичай не потрібно, EF Core відстежує зміни
            await _db.SaveChangesAsync();
        }
        

        public async Task<int?> GetScoreAsync(int userId)
        {
            // Оптимізація через Select, якщо потрібне тільки значення
            var scoreValue = await _db.Scores
                .Where(s => s.UserId == userId) // Припускаючи userId - це ключ або FK
                .Select(s => (int?)s.ScoreValue) // Проектуємо в nullable int
                .FirstOrDefaultAsync();

            // Поверне null, якщо запис Score не знайдено
            return scoreValue;
        }

        public async Task<Score[]> GetLeaderBoard()
        {
            var leaderBord = await _db.Scores
                //.Select(s => s.ScoreValue)
                .OrderByDescending(s => s.ScoreValue)
                .Take(10)
                .ToArrayAsync();
            return leaderBord;
        }

        // --- Level Methods ---

        public async Task EditLevelAsync(int userId, string lvl)
        {
            var lvlToUpdate = await _db.Levels.FindAsync(userId); // Припускаємо userId є PK для Level

            if (lvlToUpdate == null)
            {
                // Аналогічно до EditScore, перевіряємо User або кидаємо виняток
                var userExists = await _db.Users.AnyAsync(u => u.UserId == userId);
                if (!userExists) throw new NotFoundException(nameof(User), userId);
                throw new NotFoundException(nameof(Levels), userId);
            }
            if (lvl == null)
            {
                throw new ArgumentNullException(nameof(lvl));
            }

            lvlToUpdate.CompletedLevels = lvl; // Припускаємо, що поле називається CompletedLevels
            await _db.SaveChangesAsync();
        }

        public async Task<bool[]?> GetLevelAsync(int userId)
        {
            // Оптимізація через Select
            string levels = await _db.Levels
                .Where(l => l.UserId == userId) // Припускаючи userId - це ключ або FK
                .Select(l => l.CompletedLevels)
                .FirstOrDefaultAsync();
            levels = levels.TrimEnd('}').TrimStart('{').Replace("f","false").Replace("t", "true");
            // Поверне null, якщо запис Level не знайдено
            return levels.Split(',').Select(n => Convert.ToBoolean(n)).ToArray();
        }
    }
}