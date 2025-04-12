using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using web_kursachsem4.Data.Models;

namespace web_kursachsem4.Data
{
    public class mainDBcontext : DbContext
    {
        public mainDBcontext(DbContextOptions options) : base(options)
        {
        }

        /*public mainDBcontext()
        {
        }*/

        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<Score> Score { get; set; }
        public virtual DbSet<Levels> Levels { get; set; }

    }
}
