using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Greenhouse.API.Models;

public class Sensor
{
    public int Id { get; set; }

    [Required]
    public int EstufaId { get; set; }

    [Required(ErrorMessage = "Tipo é obrigatório")]
    [StringLength(50)]
    public string Tipo { get; set; } = string.Empty;

    public double ValorAtual { get; set; }

    [StringLength(20)]
    public string Unidade { get; set; } = string.Empty;

    public DateTime DataLeitura { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EstufaId))]
    public Estufa? Estufa { get; set; }
}
