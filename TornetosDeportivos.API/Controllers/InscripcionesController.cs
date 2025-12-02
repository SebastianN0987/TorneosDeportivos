using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorneoDeportivo.Modelos;
using TorneoDeportivo.Modelos.Dtos;

namespace TornetosDeportivos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InscripcionesController:ControllerBase
{
    private readonly AppDbContext _db;

    public InscripcionesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] InscripcionCreateDto dto)
    {
        // Validaciones básicas
        var torneoExiste = await _db.Torneos.AnyAsync(t => t.Id == dto.TorneoId);
        if(!torneoExiste) return NotFound($"Torneo {dto.TorneoId} no existe.");

        var equipoExiste = await _db.Equipos.AnyAsync(e => e.Id == dto.EquipoId);
        if(!equipoExiste) return NotFound($"Equipo {dto.EquipoId} no existe.");

        var yaInscrito = await _db.Inscripciones
            .AnyAsync(i => i.TorneoId == dto.TorneoId && i.EquipoId == dto.EquipoId);
        if(yaInscrito) return Conflict("El equipo ya está inscrito en este torneo.");

        var inscripcion = new Inscripcion
        {
            TorneoId = dto.TorneoId,
            EquipoId = dto.EquipoId,
            FechaInscripcion = DateTime.UtcNow
        };

        _db.Inscripciones.Add(inscripcion);
        await _db.SaveChangesAsync();

        // Opcional: devolver un envoltorio ApiResult si lo usas en toda la API
        return CreatedAtAction(nameof(ObtenerPorId), new { id = inscripcion.Id }, inscripcion);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var ins = await _db.Inscripciones
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
        return ins is null ? NotFound() : Ok(ins);
    }
}