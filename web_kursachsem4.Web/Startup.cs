using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using web_kursachsem4.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration; 
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using web_kursachsem4.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens; 
using System.Text;
using System.Text.Json.Serialization;

namespace web_kursachsem4.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options => 
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins(
                                   "https://localhost:7167", 
                                   "https://localhost:8080",
                                   "https://localhost:80",
                                   "https://localhost:5222",
                                   "https://soniklvl.win", 
                                   "http://localhost:5173"
                               )
                               .AllowAnyMethod()
                               .AllowAnyHeader(); 
                    });
            });

            services.AddControllers();
            services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
            services.AddDbContext<mainDBcontext>(opts =>
            {
                opts.EnableDetailedErrors();
                opts.UseNpgsql(Configuration.GetConnectionString("web_kursachsem4.dev"));
            });

            services.AddTransient<IMainService, MainService>();
            

            // --- НАЛАШТУВАННЯ JWT АВТЕНТИФІКАЦІЇ ---
            var jwtSettings = Configuration.GetSection("JwtSettings");
            var secretKeyString = jwtSettings["SecretKey"];
            if (string.IsNullOrEmpty(secretKeyString))
            {
                throw new ArgumentNullException("JwtSettings:SecretKey", "JWT SecretKey must be configured in appsettings.");
            }
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKeyString));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; 
            })
            .AddJwtBearer(options =>
            {
                // options.RequireHttpsMetadata = false; // false для dev, true для production (якщо немає SSL-термінації перед API)
                options.SaveToken = true; // Зберігати токен в HttpContext.Authentication.AuthenticateResult.Properties["access_token"]
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, 
                    ValidateAudience = true, 
                    ValidateLifetime = true, 
                    ValidateIssuerSigningKey = true, 

                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = secretKey,
                    ClockSkew = TimeSpan.Zero // Забрати стандартне відхилення часу (5 хв), якщо потрібно точну перевірку expire
                };
            });

            services.AddAuthorization();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection(); 

            app.UseRouting(); 

            // Застосування політики CORS (має бути ПІСЛЯ UseRouting і ПЕРЕД UseAuthentication/UseAuthorization)
            app.UseCors(); 

            // ВАЖЛИВИЙ ПОРЯДОК: Спочатку Authentication, потім Authorization
            app.UseAuthentication(); // Перевіряє наявність та валідність токена
            app.UseAuthorization(); // Перевіряє, чи має автентифікований користувач доступ ([Authorize] атрибути)


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers(); 
            });
        }
    }
}