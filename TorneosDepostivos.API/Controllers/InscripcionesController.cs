using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorneoDeportivo.Modelos; // Asegúrate de que este using sea correcto

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
        // Se cambia el tipo de retorno a ApiResult
        public async Task<ActionResult<ApiResult<Inscripcion>>> PostInscripcion(Inscripcion inscripcion)
        {
            // Lógica de validación de negocio (opcional, pero recomendable)
            var torneo = await _context.Torneos.FindAsync(inscripcion.TorneoId);
            if (torneo == null)
            {
                // Ejemplo de validación: si el torneo no existe.
                return BadRequest(new ApiResult<Inscripcion> { Success = false, Message = "El ID del torneo no es válido." });
            }

            _context.Inscripciones.Add(inscripcion);
            // La inscripción no requiere que el ID se genere automáticamente.
            await _context.SaveChangesAsync();

            // 1. **CORRECCIÓN CLAVE:** Crear y retornar el ApiResult.
            // Esto asegura que tu código de prueba reciba un código 201 y un cuerpo JSON válido.
            var result = new ApiResult<Inscripcion>
            {
                Success = true,
                Data = inscripcion,
                Message = "Inscripción registrada correctamente."
            };

            return CreatedAtAction(nameof(GetInscripcion), new { id = inscripcion.Id }, result); // Retorna 201 Created
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