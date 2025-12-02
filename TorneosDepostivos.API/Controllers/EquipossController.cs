using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorneoDeportivo.Modelos;

namespace TorneosDeportivos.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EquipossController : ControllerBase
    {
        private readonly TorneosDeportivosDbContext _context;

        public EquipossController(TorneosDeportivosDbContext context)
        {
            _context = context;
        }

        // --- GET: api/Equiposs ---
        [HttpGet]
        // Se cambia el tipo de retorno para usar ApiResult
        public async Task<ActionResult<ApiResult<IEnumerable<Equipo>>>> GetEquipo()
        {
            var equipos = await _context.Equipos.ToListAsync();

            // Envuelve la respuesta en ApiResult
            return Ok(new ApiResult<IEnumerable<Equipo>>
            {
                Success = true,
                Data = equipos
            });
        }

        // --- GET: api/Equiposs/5 ---
        [HttpGet("{id}")]
        // Se cambia el tipo de retorno para usar ApiResult
        public async Task<ActionResult<ApiResult<Equipo>>> GetEquipo(int id)
        {
            var equipo = await _context.Equipos.FindAsync(id);

            if (equipo == null)
            {
                // Devuelve NotFound con un ApiResult indicando el error
                return NotFound(new ApiResult<Equipo> { Success = false, Message = "Equipo no encontrado." });
            }

            // Envuelve la respuesta en ApiResult
            return Ok(new ApiResult<Equipo> { Success = true, Data = equipo });
        }

        // --- PUT: api/Equiposs/5 ---
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEquipo(int id, Equipo equipo)
        {
            if (id != equipo.Id)
            {
                return BadRequest();
            }

            _context.Entry(equipo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EquipoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // --- POST: api/Equiposs ---
        [HttpPost]
        // Se cambia el tipo de retorno para usar ApiResult
        public async Task<ActionResult<ApiResult<Equipo>>> PostEquipo(Equipo equipo)
        {
            _context.Equipos.Add(equipo);
            // **IMPORTANTE**: SaveChangesAsync debe ocurrir aquí para que 'equipo.Id' se genere.
            await _context.SaveChangesAsync();

            // 1. **CORRECCIÓN CLAVE:** Creación del objeto ApiResult
            // Esto asegura que el JSON tenga la estructura que la prueba espera (Data, Success).
            var result = new ApiResult<Equipo>
            {
                Success = true,
                Data = equipo,
                Message = "Equipo creado exitosamente."
            };

            // 2. Retornar 201 CreatedAtAction, usando el objeto 'result'.
            return CreatedAtAction(nameof(GetEquipo), new { id = equipo.Id }, result);
        }

        // --- DELETE: api/Equiposs/5 ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipo(int id)
        {
            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null)
            {
                return NotFound();
            }

            _context.Equipos.Remove(equipo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EquipoExists(int id)
        {
            return _context.Equipos.Any(e => e.Id == id);
        }
    }
}