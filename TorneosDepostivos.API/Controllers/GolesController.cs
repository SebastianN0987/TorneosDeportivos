using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TorneosDeportivos.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GolesController : ControllerBase
    {
        private readonly TorneosDeportivosDbContext _context;

        public GolesController(TorneosDeportivosDbContext context)
        {
            _context = context;
        }

        // GET: api/Goles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Gol>>> GetGol()
        {
            return await _context.Goles.ToListAsync();
        }

        // GET: api/Goles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Gol>> GetGol(int id)
        {
            var gol = await _context.Goles.FindAsync(id);

            if (gol == null)
            {
                return NotFound();
            }

            return gol;
        }

        // PUT: api/Goles/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGol(int id, Gol gol)
        {
            if (id != gol.Id)
            {
                return BadRequest();
            }

            _context.Entry(gol).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GolExists(id))
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

        // POST: api/Goles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Gol>> PostGol(Gol gol)
        {
            _context.Goles.Add(gol);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGol", new { id = gol.Id }, gol);
        }

        // DELETE: api/Goles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGol(int id)
        {
            var gol = await _context.Goles.FindAsync(id);
            if (gol == null)
            {
                return NotFound();
            }

            _context.Goles.Remove(gol);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GolExists(int id)
        {
            return _context.Goles.Any(e => e.Id == id);
        }
    }
}
