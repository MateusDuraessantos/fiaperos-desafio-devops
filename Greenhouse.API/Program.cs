using System.Text;
using Greenhouse.API.Data;
using Greenhouse.API.Middleware;
using Greenhouse.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var deploymentEnvironment = builder.Configuration["Deployment:Environment"]
    ?? builder.Environment.EnvironmentName;
var (environmentLabel, environmentColor) = deploymentEnvironment.ToLowerInvariant() switch
{
    "local" or "development" => ("LOCAL", "#1d4ed8"),
    "staging" => ("STAGING", "#92400e"),
    "production" => ("PRODUÇÃO", "#b91c1c"),
    _ => ("AMBIENTE NÃO IDENTIFICADO", "#374151")
};

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = $"Greenhouse API — {environmentLabel}",
        Version = "v1",
        Description = $"**Ambiente: {environmentLabel}**\n\nPlataforma ESG para monitoramento e gestão sustentável de estufas inteligentes"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT no formato: Bearer {seu_token}"
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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        logger.LogInformation("Migrations aplicadas com sucesso.");
    }
    catch (Exception ex)
    {
        logger.LogWarning("Não foi possível aplicar migrations: {Message}. A API continuará sem banco.", ex.Message);
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.DocumentTitle = $"Greenhouse API — {environmentLabel}";
    c.SwaggerEndpoint("./swagger/v1/swagger.json", $"Greenhouse API v1 — {environmentLabel}");
    c.InjectStylesheet("./environment.css");
    c.RoutePrefix = string.Empty;
});

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/swagger", (HttpContext context) => Results.Redirect($"{context.Request.PathBase}/"))
    .ExcludeFromDescription();
app.MapGet("/swagger/index.html", (HttpContext context) => Results.Redirect($"{context.Request.PathBase}/"))
    .ExcludeFromDescription();

app.MapGet("/environment.css", () => Results.Text($$"""
    .swagger-ui .topbar {
        border-bottom: 6px solid {{environmentColor}};
    }
    .swagger-ui .topbar::after {
        content: "AMBIENTE: {{environmentLabel}}";
        display: block;
        width: fit-content;
        margin: 10px auto;
        padding: 8px 20px;
        border-radius: 6px;
        background: {{environmentColor}};
        color: #fff;
        font: bold 16px sans-serif;
        letter-spacing: 1px;
    }
    """, "text/css")).ExcludeFromDescription();

app.Run();

public partial class Program { }
