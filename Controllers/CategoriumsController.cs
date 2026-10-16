using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class CategoriumController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;

        public CategoriumController(BdAulaBackendContext context)
        {
            _context = context;
        }

     
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Categorium>>> GetCategorias()
        {
            return await _context.Categoria.ToListAsync();
        }


       
        [HttpGet("{id}")]
        public async Task<ActionResult<Categorium>> GetCategoria(int id)
        {
            var categoria = await _context.Categoria
                .FirstOrDefaultAsync(c => c.Idcategoria == id);

            if (categoria == null)
            {
                return NotFound();
            }

            return categoria;
        }


     
        [HttpPost]
        public async Task<ActionResult<Categorium>> PostCategoria(Categorium categoria)
        {
            _context.Categoria.Add(categoria);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategoria),
                new { id = categoria.Idcategoria },
                categoria
            );
        }


 
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategoria(
            int id,
            Categorium categoria)
        {
            if (id != categoria.Idcategoria)
            {
                return BadRequest();
            }

            _context.Entry(categoria).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoriaExiste(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            var categoria = await _context.Categoria
                .FirstOrDefaultAsync(c => c.Idcategoria == id);

            if (categoria == null)
            {
                return NotFound();
            }

            _context.Categoria.Remove(categoria);

            await _context.SaveChangesAsync();

            return NoContent();
        }


        private bool CategoriaExiste(int id)
        {
            return _context.Categoria
                .Any(e => e.Idcategoria == id);
        }
    }
}
