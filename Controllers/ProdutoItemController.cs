using AplicacaoBackEnd.Models;
using AplicacaoBackEnd.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{
  
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutoItemController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;

        public ProdutoItemController(BdAulaBackendContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetProdutos()
        {
            var produtos = await _context.Produtos
                .Include(p => p.IditemNavigation)
                    .ThenInclude(i => i.IdcategoriaNavigation)
                .Include(p => p.Estoque)
                .ToListAsync();

            return Ok(produtos);
        }

       
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduto(int id)
        {
            var produto = await _context.Produtos
                .Include(p => p.IditemNavigation)
                    .ThenInclude(i => i.IdcategoriaNavigation)
                .Include(p => p.Estoque)
                .FirstOrDefaultAsync(p => p.Iditem == id);

            if (produto == null)
            {
                return NotFound(new { mensagem = "PRODUTO NÃO LOCALIZADO!" });
            }

            return Ok(produto);
        }

     
        [HttpPost]
        public async Task<IActionResult> PostProduto(ProdutoDTO dados)
        {
            if (string.IsNullOrWhiteSpace(dados.Item) || dados.IdCategoria <= 0)
            {
                return BadRequest(new { mensagem = "INFORME O NOME DO PRODUTO E SUA CATEGORIA!" });
            }

            if (dados.Preco < 0 || dados.PrecoVenda < 0)
            {
                return BadRequest(new { mensagem = "OS PREÇOS NÃO PODEM SER NEGATIVOS!" });
            }

            
            if (!string.IsNullOrWhiteSpace(dados.TipoItem) && dados.TipoItem != "P")
            {
                return BadRequest(new { mensagem = "PARA CADASTRAR SERVIÇOS, USE O ENDPOINT API/ITENS." });
            }

            if (!await _context.Categoria.AnyAsync(c => c.Idcategoria == dados.IdCategoria))
            {
                return BadRequest(new { mensagem = "A CATEGORIA INFORMADA NÃO EXISTE!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                var item = new Item
                {
                    Idcategoria = dados.IdCategoria,
                    Item1 = dados.Item ?? "",
                    DescricaoItem = dados.DescricaoItem ?? "",
                    Preco = (decimal)dados.Preco,
                    TipoItem = "P",
                    AtivoProdutoServico = dados.AtivoProdutoServico ?? "S"
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

              
                var produto = new Produto
                {
                    Iditem = item.Iditem,
                    CodigoPreco = dados.CodigoPreco,
                    PrecoVenda = dados.PrecoVenda,
                    Unidade = dados.Unidade ?? "UN"
                };

                _context.Produtos.Add(produto);
                await _context.SaveChangesAsync();

           
                await transaction.CommitAsync();

                var produtoCadastrado = await _context.Produtos
                    .Include(p => p.IditemNavigation)
                        .ThenInclude(i => i.IdcategoriaNavigation)
                    .Include(p => p.Estoque)
                    .FirstOrDefaultAsync(p => p.Iditem == produto.Iditem);

                return CreatedAtAction(
                    nameof(GetProduto),
                    new { id = produto.Iditem },
                    (produtoCadastrado!)
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO CADASTRAR PRODUTO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduto(int id, ProdutoDTO dados)
        {
            var produto = await _context.Produtos
                .Include(p => p.IditemNavigation)
                .FirstOrDefaultAsync(p => p.Iditem == id);

            if (produto == null)
            {
                return NotFound(new { mensagem = "PRODUTO NÃO LOCALIZADO!" });
            }

            if (dados.IdCategoria <= 0 || string.IsNullOrWhiteSpace(dados.Item))
            {
                return BadRequest(new { mensagem = "INFORME O NOME DO PRODUTO E A CATEGORIA!" });
            }

            if (!await _context.Categoria.AnyAsync(c => c.Idcategoria == dados.IdCategoria))
            {
                return BadRequest(new { mensagem = "A CATEGORIA INFORMADA NÃO EXISTE!" });
            }

            if (dados.Preco < 0 || dados.PrecoVenda < 0)
            {
                return BadRequest(new { mensagem = "OS PREÇOS NÃO PODEM SER NEGATIVOS!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                
                produto.CodigoPreco = dados.CodigoPreco;
                produto.PrecoVenda = dados.PrecoVenda;
                produto.Unidade = dados.Unidade ?? "UN";

                produto.IditemNavigation.Idcategoria = dados.IdCategoria;
                produto.IditemNavigation.Item1 = dados.Item;
                produto.IditemNavigation.DescricaoItem = dados.DescricaoItem ?? "";
                produto.IditemNavigation.Preco = (decimal)dados.Preco;
                produto.IditemNavigation.TipoItem = "P";
                produto.IditemNavigation.AtivoProdutoServico = dados.AtivoProdutoServico ?? "S";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO ALTERAR PRODUTO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

      
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduto(int id)
        {
            var produto = await _context.Produtos
                .Include(p => p.IditemNavigation)
                .Include(p => p.Estoque)
                .FirstOrDefaultAsync(p => p.Iditem == id);

            if (produto == null)
            {
                return NotFound(new { mensagem = "PRODUTO NÃO LOCALIZADO!" });
            }

            if (await _context.EntradaProdutos.AnyAsync(e => e.Iditem == id) ||
                await _context.Atendimentoitems.AnyAsync(a => a.Iditem == id))
            {
                return Conflict(new { mensagem = "O PRODUTO POSSUI ENTRADAS OU ATENDIMENTOS RELACIONADOS E NÃO PODE SER EXCLUÍDO!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                
                if (produto.Estoque != null)
                {
                    _context.Estoques.Remove(produto.Estoque);
                    await _context.SaveChangesAsync();
                }

                _context.Produtos.Remove(produto);
                await _context.SaveChangesAsync();

                _context.Items.Remove(produto.IditemNavigation);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO EXCLUIR PRODUTO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

     
        }
    }

