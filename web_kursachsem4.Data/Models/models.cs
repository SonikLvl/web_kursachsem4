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
        public virtual Levels Levels { get; set; }
    }

    public class Score
    {
        // [Key] // Можна вказати тут або через Fluent API
        // [ForeignKey("User")] // Можна вказати тут або через Fluent API
        public int UserId { get; set; } // PK та FK
        public int ScoreValue { get; set; }
        public virtual User User { get; set; }
    }

    public class Levels
    {
        // Первинний ключ Levels ТАКОЖ є зовнішнім ключем до User
        // [Key]
        // [ForeignKey("User")]
        public int UserId { get; set; } // PK та FK

        // Ця властивість буде зберігатися як JSON у базі даних
        public string CompletedLevels { get; set; } //bool array

        // Навігаційна властивість
        public virtual User User { get; set; }
    }
}
