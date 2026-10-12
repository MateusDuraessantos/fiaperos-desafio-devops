using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Greenhouse.API.Models;

public class Alerta
{
    public int Id { get; set; }

    [Required]
    public int EstufaId { get; set; }

    [Required(ErrorMessage = "Mensagem é obrigatória")]
    [StringLength(500)]
    public string Mensagem { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Tipo { get; set; } = string.Empty;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public bool Resolvido { get; set; } = false;

    [ForeignKey(nameof(EstufaId))]
    public Estufa? Estufa { get; set; }
}
