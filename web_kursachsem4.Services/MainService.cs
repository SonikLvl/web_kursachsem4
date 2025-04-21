using System;
using web_kursachsem4.Data.Models;
using web_kursachsem4.Data;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace web_kursachsem4.Services
{
    public class MainService : IMainService
    {
        private readonly mainDBcontext _db;
        public MainService(mainDBcontext db) {
            _db = db;
        }
        public void AddUser(User user)
        {
            _db.Add(user);
            _db.SaveChanges();
        }

        public void DeleteUser(int userId)
        {
            var userToDelete = _db.Users.Find(userId);
            var scoreToDelete = _db.Scores.Find(userId);
            var lvlToDelete = _db.Levels.Find(userId);

            if(userToDelete != null && scoreToDelete != null && lvlToDelete != null)
            {
                _db.Remove(userToDelete);
                //_db.Remove(scoreToDelete);
                //_db.Remove(lvlToDelete);
            }

            throw new InvalidProgramException("User doesnt exit - cannot delete it.");
        }

        public void EditLevel(int userId, List<bool> lvl)
        {
            var lvlToUpdate = _db.Levels.Find(userId);
            lvlToUpdate.CompletedLevels = lvl;
            _db.SaveChanges();
        }


        public void EditScore(int userId, int score) //?????
        {
            var scoreToUpdate = _db.Scores.Find(userId);
            scoreToUpdate.ScoreValue = score;
            _db.SaveChanges();
        }

        public List<bool> GetLevel(int userId)
        {
            return _db.Levels.Find(userId).CompletedLevels;
        }

        public int GetScore(int userId)
        {
            return _db.Scores.Find(userId).ScoreValue;
        }

        public string GetUsername(int userId)
        {
            return _db.Users.Find(userId).UserName;
        }
        public string GetPassword(int userId)
        {
            return _db.Users.Find(userId).Password;
        }


        /*public async Task<string> GetAsync(int id)
        {
            //var username = await _db.User.FromSqlRaw($"SELECT username FROM public.users \r\n WHERE userid=@id", new NpgsqlParameter<Int32>("id", id)).FirstOrDefaultAsync();//  as us \r\n WHERE us.userid=@id", new NpgsqlParameter<Int32>("id", id)
            /*var username = await _db.Database
            .SqlQuery<string>($"""
                            SELECT "BlogId" AS "x" FROM "Blogs"
                            """)
            .Where(x => x.Use.)
            .ToListAsync();
            var username = _db.Users.Find(id).UserName;

            if (username == null)
            {
                return null;
            }
            return username;
            return new GetUsername
            {
                Id = username.userid,
                username = username.username
            };
    }*/
    }

    public interface IMainService
    {
        //Task<string> GetAsync(int id);
        public string GetUsername(int userId);
        public string GetPassword(int userId);
        public void AddUser(User user);
        //public void EditUser(User user);
        public void DeleteUser(int userId);

        public void EditScore(int userId, int score);
        public int GetScore(int userId);


        public void EditLevel(int userId, List<bool> lvl);
        public List<bool> GetLevel(int userId);

    }
}
