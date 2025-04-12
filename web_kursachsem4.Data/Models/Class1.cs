using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace web_kursachsem4.Data.Models
{
    public class User
    {
        [Key]
        public int userid { get; set; }

        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
    }
    public class Score
    {
        [Key]
        public int userid { get; set; }

        public int score { get; set; }

        public User user { get; set; }
    }
    public class Levels
    {
        [Key]
        public int userid { get; set; }

        public List<bool> lvl { get; set; }
        public User user { get; set; }
    }
}
