using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web_kursachsem4.Data.Models
{
    public class User
    {
        // Первинний ключ
        public int UserId { get; set; } 

        public string UserName { get; set; }
        public string Email { get; set; } 
        public string Password { get; set; }

        // Навігація
        public virtual Score Score { get; set; }


    }

    public class Score
    {
        // [Key] // Можна вказати тут або через Fluent API
        // [ForeignKey("User")] // Можна вказати тут або через Fluent API
        public int UserId { get; set; } // PK та FK
        public string UserName { get; set; }
        public int ScoreValue { get; set; }
        public virtual User User { get; set; }
        public virtual User UserByName { get; set; }


    }

    public class EditScoreResult
    {
        // Чи було рахунок фактично оновлено або створено в результаті цього виклику
        public bool UpdatedOrCreated { get; set; }

        // Фінальне значення рахунку після виконання операції (новий рахунок, якщо оновлено/створено, або старий, якщо не оновлено)
        public int FinalScore { get; set; }

        // Повідомлення про результат операції (наприклад, "Рахунок оновлено", "Рахунок не оновлено, оскільки не більший", "Створено новий запис")
        public string StatusMessage { get; set; }

        // Значення рахунку до виконання операції (null, якщо запис був створений)
        public int? PreviousScore { get; set; }

        // Значення рахунку, яке намагалися встановити
        public int AttemptedScore { get; set; }
    }
}
