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
using Microsoft.Extensions.Logging; // Переконайтесь, що є using для логера


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
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]
                 ?? throw new InvalidOperationException("JWT SecretKey is not configured."))); // Перевірка на null
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            // Перевірка на null для Issuer/Audience, якщо вони обов'язкові
            var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
            var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");


            var claims = new List<Claim> // Використовуємо List<Claim> для гнучкості
            {
                // Використовуємо стандартні ClaimTypes
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // Subject = User ID
                new Claim(ClaimTypes.Name, user.UserName),   // Унікальне  username (Name)
                new Claim(ClaimTypes.Email, user.Email),           // Email
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Унікальний ID токена

            };

            var token = new JwtSecurityToken(
                issuer: issuer, // Використовуємо змінну після перевірки
                audience: audience, // Використовуємо змінну після перевірки
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4), // Наприклад, токен дійсний 4 години
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Метод для отримання ID поточного користувача з токена
        private int? GetCurrentUserId()
        {
            // Використовуємо User.FindFirstValue та стандартні ClaimTypes
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier); // Повинно відповідати ClaimTypes.NameIdentifier в GenerateJwtToken
            if (string.IsNullOrEmpty(userIdString))
            {
                _logger.LogWarning("GetCurrentUserId failed: Could not find NameIdentifier claim in token.");
                return null;
            }

            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            else
            {
                _logger.LogError("GetCurrentUserId failed: Could not parse user ID '{UserIdString}' from token claim into an integer.", userIdString);
                return null;
            }
        }


        // --- Ендпоінти для користувачів ---

        // POST  /api/auth/login
        [HttpPost("auth/login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))] // Повертаємо TokenResponse
        [ProducesResponseType(StatusCodes.Status400BadRequest)]         // Неправильний запит (валідація вхідних даних)
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]     // Неправильні логін/пароль (автентифікація не пройдена)
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Внутрішня помилка
        public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest loginRequest)
        {
            // Додано перевірку на null RequestModel та Model State
            if (loginRequest == null || !ModelState.IsValid)
            {
                _logger.LogWarning("Login failed: Invalid login request received.");
                return BadRequest(ModelState); // Повертаємо деталі валідації
            }

            _logger.LogInformation("Login attempt for user: {Username}", loginRequest.Username);

            try
            {
                var authenticatedUser = await _mainService.AuthenticateUserAsync(loginRequest.Username, loginRequest.Password);

                if (authenticatedUser != null)
                {
                    _logger.LogInformation("User '{Username}' authenticated successfully, generating token.", loginRequest.Username);

                    // --- Генерація JWT токена ---
                    var tokenString = GenerateJwtToken(authenticatedUser);
                    // --------------------------

                    return Ok(new TokenResponse { Token = tokenString }); // Повертаємо токен у TokenResponse об'єкті
                }
                else
                {
                    _logger.LogWarning("Authentication failed for user '{Username}': Invalid credentials.", loginRequest.Username);
                    return Unauthorized(new { message = "Invalid username or password." }); // 401 для невірних даних
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the login process for user '{Username}'.", loginRequest.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred during login.");
            }
        }



        // POST /api/users
        [HttpPost("users")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(User))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Валідація вхідних даних
        [ProducesResponseType(StatusCodes.Status409Conflict)]     // Якщо користувач з таким ім'ям вже існує
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<User>> AddUser([FromBody] NewUserRequest userRequest)
        {
            // Додано перевірку на null RequestModel та Model State
            if (userRequest == null || !ModelState.IsValid)
            {
                _logger.LogWarning("AddUser failed: Invalid user request received.");
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Attempting to add new user with username: {Username}, email: {Email}", userRequest.UserName, userRequest.Email);


            try
            {
                User user = new User // Мапуємо RequestModel до моделі БД
                {
                    UserName = userRequest.UserName,
                    Email = userRequest.Email,
                    Password = userRequest.Password, // Сервіс захешує його
                    // Інші поля моделі User, якщо є, можливо, потрібно ініціалізувати
                };
                var addedUser = await _mainService.AddUserAsync(user);

                _logger.LogInformation("User '{Username}' created successfully with ID {UserId}.", addedUser.UserName, addedUser.UserId);

                // Повертаємо 201 Created.
                // Припускаємо, що GetUsername(int userId) - це ендпоінт для отримання користувача за ID.
                // Якщо такого ендпоінта немає або його назва інша, змініть nameof()
                try
                {
                    return CreatedAtAction(nameof(GetUsername), new { userId = addedUser.UserId }, addedUser);
                }
                catch (Exception ex)
                {
                    // Якщо GetUsername ендпоінт не знайдено, повертаємо 200 OK
                    _logger.LogError(ex, "Failed to create CreatedAtAction URL for user {UserId}. Returning Ok.", addedUser.UserId);
                    return Ok(addedUser);
                }
            }
            // Обробка InvalidOperationException з сервісу (наприклад, ім'я користувача вже існує)
            catch (InvalidOperationException ioex)
            {
                _logger.LogWarning(ioex, "AddUser failed due to business logic: {Message}", ioex.Message);
                return Conflict(ioex.Message); // 409 Conflict для конфлікту ресурсів (існуюче ім'я)
                // Або BadRequest(), якщо ви вважаєте це помилкою запиту
            }
            catch (ArgumentException argEx)
            {
                // Обробка ArgumentException з сервісу (наприклад, порожні логін/пароль)
                _logger.LogWarning(argEx, "AddUser failed due to invalid arguments: {Message}", argEx.Message);
                return BadRequest(argEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user '{Username}'.", userRequest.UserName);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while adding the user.");
            }
        }

        // GET /api/me/username (Отримати ім'я поточного користувача з токена)
        [HttpGet("me/username")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Якщо немає токена або недійсний
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Якщо користувача знайдено за токеном, але немає в БД (рідкісний випадок)
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        // Перейменовано метод, щоб уникнути конфлікту імен з GetUsername(int userId)
        public async Task<ActionResult<string>> GetMyUsername()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                // Цей випадок обробляється зазвичай middleware для [Authorize],
                // але явна перевірка додає надійності.
                _logger.LogWarning("GetMyUsername: Unauthorized access attempt - no valid user ID in token.");
                return Unauthorized();
            }

            _logger.LogInformation("User {UserId} requesting their own username.", userId.Value);

            try
            {
                var username = await _mainService.GetUsernameAsync(userId.Value);
                if (username == null)
                {
                    // Цей випадок означає, що токен валідний і містить UserID,
                    // але користувача з таким ID немає в базі даних. Це може вказувати на проблему.
                    _logger.LogError("GetMyUsername: Authenticated user ID {UserId} not found in database.", userId.Value);
                    return NotFound("User data not found.");
                }
                _logger.LogInformation("Returning username '{Username}' for user ID {UserId}.", username, userId.Value);
                return Ok(username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting own username for user ID {UserId}.", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // GET /api/me/email (Отримати email поточного користувача з токена)
        [HttpGet("me/email")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<string>> GetMyEmail() // Перейменовано для ясності
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("GetMyEmail: Unauthorized access attempt - no valid user ID in token.");
                return Unauthorized();
            }


            _logger.LogInformation("User {UserId} requesting their own email.", userId.Value);

            try
            {
                var email = await _mainService.GetEmailAsync(userId.Value);
                if (email == null)
                {
                    _logger.LogError("GetMyEmail: Authenticated user ID {UserId} not found in database for email.", userId.Value);
                    return NotFound("User data not found.");
                }
                _logger.LogInformation("Returning email '{Email}' for user ID {UserId}.", email, userId.Value);
                return Ok(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting own email for user ID {UserId}.", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // DELETE /api/me (Видалити обліковий запис поточного користувача)
        [HttpDelete("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Успішне видалення
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Якщо користувача знайдено за токеном, але немає в БД
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMyAccount() // Перейменовано для ясності
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("DeleteMyAccount: Unauthorized access attempt - no valid user ID in token.");
                return Unauthorized();
            }


            _logger.LogInformation("User {UserId} attempting to delete their own account.", userId.Value);

            try
            {
                await _mainService.DeleteUserAsync(userId.Value);
                _logger.LogInformation("User {UserId} deleted successfully.", userId.Value);
                return NoContent(); // 204 NoContent - успішно, але немає тіла відповіді
            }
            // Обробка NotFoundException з сервісу, якщо користувача не знайдено
            catch (NotFoundException nfex)
            {
                _logger.LogWarning(nfex, "DeleteMyAccount failed: User with ID {UserId} not found in database.", userId.Value);
                // Хоча токен був валідний, користувача немає. Можливо, це 404, або 401, якщо вважати сесію невалідною. 404 більш конкретно про ресурс.
                return NotFound(nfex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting own account for user ID {UserId}.", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deleting the account.");
            }
        }


        // --- Ендпоінти для Score ---

        // PUT /api/me/score (Оновити рахунок поточного користувача)
        // Тепер цей ендпоінт повертає результат операції (EditScoreResult)
        [HttpPut("me/score")]
        [Authorize]
        // Оновлені ProducesResponseType: повертаємо EditScoreResult з 200 OK
        [ProducesResponseType(typeof(EditScoreResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Наприклад, від'ємний рахунок або ArgumentException з сервісу
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Забезпечується [Authorize]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Якщо користувача знайдено за токеном, але немає в БД
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        // Змінюємо тип повернення на ActionResult<EditScoreResult>
        public async Task<ActionResult<EditScoreResult>> EditScore([FromBody] ScoreChangeRequest scoreChangeRequest)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("EditScore: Unauthorized access attempt - no valid user ID in token.");
                return Unauthorized();
            }

            _logger.LogInformation("User {UserId} attempting to edit their score with value: {AttemptedScore}.", userId.Value, scoreChangeRequest?.Score);

            // Додано перевірку RequestModel на null та валідацію ModelState
            if (scoreChangeRequest == null || !ModelState.IsValid)
            {
                _logger.LogWarning("EditScore: Invalid score change request received.");
                return BadRequest(ModelState);
            }

            // Перевірка на від'ємний рахунок - може залишитись тут або тільки в сервісі
            if (scoreChangeRequest.Score < 0)
            {
                _logger.LogWarning("EditScore: User {UserId} sent a negative score value {AttemptedScore}.", userId.Value, scoreChangeRequest.Score);
                return BadRequest("Score cannot be negative.");
            }


            try
            {
                // Викликаємо оновлений метод сервісу, який повертає EditScoreResult
                var result = await _mainService.EditScoreAsync(userId.Value, scoreChangeRequest.Score);

                // Логуємо результат, отриманий від сервісу
                _logger.LogInformation(
                    "EditScore result for user {UserId}: UpdatedOrCreated={UpdatedOrCreated}, FinalScore={FinalScore}, Message='{Message}'",
                    userId.Value, result.UpdatedOrCreated, result.FinalScore, result.StatusMessage);


                // Повертаємо статус 200 OK разом з об'єктом результату.
                // Об'єкт EditScoreResult пояснить клієнту, чи було оновлення.
                return Ok(result);

            }
            // Обробка ArgumentException з сервісу (наприклад, якщо score < 0, хоча ми перевіряємо вище)
            catch (ArgumentException argEx) when (argEx.ParamName == "score")
            {
                _logger.LogWarning(argEx, "EditScore failed due to invalid score value from service: {Message}", argEx.Message);
                return BadRequest(argEx.Message);
            }
            // Обробка NotFoundException, якщо користувача не знайдено в сервісі (малоймовірно при Authorize)
            catch (NotFoundException nfex)
            {
                _logger.LogWarning(nfex, "EditScore failed: User with ID {UserId} not found in database according to service.", userId.Value);
                return NotFound(nfex.Message); // 404, якщо сервіс каже, що користувача немає
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing score for user ID {UserId}.", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while editing the score.");
            }
        }

        // GET /api/me/score (Отримати рахунок поточного користувача)
        [HttpGet("me/score")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Якщо користувача або його рахунку немає
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<int>> GetMyScore() // Перейменовано для ясності
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("GetMyScore: Unauthorized access attempt - no valid user ID in token.");
                return Unauthorized();
            }


            _logger.LogInformation("User {UserId} requesting their own score.", userId.Value);

            try
            {
                // Викликаємо сервіс з ID з токена
                var score = await _mainService.GetScoreAsync(userId.Value);
                if (score == null)
                {
                    // Якщо GetScoreAsync повернув null (запис Score не знайдено для цього UserID)
                    _logger.LogInformation("Score data not found for user ID {UserId}.", userId.Value);
                    return NotFound("Score data not found.");
                }
                _logger.LogInformation("Returning score {ScoreValue} for user ID {UserId}.", score.Value, userId.Value);
                return Ok(score.Value); // Повертаємо int
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting own score for user ID {UserId}.", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the score.");
            }
        }
        // GET /api/leaderboard (Отримати топ-10 рахунків)
        [HttpGet("leaderboard")]
        [AllowAnonymous] // Дозволяємо всім дивитись таблицю лідерів
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Score[]))] // Повертаємо масив об'єктів Score
        // ProducesResponseType 404 не потрібен, бо сервіс поверне порожній масив, а не null/NotFoundException
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Score[]>> GetLeaderboard()
        {
            _logger.LogInformation("Request received for leaderboard.");

            try
            {
                // Сервіс GetLeaderBoard тепер повертає Score[], а не null
                var leaderboard = await _mainService.GetLeaderBoard();

                // Перевірка на null більше не потрібна тут, бо ToArrayAsync повертає порожній масив, якщо немає даних.
                // if (leaderboard == null) // Це було б некоректно з ToArrayAsync()
                // {
                //    _logger.LogInformation($"No data for leaderboard");
                //    return NotFound("Leaderboard data not found."); // Можна повернути Ok з порожнім масивом замість NotFound
                // }

                _logger.LogInformation("Returning {Count} leaderboard entries.", leaderboard.Length);
                return Ok(leaderboard); // Повертаємо 200 OK з масивом (можливо, порожнім)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception Caught while fetching leaderboard.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the leaderboard.");
            }
        }


        // --- Захищений ендпоінт для отримання даних ІНШОГО користувача ---

        // GET /api/users/{userId:int}/username (Отримати ім'я будь-якого користувача за ID)
        // Використовується в CreatedAtAction
        [HttpGet("users/{userId:int}/username")] // Маршрут з параметром ID
        [Authorize] // Вимагає лише автентифікації, щоб дивитись імена інших
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        // Залишаємо оригінальне ім'я методу GetUsername, оскільки воно використовується в CreatedAtAction
        public async Task<ActionResult<string>> GetUsername(int userId) // Приймає userId з URL
        {
            var currentUserId = GetCurrentUserId(); // Отримуємо ID того, хто запитує, для логування
            _logger.LogInformation("User {CurrentUserId} requesting username for user ID {TargetUserId}", currentUserId?.ToString() ?? "Unknown", userId);

            try
            {
                // Викликаємо сервіс з ID з URL
                var username = await _mainService.GetUsernameAsync(userId);
                if (username == null)
                {
                    _logger.LogInformation("Username not found for target user ID {TargetUserId}.", userId);
                    return NotFound($"User with ID {userId} not found.");
                }
                _logger.LogInformation("Returning username '{Username}' for target user ID {TargetUserId}.", username, userId);
                return Ok(username);
            }
            // Немає специфічних винятків для обробки від GetUsernameAsync (він повертає null на 404)
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting username for user ID {UserId}.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // Можна видалити ваш тестовий ендпоінт або залишити для швидкої перевірки
        [HttpGet("/api/test")]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)] // Не показувати в Swagger UI
        public ActionResult GetTest()
        {
            _logger.LogInformation("Test endpoint called.");
            return Ok("API is running ok!");
        }
    }
}