using System.ComponentModel.DataAnnotations;
using Greenhouse.API.ViewModels;
using Xunit;

namespace Greenhouse.API.Tests;

public class EstufaValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(100001)]
    public void AreaOutsideAllowedRangeIsRejected(double area)
    {
        var model = new EstufaCreateViewModel
        {
            Nome = "Estufa", Localizacao = "São Paulo", TipoCultivo = "Tomate", Area = area
        };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(model.Area)));
    }

    [Fact]
    public void EstufaWithoutNameIsRejected()
    {
        var model = new EstufaCreateViewModel
        {
            Nome = "", Localizacao = "São Paulo", TipoCultivo = "Tomate", Area = 100
        };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(model.Nome)));
    }
}
