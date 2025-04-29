using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 
using web_kursachsem4.Services;
using web_kursachsem4.Data.Models;
using System.Threading.Tasks;
using web_kursachsem4.Exceptions; 
using System.Collections.Generic;
using web_kursachsem4.Web.RequestModels;

namespace web_kursachsem4.Web
{
    [ApiController]
    [Route("api")] // Базовий маршрут
    public class MainController : ControllerBase
    {
        private readonly ILogger<MainController> _logger;
        private readonly IMainService _mainService;

        public MainController(ILogger<MainController> logger, IMainService mainService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mainService = mainService ?? throw new ArgumentNullException(nameof(mainService));
        }

        // --- Ендпоінти для користувачів ---

        // POST  /api/auth/login
        [HttpPost("auth/login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(User))] // Або тип вашої відповіді при успіху (напр. TokenResponse)
        [ProducesResponseType(StatusCodes.Status400BadRequest)]         // Неправильний запит
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]     // Неправильні логін/пароль
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Внутрішня помилка
        public async Task<ActionResult<User>> Login([FromBody] LoginRequest loginRequest) // Або ActionResult<TokenResponse>
        {
            if (loginRequest == null || string.IsNullOrWhiteSpace(loginRequest.Username) || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                return BadRequest("Username and password are required.");
            }

            try
            {
                var authenticatedUser = await _mainService.AuthenticateUserAsync(loginRequest.Username, loginRequest.Password);

                if (authenticatedUser != null)
                {
                    _logger.LogInformation("User {Username} authenticated successfully.", loginRequest.Username);

                    // Успішна автентифікація!
                    // ЩО ПОВЕРТАТИ?
                    // 1. Просто дані користувача (без хешу пароля!) - як у прикладі нижче.
                    // 2. Або краще: згенерувати JWT токен і повернути його.
                    // 3. Або встановити сесійну куку.

                    // Приклад повернення даних користувача (видаліть пароль перед поверненням!)
                    authenticatedUser.Password = string.Empty; // НІКОЛИ не повертайте хеш клієнту
                    return Ok(authenticatedUser.UserId); 

                    // Приклад повернення JWT (потребує налаштування JWT):
                    // var token = GenerateJwtToken(authenticatedUser);
                    // return Ok(new { Token = token });
                }
                else
                {
                    _logger.LogWarning("Authentication failed for user {Username}.", loginRequest.Username);
                    // Не вказуйте, що саме не так (логін чи пароль) для безпеки
                    return Unauthorized(new { message = "Invalid username or password." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the login process for user {Username}.", loginRequest.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred during login.");
            }
        }

        // GET /api/{userId}/username
        [HttpGet("{userId:int}/username")] // Додаємо обмеження типу :int
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<string>> GetUsername(int userId)
        {
            try
            {
                var username = await _mainService.GetUsernameAsync(userId);
                if (username == null)
                {
                    _logger.LogInformation("Username for user {UserId} not found.", userId);
                    return NotFound($"User with ID {userId} not found.");
                }
                return Ok(username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting username for user {UserId}", userId);
                // Повертаємо 500 Internal Server Error для неочікуваних помилок
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }


        // POST /api
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(User))] // Успішне створення
        [ProducesResponseType(StatusCodes.Status400BadRequest)]                 // Неправильний запит (валідація)
        [ProducesResponseType(StatusCodes.Status409Conflict)]                 // Користувач вже існує
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]         // Внутрішня помилка сервера
        public async Task<ActionResult<User>> AddUser([FromBody] NewUserRequest userRequest) // Дані користувача йдуть у тілі запиту
        {
            if (userRequest == null && !ModelState.IsValid) // Перевірка валідності моделі Якщо не використовуєте [ApiController] - (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                User user = new User
                {
                    UserName = userRequest.UserName,
                    Email = userRequest.Email,
                    Password = userRequest.Password,
                };
                var addedUser = await _mainService.AddUserAsync(user);
                _logger.LogInformation("User {Username} created with ID {UserId}", addedUser.UserName, addedUser.UserId);
                // Повертаємо 201 Created з посиланням на створений ресурс (якщо є GetUserById ендпоінт)
                // або просто повертаємо створеного користувача
                return CreatedAtAction(nameof(GetUsername), new { userId = addedUser.UserId }, addedUser); // Потрібен Get ендпоінт з іменем GetUsername
                // Або просто: return Ok(addedUser); (менш RESTful)
            }
            // Можна додати обробку InvalidOperationException, якщо ви додали перевірку на унікальність
            catch (InvalidOperationException ioex)
            {
                _logger.LogWarning(ioex, "Attempted to add user with existing username.");
                return BadRequest(ioex.Message); // Або Conflict() - 409
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {Username}", userRequest.UserName);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while adding the user.");
            }
        }

        // DELETE /api/{userId}
        [HttpDelete("{userId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Успішне видалення
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                await _mainService.DeleteUserAsync(userId);
                _logger.LogInformation("User {UserId} deleted successfully.", userId);
                return NoContent(); // Стандартна відповідь для успішного DELETE
            }
            catch (NotFoundException nfex)
            {
                _logger.LogWarning(nfex, "Attempted to delete non-existent user {UserId}.", userId);
                return NotFound(nfex.Message); // Повертаємо 404 Not Found
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deleting the user.");
            }
        }

        // --- Ендпоінти для Score ---

        // PUT /api/{userId}/score
        // Використовуємо PUT, оскільки замінюємо значення повністю.
        // Якщо б додавали до існуючого, PATCH був би кращим.
        [HttpPut("{userId:int}/score")]
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Успішне оновлення
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EditScore(int userId, [FromBody] int score) // Рахунок передається в тілі запиту
        {
            // Можна додати валідацію для score (наприклад, >= 0)
            if (score < 0)
            {
                return BadRequest("Score cannot be negative.");
            }
            try
            {
                await _mainService.EditScoreAsync(userId, score);
                _logger.LogInformation("Score for user {UserId} updated to {ScoreValue}", userId, score);
                return NoContent(); // Успішне оновлення, вміст не повертаємо
            }
            catch (NotFoundException nfex)
            {
                _logger.LogWarning(nfex, "Attempted to edit score for non-existent user or score record {UserId}.", userId);
                return NotFound(nfex.Message);
            }
            catch (ArgumentNullException anex) // Якщо score передали як null (хоча int не може бути null)
            {
                _logger.LogWarning(anex, "Attempted to edit score with null value for user {UserId}.", userId);
                return BadRequest(anex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing score for user {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // GET /api/users/{userId}/score
        [HttpGet("{userId:int}/score")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Якщо юзера або рахунку немає
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<int>> GetScore(int userId)
        {
            try
            {
                var score = await _mainService.GetScoreAsync(userId);
                if (score == null)
                {
                    _logger.LogInformation("Score for user {UserId} not found.", userId);
                    // Вирішіть, чи це 404 для юзера чи для рахунку.
                    // Можливо, перевірити існування юзера окремо, якщо потрібно розрізняти.
                    return NotFound($"Score data not found for user ID {userId}.");
                }
                return Ok(score.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting score for user {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // --- Ендпоінти для Level ---

        // PUT /api/users/{userId}/levels
        [HttpPut("{userId:int}/levels")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EditLevel(int userId, [FromBody] List<bool> levels) // Список рівнів передається в тілі
        {
            if (levels == null)
            {
                return BadRequest("Level data cannot be null.");
            }
            try
            {
                await _mainService.EditLevelAsync(userId, levels);
                _logger.LogInformation("Levels for user {UserId} updated.", userId);
                return NoContent();
            }
            catch (NotFoundException nfex)
            {
                _logger.LogWarning(nfex, "Attempted to edit levels for non-existent user or level record {UserId}.", userId);
                return NotFound(nfex.Message);
            }
            catch (ArgumentNullException anex)
            {
                _logger.LogWarning(anex, "Attempted to edit levels with null value for user {UserId}.", userId);
                return BadRequest(anex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing levels for user {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // GET /api/users/{userId}/levels
        [HttpGet("{userId:int}/levels")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<bool>>> GetLevel(int userId)
        {
            try
            {
                var levels = await _mainService.GetLevelAsync(userId);
                if (levels == null)
                {
                    _logger.LogInformation("Level data for user {UserId} not found.", userId);
                    return NotFound($"Level data not found for user ID {userId}.");
                }
                return Ok(levels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting levels for user {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // Можна видалити ваш тестовий ендпоінт або залишити для швидкої перевірки
        [HttpGet("/api/test")]
        [ApiExplorerSettings(IgnoreApi = true)] // Не показувати в Swagger UI
        public ActionResult GetTest()
        {
            return Ok("API is running ok!");
        }
    }
}