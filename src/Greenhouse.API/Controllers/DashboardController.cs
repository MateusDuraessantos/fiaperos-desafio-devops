using Greenhouse.API.Data;
using Greenhouse.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var estufasAtivas = await db.Estufas.CountAsync(e => e.Ativa);
        var sensoresOnline = await db.Sensores.CountAsync();
        var alertasPendentes = await db.Alertas.CountAsync(a => !a.Resolvido);

        var mediaTemperatura = await db.LeiturasAmbientais.AnyAsync()
            ? await db.LeiturasAmbientais.AverageAsync(l => l.Temperatura)
            : 0.0;

        return Ok(new DashboardViewModel
        {
            EstufasAtivas = estufasAtivas,
            SensoresOnline = sensoresOnline,
            AlertasPendentes = alertasPendentes,
            MediaTemperatura = Math.Round(mediaTemperatura, 1)
        });
    }
}
