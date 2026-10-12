using System.ComponentModel.DataAnnotations;

namespace Greenhouse.API.Models;

public class Estufa
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Localização é obrigatória")]
    [StringLength(200)]
    public string Localizacao { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Área deve ser entre 1 e 100.000 m²")]
    public double Area { get; set; }

    [Required]
    [StringLength(100)]
    public string TipoCultivo { get; set; } = string.Empty;

    public bool Ativa { get; set; } = true;

    public ICollection<Sensor> Sensores { get; set; } = [];
    public ICollection<Alerta> Alertas { get; set; } = [];
    public ICollection<LeituraAmbiental> LeiturasAmbientais { get; set; } = [];
}
