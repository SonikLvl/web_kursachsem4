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
            var userToDelete = _db.User.Find(userId);
            var scoreToDelete = _db.Score.Find(userId);
            var lvlToDelete = _db.Levels.Find(userId);

            if(userToDelete != null && scoreToDelete != null && lvlToDelete != null)
            {
                _db.Remove(userToDelete);
                _db.Remove(scoreToDelete);
                _db.Remove(lvlToDelete);
            }

            throw new InvalidProgramException("User doesnt exit - cannot delete it.");
        }

        public void EditLevel(int userId, List<bool> lvl)
        {
            var scoreToUpdate = _db.Levels.Find(userId);
            scoreToUpdate.lvl = lvl;
            _db.SaveChanges();
        }


        public void EditScore(int userId, int score) //?????
        {
            var scoreToUpdate = _db.Score.Find(userId);
            scoreToUpdate.score = score;
            _db.SaveChanges();
        }

        public List<bool> GetLevel(int userId, List<bool> lvl)
        {
            return _db.Levels.Find(userId).lvl;
        }

        public int GetScore(int userId, int score)
        {
            return _db.Score.Find(userId).score;
        }

        public string GetUsername(int userId)
        {
            return "dfgsgf";
            //return _db.User.Find(userId).UserName;
            //return  _db.User.FromSql($"select user from  public.users u \r\n--WHERE userid =  {userId} ;").ToString();
            //return _db.User.FromSqlRaw($"select * from  public.users").FirstOrDefaultAsync();
        }
        public async Task<string> GetAsync(int id)
        {
            //var username = await _db.User.FromSqlRaw($"SELECT username FROM public.users \r\n WHERE userid=@id", new NpgsqlParameter<Int32>("id", id)).FirstOrDefaultAsync();//  as us \r\n WHERE us.userid=@id", new NpgsqlParameter<Int32>("id", id)
            /*var username = await _db.Database
            .SqlQuery<string>($"""
                            SELECT "BlogId" AS "x" FROM "Blogs"
                            """)
            .Where(x => x.Use.)
            .ToListAsync();*/
            var username = _db.User.Find(id).username;

            if (username == null)
            {
                return null;
            }
            return username;
            /*return new GetUsername
            {
                Id = username.userid,
                username = username.username
            };*/
        }
    }

    public interface IMainService
    {
        Task<string> GetAsync(int id);
        public string GetUsername(int userId);
        public void AddUser(User user);
        public void DeleteUser(int userId);

        public void EditScore(int userId, int score);
        public int GetScore(int userId, int score);


        public void EditLevel(int userId, List<bool> lvl);
        public List<bool> GetLevel(int userId, List<bool> lvl);

    }
}
