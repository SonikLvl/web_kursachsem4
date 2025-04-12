using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;
using web_kursachsem4.Web;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;
using web_kursachsem4.Services;

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
        [HttpGet("/api/test")]
        public ActionResult GetTest()
        {
            var userName = _mainService.GetUsername(1);
            return Ok(userName);
        }
        [HttpGet("/api/{id}")]
        public async Task<IActionResult> GetAsync(int id)
        {
            var username = await _mainService.GetAsync(id);
            if (username == null)
            {
                return null;

            }
            return Ok(username);

        }
    }
}

