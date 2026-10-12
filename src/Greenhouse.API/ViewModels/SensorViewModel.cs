using System.ComponentModel.DataAnnotations;

namespace Greenhouse.API.ViewModels;

public class SensorCreateViewModel
{
    [Required]
    public int EstufaId { get; set; }

    [Required(ErrorMessage = "Tipo é obrigatório")]
    [StringLength(50)]
    public string Tipo { get; set; } = string.Empty;

    public double ValorAtual { get; set; }

    [StringLength(20)]
    public string Unidade { get; set; } = string.Empty;
}

public class SensorResponseViewModel
{
    public int Id { get; set; }
    public int EstufaId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public double ValorAtual { get; set; }
    public string Unidade { get; set; } = string.Empty;
    public DateTime DataLeitura { get; set; }
}
