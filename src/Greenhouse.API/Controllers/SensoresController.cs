using Greenhouse.API.Data;
using Greenhouse.API.Models;
using Greenhouse.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.API.Controllers;

[ApiController]
[Route("api/sensores")]
[Authorize]
public class SensoresController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? tipo = null)
    {
        var query = db.Sensores.AsQueryable();

        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(s => s.Tipo.ToLower() == tipo.ToLower());

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SensorResponseViewModel
            {
                Id = s.Id,
                EstufaId = s.EstufaId,
                Tipo = s.Tipo,
                ValorAtual = s.ValorAtual,
                Unidade = s.Unidade,
                DataLeitura = s.DataLeitura
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sensor = await db.Sensores.FindAsync(id);
        if (sensor is null) return NotFound(new { message = "Sensor não encontrado" });

        return Ok(new SensorResponseViewModel
        {
            Id = sensor.Id,
            EstufaId = sensor.EstufaId,
            Tipo = sensor.Tipo,
            ValorAtual = sensor.ValorAtual,
            Unidade = sensor.Unidade,
            DataLeitura = sensor.DataLeitura
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SensorCreateViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var estufaExiste = await db.Estufas.AnyAsync(e => e.Id == model.EstufaId);
        if (!estufaExiste) return BadRequest(new { message = "Estufa não encontrada" });

        var sensor = new Sensor
        {
            EstufaId = model.EstufaId,
            Tipo = model.Tipo,
            ValorAtual = model.ValorAtual,
            Unidade = model.Unidade,
            DataLeitura = DateTime.UtcNow
        };

        db.Sensores.Add(sensor);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = sensor.Id },
            new SensorResponseViewModel { Id = sensor.Id, EstufaId = sensor.EstufaId, Tipo = sensor.Tipo, ValorAtual = sensor.ValorAtual, Unidade = sensor.Unidade, DataLeitura = sensor.DataLeitura });
    }
}
