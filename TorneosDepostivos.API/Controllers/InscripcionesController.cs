using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorneoDeportivo.Modelos; // Asegúrate de que este using sea correcto
using TorneoDeportivo.Modelos.Dtos;

namespace TorneosDeportivos.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InscripcionesController : ControllerBase
    {
        private readonly TorneosDeportivosDbContext _context;

        public InscripcionesController(TorneosDeportivosDbContext context)
        {
            _context = context;
        }

        // --- GET: api/Inscripciones ---
        [HttpGet]
        // Se cambia el tipo de retorno a ApiResult para consistencia
        public async Task<ActionResult<ApiResult<IEnumerable<Inscripcion>>>> GetInscripcion()
        {
            var inscripciones = await _context.Inscripciones.ToListAsync();

            return Ok(new ApiResult<IEnumerable<Inscripcion>>
            {
                Success = true,
                Data = inscripciones
            });
        }

        // --- GET: api/Inscripciones/5 ---
        [HttpGet("{id}")]
        // Se cambia el tipo de retorno a ApiResult para consistencia
        public async Task<ActionResult<ApiResult<Inscripcion>>> GetInscripcion(int id)
        {
            var inscripcion = await _context.Inscripciones.FindAsync(id);

            if (inscripcion == null)
            {
                // Retorno 404 con mensaje
                return NotFound(new ApiResult<Inscripcion> { Success = false, Message = "Inscripción no encontrada." });
            }

            return Ok(new ApiResult<Inscripcion> { Success = true, Data = inscripcion });
        }

        // --- PUT: api/Inscripciones/5 ---
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInscripcion(int id, Inscripcion inscripcion)
        {
            if (id != inscripcion.Id)
            {
                return BadRequest();
            }

            _context.Entry(inscripcion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InscripcionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // Retorna 204 No Content
        }

        // --- POST: api/Inscripciones ---
        [HttpPost]
        // Ahora recibimos un DTO para evitar validación de navegaciones
        public async Task<ActionResult<ApiResult<Inscripcion>>> PostInscripcion([FromBody] InscripcionCreateDto dto)
        {
            // Validaciones básicas
            var torneoExiste = await _context.Torneos.AnyAsync(t => t.Id == dto.TorneoId);
            if (!torneoExiste)
            {
                return NotFound(new ApiResult<Inscripcion> { Success = false, Message = $"Torneo {dto.TorneoId} no existe." });
            }

            var equipoExiste = await _context.Equipos.AnyAsync(e => e.Id == dto.EquipoId);
            if (!equipoExiste)
            {
                return NotFound(new ApiResult<Inscripcion> { Success = false, Message = $"Equipo {dto.EquipoId} no existe." });
            }

            var yaInscrito = await _context.Inscripciones.AnyAsync(i => i.TorneoId == dto.TorneoId && i.EquipoId == dto.EquipoId);
            if (yaInscrito)
            {
                return Conflict(new ApiResult<Inscripcion> { Success = false, Message = "El equipo ya está inscrito en este torneo." });
            }

            var inscripcion = new Inscripcion
            {
                TorneoId = dto.TorneoId,
                EquipoId = dto.EquipoId,
                FechaInscripcion = DateTime.UtcNow
            };

            _context.Inscripciones.Add(inscripcion);
            await _context.SaveChangesAsync();

            var result = new ApiResult<Inscripcion>
            {
                Success = true,
                Data = inscripcion,
                Message = "Inscripción registrada correctamente."
            };

            return CreatedAtAction(nameof(GetInscripcion), new { id = inscripcion.Id }, result);
        }

        // --- DELETE: api/Inscripciones/5 ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInscripcion(int id)
        {
            var inscripcion = await _context.Inscripciones.FindAsync(id);
            if (inscripcion == null)
            {
                return NotFound();
            }

            _context.Inscripciones.Remove(inscripcion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool InscripcionExists(int id)
        {
            return _context.Inscripciones.Any(e => e.Id == id);
        }
    }
}