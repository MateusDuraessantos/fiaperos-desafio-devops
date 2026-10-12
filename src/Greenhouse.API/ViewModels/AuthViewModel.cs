using System.ComponentModel.DataAnnotations;

namespace Greenhouse.API.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Usuário é obrigatório")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Password { get; set; } = string.Empty;
}

public class TokenResponseViewModel
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}
