using AplicacaoBackAula.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItensController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;

        public ItensController(BdAulaBackendContext context)
        {
            _context = context;
        }

      
        [HttpGet]
        public async Task<IActionResult> GetItens()
        {
            var itens = await _context.Items
                .Include(i => i.IdcategoriaNavigation)
                .Where(i => i.TipoItem == "S")
                .ToListAsync();

            return Ok(itens);
        }

    
        [HttpGet("{id}")]
        public async Task<IActionResult> GetItem(int id)
        {
            var item = await _context.Items
                .Include(i => i.IdcategoriaNavigation)
                .FirstOrDefaultAsync(i => i.Iditem == id && i.TipoItem == "S");

            if (item == null)
            {
                return NotFound(new { mensagem = "SERVIÇO NÃO LOCALIZADO!" });
            }

            return Ok(item);
        }

        
        [HttpPost]
        public async Task<IActionResult> PostItem(ItemDTO dados)
        {
            if (string.IsNullOrWhiteSpace(dados.Item) || dados.IdCategoria <= 0)
            {
                return BadRequest(new { mensagem = "INFORME O NOME DO SERVIÇO E A CATEGORIA!" });
            }

            if (dados.Preco < 0)
            {
                return BadRequest(new { mensagem = "O PREÇO NÃO PODE SER NEGATIVO!" });
            }

            if (!await _context.Categoria.AnyAsync(c => c.Idcategoria == dados.IdCategoria))
            {
                return BadRequest(new { mensagem = "A CATEGORIA INFORMADA NÃO EXISTE!" });
            }

            try
            {
                var item = new Item
                {
                    Idcategoria = dados.IdCategoria,
                    Item1 = dados.Item ?? "",
                    DescricaoItem = dados.DescricaoItem ?? "",
                    Preco = dados.Preco,
                    TipoItem = "S",
                    AtivoProdutoServico = dados.AtivoProdutoServico ?? "S"
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                var itemCadastrado = await _context.Items
                    .Include(i => i.IdcategoriaNavigation)
                    .FirstOrDefaultAsync(i => i.Iditem == item.Iditem);

                return Ok(itemCadastrado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO CADASTRAR SERVIÇO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> PutItem(int id, ItemDTO dados)
        {
            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Iditem == id && i.TipoItem == "S");

            if (item == null)
            {
                return NotFound(new { mensagem = "SERVIÇO NÃO LOCALIZADO!" });
            }

            if (string.IsNullOrWhiteSpace(dados.Item) || dados.IdCategoria <= 0 || dados.Preco < 0)
            {
                return BadRequest(new { mensagem = "CONFIRA O NOME, A CATEGORIA E O PREÇO DO SERVIÇO!" });
            }

            if (!await _context.Categoria.AnyAsync(c => c.Idcategoria == dados.IdCategoria))
            {
                return BadRequest(new { mensagem = "A CATEGORIA INFORMADA NÃO EXISTE!" });
            }

            try
            {
                item.Idcategoria = dados.IdCategoria;
                item.Item1 = dados.Item ?? "";
                item.DescricaoItem = dados.DescricaoItem ?? "";
                item.Preco = dados.Preco;
                item.AtivoProdutoServico = dados.AtivoProdutoServico ?? "S";

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO ALTERAR SERVIÇO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Iditem == id && i.TipoItem == "S");

            if (item == null)
            {
                return NotFound(new { mensagem = "SERVIÇO NÃO LOCALIZADO!" });
            }

            if (await _context.Atendimentoitems.AnyAsync(a => a.Iditem == id))
            {
                return Conflict(new { mensagem = "O SERVIÇO POSSUI ATENDIMENTOS RELACIONADOS E NÃO PODE SER EXCLUÍDO!" });
            }

            try
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO EXCLUIR SERVIÇO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
    }
}
