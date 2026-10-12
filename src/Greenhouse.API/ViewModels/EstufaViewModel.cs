using System.ComponentModel.DataAnnotations;

namespace Greenhouse.API.ViewModels;

public class EstufaCreateViewModel
{
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
}

public class EstufaResponseViewModel
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Localizacao { get; set; } = string.Empty;
    public double Area { get; set; }
    public string TipoCultivo { get; set; } = string.Empty;
    public bool Ativa { get; set; }
}
