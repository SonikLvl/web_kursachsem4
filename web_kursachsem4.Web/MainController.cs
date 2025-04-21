using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;
using web_kursachsem4.Web;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;
using web_kursachsem4.Services;
using web_kursachsem4.Data.Models;

namespace web_kursachsem4.Web
{
    [ApiController]
    public class MainController : ControllerBase
    {
        private readonly ILogger<MainController> _logger;
        private readonly IMainService _mainService;
        public MainController(ILogger<MainController> logger, IMainService mainService)
        {
            _logger = logger;
            _mainService = mainService;
        }
        [HttpGet("/api/test")] //nice
        public ActionResult GetTest()
        {
            return Ok("ok");
        }

        //-------TESTING section------ add more boundries
        //testing pages
        [HttpGet("/test/GetUsername/{userId}")] //nice
        public ActionResult GetUsername(int userId)
        {
            return Ok(_mainService.GetUsername(userId));
        }
        [HttpGet("/test/GetPassword/{userId}")]//nice
        public ActionResult GetPassword(int userId)
        {
            return Ok(_mainService.GetPassword(userId));
        }
        [HttpGet("/test/AddUser/{user}")] 
        public ActionResult AddUser(User user)
        {
            //User u = new User 
            _mainService.AddUser(user);
            return Ok("user added "+ user);
        }
        [HttpGet("/test/DeleteUser/{userId}")]
        public ActionResult DeleteUser(int userId) // InvalidProgramException
        {
            _mainService.DeleteUser(userId);
            return Ok("user deleted " + userId);

        }
        //SCORE test
        [HttpGet("/test/EditScore/{userId}")]
        public ActionResult EditScore(int userId)
        {
            _mainService.EditScore(userId, 100);
            return Ok("score edited " + 100);
        }
        [HttpGet("/test/GetScore/{userId}")]
        public ActionResult GetScore(int userId) //test if exists
        {
            return Ok("score got " + _mainService.GetScore(userId));
        }
        //LEVEL test
        [HttpGet("/test/EditLevel/{userId}")]
        public ActionResult EditLevel(int userId)
        {
            _mainService.EditLevel(userId, new List<bool> { true, false, true});
            return Ok("levels edited t f t");
        }
        [HttpGet("/test/GetLevel/{userId}")]
        public ActionResult GetLevel(int userId)
        {
            return Ok("levels got " + _mainService.GetLevel(userId));
        }
    }
}

