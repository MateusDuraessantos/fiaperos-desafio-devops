using Greenhouse.API.Data;
using Greenhouse.API.Models;
using Greenhouse.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.API.Controllers;

[ApiController]
[Route("api/estufas")]
[Authorize]
public class EstufasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var total = await db.Estufas.CountAsync();
        var items = await db.Estufas
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EstufaResponseViewModel
            {
                Id = e.Id,
                Nome = e.Nome,
                Localizacao = e.Localizacao,
                Area = e.Area,
                TipoCultivo = e.TipoCultivo,
                Ativa = e.Ativa
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var estufa = await db.Estufas.FindAsync(id);
        if (estufa is null) return NotFound(new { message = "Estufa não encontrada" });

        return Ok(new EstufaResponseViewModel
        {
            Id = estufa.Id,
            Nome = estufa.Nome,
            Localizacao = estufa.Localizacao,
            Area = estufa.Area,
            TipoCultivo = estufa.TipoCultivo,
            Ativa = estufa.Ativa
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EstufaCreateViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var estufa = new Estufa
        {
            Nome = model.Nome,
            Localizacao = model.Localizacao,
            Area = model.Area,
            TipoCultivo = model.TipoCultivo,
            Ativa = model.Ativa
        };

        db.Estufas.Add(estufa);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = estufa.Id },
            new EstufaResponseViewModel { Id = estufa.Id, Nome = estufa.Nome, Localizacao = estufa.Localizacao, Area = estufa.Area, TipoCultivo = estufa.TipoCultivo, Ativa = estufa.Ativa });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EstufaCreateViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var estufa = await db.Estufas.FindAsync(id);
        if (estufa is null) return NotFound(new { message = "Estufa não encontrada" });

        estufa.Nome = model.Nome;
        estufa.Localizacao = model.Localizacao;
        estufa.Area = model.Area;
        estufa.TipoCultivo = model.TipoCultivo;
        estufa.Ativa = model.Ativa;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var estufa = await db.Estufas.FindAsync(id);
        if (estufa is null) return NotFound(new { message = "Estufa não encontrada" });

        db.Estufas.Remove(estufa);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
