using ChineseRaffleApi.Data;
using ChineseRaffleApi.Configuration;
using ChineseRaffleApi.Repository;
using ChineseRaffleApi.Repository.DI;
using ChineseRaffleApi.Services;
using ChineseRaffleApi.Services.DI;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// שימוש ב-MyContext עבור בסיס הנתונים
builder.Services.AddDbContext<MyContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDistributedMemoryCache();

var rateLimitSection = builder.Configuration.GetSection("RateLimiting");
var requestLimit = rateLimitSection.GetValue<int>("RequestLimit", 100);
var timeWindowMinutes = rateLimitSection.GetValue<int>("TimeWindowMinutes", 1);

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var clientIdentifier = context.Connection.RemoteIpAddress?.ToString()
                               ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                               ?? "unknown";

        return RateLimitPartition.GetSlidingWindowLimiter(clientIdentifier, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = requestLimit,
            Window = TimeSpan.FromMinutes(timeWindowMinutes),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.RejectionStatusCode = 429;
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = (timeWindowMinutes * 60).ToString();
        return ValueTask.CompletedTask;
    };
});

var redisHost = builder.Configuration["REDIS_HOST"] ?? builder.Configuration["Redis:Host"];
if (!string.IsNullOrWhiteSpace(redisHost))
{
    var redisPort = builder.Configuration["REDIS_PORT"] ?? builder.Configuration["Redis:Port"] ?? "6379";
    var redisPassword = builder.Configuration["REDIS_PASSWORD"] ?? builder.Configuration["Redis:Password"];
    var redisInstance = builder.Configuration["Redis:InstanceName"] ?? "ChineseRaffleApi:";
    var redisConfiguration = $"{redisHost}:{redisPort}";
    if (!string.IsNullOrWhiteSpace(redisPassword))
    {
        redisConfiguration += $",password={redisPassword},abortConnect=false";
    }

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConfiguration;
        options.InstanceName = redisInstance;
    });
}

var kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>()
    ?? throw new InvalidOperationException("Kafka configuration is missing");

builder.Services.AddSingleton(kafkaSettings);

var producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaSettings.BootstrapServers,
    ClientId = kafkaSettings.Producer.ClientId ?? builder.Environment.ApplicationName,
    Acks = Enum.TryParse<Acks>(kafkaSettings.Producer.Acks, true, out var acks) ? acks : Acks.All,
    EnableIdempotence = kafkaSettings.Producer.EnableIdempotence,
    MessageSendMaxRetries = kafkaSettings.Producer.MessageSendMaxRetries,
    RetryBackoffMs = kafkaSettings.Producer.RetryBackoffMs,
    LingerMs = kafkaSettings.Producer.LingerMs,
    CompressionType = Enum.TryParse<CompressionType>(kafkaSettings.Producer.CompressionType, true, out var compressionType)
        ? compressionType
        : CompressionType.None
};

if (kafkaSettings.Producer.SocketTimeoutMs.HasValue)
{
    producerConfig.SocketTimeoutMs = kafkaSettings.Producer.SocketTimeoutMs.Value;
}

builder.Services.AddSingleton(producerConfig);
builder.Services.AddSingleton<IProducer<Null, string>>(sp =>
    new ProducerBuilder<Null, string>(sp.GetRequiredService<ProducerConfig>()).Build());
builder.Services.AddSingleton<IKafkaEventPublisher, KafkaEventPublisher>();

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDonorRepo, DonorRepo>();
builder.Services.AddScoped<IDonorService, DonorService>();
builder.Services.AddScoped<IGiftRepo, GiftRepo>();
builder.Services.AddScoped<IGiftService, GiftService>();
builder.Services.AddScoped<ITicketRepo, TicketRepo>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IBasketRepo, BasketRepo>();
builder.Services.AddScoped<IBasketService, BasketService>();
builder.Services.AddScoped<IRaffleService, RaffleService>();
builder.Services.AddScoped<ICategoryRepo, CategoryRepo>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IRaffleStatisticsService, RaffleStatisticsService>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token) && context.Request.Cookies.ContainsKey("jwt_token"))
            {
                context.Token = context.Request.Cookies["jwt_token"];
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Log.Debug("JWT token validated for user {UserId}", userId);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new { message = "Unauthorized" });
            return context.Response.WriteAsync(result);
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new { message = "Forbidden – you do not have the required permissions" });
            return context.Response.WriteAsync(result);
        }
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:60607")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");
app.UseRateLimiter();
app.UseStaticFiles();
app.UseAuthentication(); 
app.UseAuthorization();  

app.MapControllers();

// ==========================================
// הקוד שמקים את ה-DB ומחיל מייגרציות אוטומטית בעלייה
// ==========================================
using (var scope = app.Services.CreateScope())
{
    // השתמשנו ב-MyContext שנמצא ב-using ChineseRaffleApi.Data;
    var dbContext = scope.ServiceProvider.GetRequiredService<MyContext>(); 
    dbContext.Database.Migrate(); 
}

app.Run();