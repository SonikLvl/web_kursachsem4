using Microsoft.AspNetCore.Mvc;
using web_kursachsem4.Services;
using web_kursachsem4.Data.Models;
using System.Threading.Tasks;
using web_kursachsem4.Exceptions; 
using System.Collections.Generic;
using web_kursachsem4.Web.RequestModels;
using Microsoft.Extensions.Configuration; 
using System.IdentityModel.Tokens.Jwt; 
using System.Security.Claims;          
using Microsoft.IdentityModel.Tokens;   
using System.Text;                       
using Microsoft.AspNetCore.Authorization;

namespace web_kursachsem4.Web
{
    [ApiController]
    [Route("api")] // Базовий маршрут
    public class MainController : ControllerBase
    {
        private readonly ILogger<MainController> _logger;
        private readonly IMainService _mainService;
        private readonly IConfiguration _configuration;

        public MainController(ILogger<MainController> logger, IMainService mainService, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mainService = mainService ?? throw new ArgumentNullException(nameof(mainService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        // Допоміжний метод для генерації токена
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim> // Використовуємо List<Claim> для гнучкості
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()), // Subject = User ID
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),   // Унікальне  username
                new Claim(JwtRegisteredClaimNames.Email, user.Email),           // Email
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Унікальний ID токена

            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4), // Наприклад, токен дійсний 4 години
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        // Метод для отримання ID поточного користувача з токена
        private int? GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                _logger.LogWarning("Could not find NameIdentifier claim in token for current user.");
                return null; // Або кинути виняток, або повернути помилку в викликаючому методі
            }

            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            else
            {
                _logger.LogError("Could not parse user ID '{UserIdString}' from token claim.", userIdString);
                return null; // Або кинути виняток
            }
        }


        // --- Ендпоінти для користувачів ---

        // POST  /api/auth/login
        [HttpPost("auth/login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(User))] // Або тип вашої відповіді при успіху (напр. TokenResponse)
        [ProducesResponseType(StatusCodes.Status400BadRequest)]         // Неправильний запит
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]     // Неправильні логін/пароль
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Внутрішня помилка
        public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest loginRequest) // Або ActionResult<TokenResponse>
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

                    // --- Генерація JWT токена ---
                    var tokenString = GenerateJwtToken(authenticatedUser);
                    // --------------------------

                    return Ok(new TokenResponse { Token = tokenString }); // Повертаємо токен
                }
                else
                {
                    _logger.LogWarning("Authentication failed for user {Username}.", loginRequest.Username);
                    return Unauthorized(new { message = "Invalid username or password." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the login process for user {Username}.", loginRequest.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred during login.");
            }
        }



        // POST /api/users
        [HttpPost("users")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(User))] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)]    
        [ProducesResponseType(StatusCodes.Status409Conflict)]      
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]         
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
                _logger.LogInformation($"User {userRequest.UserName} created with ID {addedUser.UserId}", addedUser.UserName, addedUser.UserId);
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

        // GET /api/{userId}/username
        [HttpGet("me/username")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<string>> GetUsername()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(); // Якщо ID не вдалося отримати з токена

            _logger.LogInformation($"User {userId} requesting their own username", userId.Value);

            try
            {
                var username = await _mainService.GetUsernameAsync(userId.Value);
                if (username == null)
                {
                    _logger.LogWarning($"Authenticated user {userId} not found in DB for GetMyUsername.", userId.Value);
                    return NotFound("User data not found.");
                }
                return Ok(username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting own username for user {userId}", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // DELETE /api/me
        [HttpDelete("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Успішне видалення
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUser()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            _logger.LogInformation("User {UserId} attempting to delete their own account", userId.Value);

            try
            {
                await _mainService.DeleteUserAsync(userId.Value);
                _logger.LogInformation("User {UserId} deleted successfully.", userId.Value);
                return NoContent();
            }
            catch (NotFoundException nfex) // Якщо DeleteUserAsync кидає NotFoundException
            {
                _logger.LogWarning(nfex, "Attempted to delete own account for user {UserId}, but user was not found.", userId.Value);
                // Можливо, варто повернути 401 або 500, бо це дивна ситуація
                return NotFound(nfex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting own account for user {UserId}", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deleting the account.");
            }
        }

        // --- Ендпоінти для Score ---

        // PUT /api/{userId}/score
        // Використовуємо PUT, оскільки замінюємо значення повністю.
        // Якщо б додавали до існуючого, PATCH був би кращим.
        [HttpPut("me/score")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Успішне оновлення
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EditScore([FromBody] ScoreChangeRequest scoreChangeRequest) // Рахунок передається в тілі запиту
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            _logger.LogInformation("User {UserId} attempting to edit their score", userId.Value);

            if (scoreChangeRequest.Score < 0)
            {
                return BadRequest("Score cannot be negative.");
            }
            try
            {
                // Викликаємо сервіс з ID з токена
                await _mainService.EditScoreAsync(userId.Value, scoreChangeRequest.Score);
                _logger.LogInformation($"Score for user {userId} updated to {scoreChangeRequest.Score}", userId.Value, scoreChangeRequest.Score);
                return NoContent();
            }
            catch (NotFoundException nfex)
            {
                _logger.LogWarning(nfex, $"Attempted to edit score for user {userId}, but user/score record not found.", userId.Value);
                return NotFound(nfex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing own score for user {userId}", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while editing the score.");
            }
        }

        // GET /api/{userId}/score
        [HttpGet("me/score")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Якщо юзера або рахунку немає
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<int>> GetScore()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            _logger.LogInformation($"User {userId} requesting their own score", userId.Value);

            try
            {
                // Викликаємо сервіс з ID з токена
                var score = await _mainService.GetScoreAsync(userId.Value);
                if (score == null)
                {
                    _logger.LogInformation($"Score data not found for user {userId}.", userId.Value);
                    return NotFound("Score data not found.");
                }
                return Ok(score.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting own score for user {userId}", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the score.");
            }
        }
        // GET /api/leaderboard
        [HttpGet("leaderboard")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        [ProducesResponseType(StatusCodes.Status404NotFound)] 
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Score[]>> GetLeaderboard()
        {

            try
            {
                // Викликаємо сервіс з ID з токена
                var score = await _mainService.GetLeaderBoard();
                if (score == null)
                {
                    _logger.LogInformation($"No data for leaderboard");
                    return NotFound("Score data not found.");
                }
                return Ok(score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Exception Caught");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the score.");
            }
        }

        // --- Ендпоінти для Level ---

        // PUT /api/{userId}/levels
        [HttpPut("me/levels")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EditMyLevels(LvlsChangeRequest lvlsChangeRequest)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            _logger.LogInformation($"User {userId} attempting to edit their levels", userId.Value);

            if (lvlsChangeRequest.Levels == null)
            {
                return BadRequest("Level data cannot be null.");
            }
            try
            {
                // Викликаємо сервіс з ID з токена
                await _mainService.EditLevelAsync(userId.Value, lvlsChangeRequest.Levels);
                _logger.LogInformation($"Levels for user {userId} updated.", userId.Value);
                return NoContent();
            }
            catch (NotFoundException nfex)
            {
                _logger.LogWarning(nfex, $"Attempted to edit levels for user {userId}, but user/level record not found.", userId.Value);
                return NotFound(nfex.Message);
            }
            catch (ArgumentNullException anex)
            {
                _logger.LogWarning(anex, $"Attempted to edit levels with null value for user {userId}.", userId.Value);
                return BadRequest(anex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing own levels for user {userId}", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while editing levels.");
            }
        }

        // GET /api/me/levels (Отримати рівні поточного користувача)
        [HttpGet("me/levels")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool[]))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<bool[]>> GetMyLevels()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            _logger.LogInformation($"User {userId} requesting their own levels", userId.Value);

            try
            {
                // Викликаємо сервіс з ID з токена
                var levels = await _mainService.GetLevelAsync(userId.Value);
                _logger.LogInformation($"Level data {levels} {levels.GetType}");

                if (levels == null)
                {
                    _logger.LogInformation($"Level data not found for user {userId}.", levels);
                    return NotFound($"Level data not found.");
                }
                return Ok(levels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting own levels for user {userId}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving levels.");
            }
        }

        // --- Захищений ендпоінт для отримання даних ІНШОГО користувача ---

        // GET /api/users/{userId}/username (Отримати ім'я будь-якого користувача за ID)
        [HttpGet("users/{userId:int}/username")]
        [Authorize] // Вимагає лише автентифікації, щоб дивитись імена інших
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<string>> GetUsername(int userId) // Приймає userId з URL
        {
            var currentUserId = GetCurrentUserId(); // Отримуємо ID того, хто запитує, для логування
            _logger.LogInformation("User {CurrentUserId} requesting username for {TargetUserId}", currentUserId?.ToString() ?? "Unknown", userId);

            try
            {
                // Викликаємо сервіс з ID з URL
                var username = await _mainService.GetUsernameAsync(userId);
                if (username == null)
                {
                    return NotFound($"User with ID {userId} not found.");
                }
                return Ok(username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting username for user {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // Можна видалити ваш тестовий ендпоінт або залишити для швидкої перевірки
        [HttpGet("/api/test")]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)] // Не показувати в Swagger UI
        public ActionResult GetTest()
        {
            return Ok("API is running ok!");
        }
    }
}