using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Greenhouse.API.Models;

public class LeituraAmbiental
{
    public int Id { get; set; }

    [Required]
    public int EstufaId { get; set; }

    [Range(-50, 100, ErrorMessage = "Temperatura deve ser entre -50°C e 100°C")]
    public double Temperatura { get; set; }

    [Range(0, 100, ErrorMessage = "Umidade deve ser entre 0% e 100%")]
    public double Umidade { get; set; }

    [Range(0, 200000, ErrorMessage = "Luminosidade deve ser entre 0 e 200.000 lux")]
    public double Luminosidade { get; set; }

    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EstufaId))]
    public Estufa? Estufa { get; set; }
}
