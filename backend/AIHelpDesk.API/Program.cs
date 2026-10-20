using System.Text;
using System.Threading.Channels;
using AIHelpDesk.API.Middleware;
using AIHelpDesk.Application.Categories.Services;
using AIHelpDesk.Application.Comments.Services;
using AIHelpDesk.Application.Common.Interfaces;
using AIHelpDesk.Application.Dashboard.Services;
using AIHelpDesk.Application.Priorities.Services;
using AIHelpDesk.Application.Tickets.Services;
using AIHelpDesk.Infrastructure.AI;
using AIHelpDesk.Infrastructure.BackgroundWorkers;
using AIHelpDesk.Infrastructure.Cache;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.MongoDB;
using AIHelpDesk.Infrastructure.Persistence;
using AIHelpDesk.Infrastructure.Repositories;
using AIHelpDesk.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;
using Serilog.Events;
using AIHelpDesk.API.Services;
using AIHelpDesk.Application.AgentApplications.Services;
using AIHelpDesk.Application.AgentAssignment.Services;
using AIHelpDesk.Application.AgentProfile.Services;

// ── Bootstrap Serilog early for startup logging ────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/aiHelpDesk.log", rollingInterval: RollingInterval.Day));

    // ── Validate required secrets at startup ──────────────────────────────────
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("JWT key is not configured (Jwt:Key).");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("JWT issuer is not configured (Jwt:Issuer).");
    var jwtAudience = builder.Configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("JWT audience is not configured (Jwt:Audience).");

    // ── SQL Server / EF Core ──────────────────────────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ── ASP.NET Identity ──────────────────────────────────────────────────────
    builder.Services
        .AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

    // ── JWT Authentication ────────────────────────────────────────────────────
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userManager = context.HttpContext.RequestServices
                        .GetRequiredService<UserManager<ApplicationUser>>();
                    var userId = context.Principal?.FindFirst(
                        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrEmpty(userId)) { context.Fail("Invalid token."); return; }

                    var user = await userManager.FindByIdAsync(userId);
                    if (user is null || !user.IsActive)
                        context.Fail("Account is inactive or no longer exists.");
                }
            };
        });

    builder.Services.AddAuthorization();

    // ── MongoDB ───────────────────────────────────────────────────────────────
    var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]
        ?? throw new InvalidOperationException("MongoDB connection string is not configured (MongoDB:ConnectionString).");
    var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "AIHelpDesk";

    builder.Services.AddSingleton<IMongoClient>(_ =>
        new MongoClient(mongoConnectionString));

    builder.Services.AddSingleton<MongoDbContext>(sp =>
        new MongoDbContext(sp.GetRequiredService<IMongoClient>(), mongoDatabaseName));

    // ── Lookup Cache ──────────────────────────────────────────────────────────
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ILookupCache, LookupCache>();

    // ── AI Analysis Channel (Async background processing) ─────────────────────
    var aiChannel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
    {
        SingleReader = true  // Only AiAnalysisWorker reads from this channel
    });
    builder.Services.AddSingleton(aiChannel);

    // ── Gemini HTTP Client ────────────────────────────────────────────────────
    builder.Services.AddHttpClient<IAiAnalysisService, GeminiAiAnalysisService>();

    // ── Background Worker ─────────────────────────────────────────────────────
    builder.Services.AddHostedService<AiAnalysisWorker>();

    // ── Repository Registrations (MongoDB) ───────────────────────────────────
    builder.Services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();
    builder.Services.AddScoped<IAiAnalysisRepository, AiAnalysisRepository>();

    // ── Application Services ──────────────────────────────────────────────────
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IPriorityService, PriorityService>();
    builder.Services.AddScoped<ITicketService, TicketService>();
    builder.Services.AddScoped<ICommentService, CommentService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAgentApplicationService, AgentApplicationService>();
    builder.Services.AddScoped<IAgentAssignmentService, AgentAssignmentService>();
    builder.Services.AddScoped<IAgentProfileService, AgentProfileService>();

    // ── FluentValidation ──────────────────────────────────────────────────────
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<
        AIHelpDesk.Application.Tickets.Validators.CreateTicketRequestValidator>();

    // ── Controllers + Swagger ─────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "AI Help Desk API",
            Version = "v1",
            Description = "Internal support ticket management system with AI-powered classification."
        });

        // JWT bearer auth in Swagger UI
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token (without 'Bearer ' prefix)."
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

    // ── CORS ──────────────────────────────────────────────────────────────────
    var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]
        ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
        ?? ["http://localhost:5173"];  // Vite default

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontendPolicy", policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    });

    // ── Build App ─────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Database migration + seeding ──────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await IdentitySeeder.SeedRolesAsync(roleManager);
        await IdentitySeeder.SeedAdminAsync(userManager, app.Configuration);
    }

    // ── Middleware Pipeline ───────────────────────────────────────────────────
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Help Desk API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseHttpsRedirection();
    app.UseCors("FrontendPolicy");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
