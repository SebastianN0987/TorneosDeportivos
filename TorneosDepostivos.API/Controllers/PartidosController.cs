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
    public class PartidosController : ControllerBase
    {
        private readonly TorneosDeportivosDbContext _context;

        public PartidosController(TorneosDeportivosDbContext context)
        {
            _context = context;
        }

        // --- GET: api/Partidos ---
        [HttpGet]
        public async Task<ActionResult<ApiResult<IEnumerable<Partido>>>> GetPartido()
        {
            var partidos = await _context.Partidos.ToListAsync();

            // Envuelve la respuesta en ApiResult
            return Ok(new ApiResult<IEnumerable<Partido>>
            {
                Success = true,
                Data = partidos
            });
        }

        // --- GET: api/Partidos/5 ---
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResult<Partido>>> GetPartido(int id)
        {
            var partido = await _context.Partidos.FindAsync(id);

            if (partido == null)
            {
                return NotFound(new ApiResult<Partido> { Success = false, Message = "Partido no encontrado." });
            }

            // Envuelve la respuesta en ApiResult
            return Ok(new ApiResult<Partido> { Success = true, Data = partido });
        }

        // --- PUT: api/Partidos/5 ---
        // Método utilizado para actualizar el resultado de un partido
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPartido(int id, Partido partido)
        {
            if (id != partido.Id)
            {
                return BadRequest();
            }

            // Aquí se asume que la lógica de negocio es actualizar el partido
            _context.Entry(partido).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PartidoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Retorna un 204 No Content para una actualización exitosa
            return NoContent();
        }

        // --- POST: api/Partidos ---
        // Método para crear un nuevo partido
        [HttpPost]
        public async Task<ActionResult<ApiResult<Partido>>> PostPartido(Partido partido)
        {
            // Validaciones si son necesarias...

            _context.Partidos.Add(partido);
            await _context.SaveChangesAsync();

            // 1. Envuelve el objeto en ApiResult
            var result = new ApiResult<Partido> { Success = true, Data = partido, Message = "Partido creado." };

            // 2. Retorna 201 CreatedAtAction, usando el nombre correcto 'GetPartido'
            return CreatedAtAction(nameof(GetPartido), new { id = partido.Id }, result);
        }

        // --- DELETE: api/Partidos/5 ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePartido(int id)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null)
            {
                return NotFound();
            }

            _context.Partidos.Remove(partido);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PartidoExists(int id)
        {
            return _context.Partidos.Any(e => e.Id == id);
        }
    }
}