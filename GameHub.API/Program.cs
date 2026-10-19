using GameHub.API.Infrastructure;
using GameHub.Core.Interfaces;
using GameHub.Core.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog.Web;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}
// ---------- Logging (NLog) ----------
// הקוד ממשיך להשתמש ב-ILogger<T> הרגיל. NLog מתחבר מתחת.
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// ---------- Data + Service layers ----------
// AddDataLayer: DbContext, Repositories, UnitOfWork, JwtOptions (עם ValidateOnStart), SeedOptions,
//               PasswordHasher, TokenService, DatabaseInitializer.
// AddServiceLayer: AutoMapper, Services, Generators, CaptureRules, Clock.
// ConnectionStrings:Default, Jwt:Key ו-Seed:* מגיעים מ-User Secrets (או ממשתני סביבה בענן).
builder.Services.AddDataLayer(builder.Configuration);
builder.Services.AddServiceLayer();

// ---------- Authentication / Authorization (JWT) ----------
// ההגדרות נקראות מ-IOptions<JwtOptions>, שכבר עובר ולידציה באתחול (JwtOptionsValidator),
// ולכן אין כאן קריאה ישירה ל-Configuration ואין חשש ממפתח חסר.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // 401/403 כ-ErrorResponse אחיד (JSON) במקום גוף ריק.
        bearer.Events = JwtBearerErrorEvents.Create();
    });

builder.Services.AddAuthorization();

// ---------- Controllers + Swagger ----------
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // 400 של [ApiController] כ-ErrorResponse אחיד במקום ProblemDetails.
        options.InvalidModelStateResponseFactory = ValidationErrorResponseFactory.Create;
    });

builder.Services.AddSwaggerWithJwt();

// ---------- CORS (Development בלבד, עבור שרת הפיתוח של Vite) ----------
const string ViteCorsPolicy = "ViteDevClient";
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
    options.AddPolicy(ViteCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("X-Correlation-Id", "Location")));
var app = builder.Build();

// ---------- עלייה אוטומטית: Migrate + Seed ----------
// המערכת צריכה לעלות בלי התערבות ידנית בבסיס הנתונים.
// אם חסר מפתח Seed ב-User Secrets, האתחול נכשל כאן עם הודעה שמפרטת מה חסר.
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await initializer.InitializeAsync(app.Lifetime.ApplicationStopping);
}

// ---------- Pipeline (הסדר קריטי) ----------
// CorrelationId -> ExceptionHandling -> RequestLogging -> Swagger/CORS (Dev) -> Authentication -> Authorization -> Controllers
// CorrelationId ראשון, כדי שגם תשובת 500 תכלול את המזהה.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(ViteCorsPolicy); // לפני Authentication, כדי ש-preflight לא יידחה ב-401
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// נדרש כדי ש-WebApplicationFactory<Program> יוכל לשמש בבדיקות אינטגרציה בעתיד.
public partial class Program { }