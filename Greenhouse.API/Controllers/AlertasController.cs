using Greenhouse.API.Data;
using Greenhouse.API.Models;
using Greenhouse.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.API.Controllers;

[ApiController]
[Route("api/alertas")]
[Authorize]
public class AlertasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var total = await db.Alertas.CountAsync();
        var items = await db.Alertas
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AlertaResponseViewModel
            {
                Id = a.Id,
                EstufaId = a.EstufaId,
                Mensagem = a.Mensagem,
                Tipo = a.Tipo,
                DataCriacao = a.DataCriacao,
                Resolvido = a.Resolvido
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var alerta = await db.Alertas.FindAsync(id);
        if (alerta is null) return NotFound(new { message = "Alerta não encontrado" });

        return Ok(new AlertaResponseViewModel
        {
            Id = alerta.Id,
            EstufaId = alerta.EstufaId,
            Mensagem = alerta.Mensagem,
            Tipo = alerta.Tipo,
            DataCriacao = alerta.DataCriacao,
            Resolvido = alerta.Resolvido
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AlertaCreateViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var estufaExiste = await db.Estufas.AnyAsync(e => e.Id == model.EstufaId);
        if (!estufaExiste) return BadRequest(new { message = "Estufa não encontrada" });

        var alerta = new Alerta
        {
            EstufaId = model.EstufaId,
            Mensagem = model.Mensagem,
            Tipo = model.Tipo,
            DataCriacao = DateTime.UtcNow,
            Resolvido = false
        };

        db.Alertas.Add(alerta);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = alerta.Id },
            new AlertaResponseViewModel { Id = alerta.Id, EstufaId = alerta.EstufaId, Mensagem = alerta.Mensagem, Tipo = alerta.Tipo, DataCriacao = alerta.DataCriacao, Resolvido = alerta.Resolvido });
    }
}
