using System.ComponentModel.DataAnnotations;

namespace Greenhouse.API.ViewModels;

public class AlertaCreateViewModel
{
    [Required]
    public int EstufaId { get; set; }

    [Required(ErrorMessage = "Mensagem é obrigatória")]
    [StringLength(500)]
    public string Mensagem { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Tipo { get; set; } = string.Empty;
}

public class AlertaResponseViewModel
{
    public int Id { get; set; }
    public int EstufaId { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public bool Resolvido { get; set; }
}
