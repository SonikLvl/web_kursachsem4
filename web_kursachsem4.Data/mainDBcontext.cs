using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using web_kursachsem4.Data.Models;

namespace web_kursachsem4.Data 
{
    public class mainDBcontext : DbContext 
    {
        public mainDBcontext(DbContextOptions<mainDBcontext> options) : base(options)
        {
        }

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Score> Scores { get; set; }
        public virtual DbSet<Levels> Levels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);

                entity.Property(u => u.UserName).IsRequired().HasMaxLength(100);
                entity.HasIndex(u => u.UserName).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique(); 
                entity.Property(u => u.Email).IsRequired();
                entity.Property(u => u.Password).IsRequired();

                // User має один Score, Score має одного User, зовнішній ключ у Score
                entity.HasOne(u => u.Score)
                      .WithOne(s => s.User)
                      .HasForeignKey<Score>(s => s.UserId);
                

                // User має один Levels, Levels має одного User, зовнішній ключ у Levels
                entity.HasOne(u => u.Levels)
                      .WithOne(l => l.User)
                      .HasForeignKey<Levels>(l => l.UserId);
            });

            modelBuilder.Entity<Score>(entity =>
            {
                entity.HasKey(s => s.UserId);

                entity.Property(s => s.ScoreValue).IsRequired();
                entity.HasOne(s => s.UserByName)
                      .WithOne()
                      .HasPrincipalKey<User>(u => u.UserName)
                      .HasForeignKey<Score>(s => s.UserName)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Levels>(entity =>
            {
                entity.HasKey(l => l.UserId);

                /*entity.Property(l => l.CompletedLevels)
                     .HasConversion(
                         v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                         v => JsonSerializer.Deserialize<List<bool>>(v, (JsonSerializerOptions)null)
                     )
                     .IsRequired(); // Робимо поле обов'язковим, якщо логіка цього вимагає
                                    */
                 entity.Property(l => l.CompletedLevels).IsRequired();

            });
        }
    }
}