using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Greenhouse.API.Controllers;
using Greenhouse.API.Services;
using Greenhouse.API.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Greenhouse.API.Tests;

public class AuthenticationTests
{
    // Credenciais exclusivas dos testes, sem acesso aos ambientes publicados.
    private const string Secret = "TestOnly_Key_For_HS256_AtLeast32Bytes!";

    private static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AdminCredentials:Username"] = "test-admin",
            ["AdminCredentials:Password"] = "TestOnly@123!",
            ["JwtSettings:SecretKey"] = Secret,
            ["JwtSettings:Issuer"] = "Greenhouse.Tests",
            ["JwtSettings:Audience"] = "Greenhouse.Tests.Client",
            ["JwtSettings:ExpirationHours"] = "1"
        }).Build();

    [Fact]
    public void ValidLoginReturnsSignedTokenForAuthenticatedUser()
    {
        var configuration = Configuration();
        var controller = new AuthController(new TokenService(configuration), configuration);

        var result = controller.Login(new LoginViewModel
        {
            Username = "test-admin", Password = "TestOnly@123!"
        });

        var response = Assert.IsType<TokenResponseViewModel>(Assert.IsType<OkObjectResult>(result).Value);
        var principal = new JwtSecurityTokenHandler().ValidateToken(response.Token,
            ValidationParameters(Secret), out _);
        Assert.Equal("test-admin", principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.InRange(response.Expiration, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
    }

    [Theory]
    [InlineData("test-admin", "wrong-password")]
    [InlineData("unknown-user", "TestOnly@123!")]
    public void InvalidCredentialsAreRejected(string username, string password)
    {
        var configuration = Configuration();
        var controller = new AuthController(new TokenService(configuration), configuration);
        Assert.IsType<UnauthorizedObjectResult>(controller.Login(new LoginViewModel
        {
            Username = username, Password = password
        }));
    }

    [Fact]
    public void TokenCannotBeValidatedWithAnotherSigningKey()
    {
        var (token, _) = new TokenService(Configuration()).GenerateToken("test-admin");
        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token,
                ValidationParameters("DifferentTestOnlyKey_AtLeast32Bytes!"), out _));
    }

    private static TokenValidationParameters ValidationParameters(string secret) => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ValidateIssuer = true,
        ValidIssuer = "Greenhouse.Tests",
        ValidateAudience = true,
        ValidAudience = "Greenhouse.Tests.Client",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
}
