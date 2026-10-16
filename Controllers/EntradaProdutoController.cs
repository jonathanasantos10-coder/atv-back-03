using AplicacaoBackAula.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{
    // Controller para controlar as entradas de produtos no estoque.
    [Route("api/[controller]")]
    [ApiController]
    public class EntradaProdutoController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;

        public EntradaProdutoController(BdAulaBackendContext context)
        {
            _context = context;
        }

        // LISTAR TODAS AS ENTRADAS
        [HttpGet]
        public async Task<IActionResult> GetEntradasProduto()
        {
            var entradas = await _context.EntradaProdutos
                .Include(e => e.IditemNavigation)
                    .ThenInclude(e => e.IditemNavigation)
                .OrderBy(e => e.IdentradaProduto)
                .ToListAsync();

            return Ok(entradas);
        }

        // BUSCAR UMA ENTRADA PELO ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEntradaProduto(int id)
        {
            var entrada = await _context.EntradaProdutos
                .Include(e => e.IditemNavigation)
                    .ThenInclude(e => e.IditemNavigation)
                .FirstOrDefaultAsync(e => e.IdentradaProduto == id);

            if (entrada == null)
            {
                return NotFound(new { mensagem = "ENTRADA DE PRODUTO NÃO LOCALIZADA!" });
            }

            return Ok(entrada);
        }

        // CADASTRAR UMA ENTRADA
        [HttpPost]
        public async Task<IActionResult> PostEntradaProduto(EntradaProdutoDTO dados)
        {
            if (dados.IdItem <= 0)
            {
                return BadRequest(new { mensagem = "INFORME O PRODUTO DA ENTRADA!" });
            }

            if (dados.QuantidadeEntrada <= 0)
            {
                return BadRequest(new { mensagem = "A QUANTIDADE DEVE SER MAIOR QUE ZERO!" });
            }

            if (dados.ValorUnitarioEntrada <= 0)
            {
                return BadRequest(new { mensagem = "O VALOR UNITÁRIO DEVE SER MAIOR QUE ZERO!" });
            }

            if (dados.DataEntradaProduto.Year < 1000)
            {
                return BadRequest(new { mensagem = "INFORME UMA DATA DE ENTRADA VÁLIDA!" });
            }

            var produto = await _context.Produtos
                .Include(p => p.IditemNavigation)
                .Include(p => p.Estoque)
                .FirstOrDefaultAsync(p => p.Iditem == dados.IdItem);

            if (produto == null)
            {
                return BadRequest(new { mensagem = "O PRODUTO INFORMADO NÃO EXISTE!" });
            }

            if (produto.IditemNavigation.TipoItem != "P")
            {
                return BadRequest(new { mensagem = "SÓ É POSSÍVEL REGISTRAR ENTRADA PARA PRODUTOS, NÃO PARA SERVIÇOS!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                
                if (produto.Estoque == null)
                {
                    var estoqueNovo = new Estoque
                    {
                        Iditem = produto.Iditem,
                        QuantidadeEstoque = 0,
                        IditemNavigation = produto
                    };
                    _context.Estoques.Add(estoqueNovo);
                    await _context.SaveChangesAsync();
                    produto.Estoque = estoqueNovo;
                }

                var entrada = new EntradaProduto
                {
                    Iditem = dados.IdItem,
                    DataEntradaProduto = dados.DataEntradaProduto,
                    QuantidadeEntrada = dados.QuantidadeEntrada,
                    Unidade = dados.Unidade ?? produto.Unidade ?? "UN",
                    ValorUnitarioEntrada = dados.ValorUnitarioEntrada
                };

                _context.EntradaProdutos.Add(entrada);
                await _context.SaveChangesAsync();

                
                await transaction.CommitAsync();

                var entradaCadastrada = await _context.EntradaProdutos
                    .Include(e => e.IditemNavigation)
                        .ThenInclude(e => e.IditemNavigation)
                    .FirstOrDefaultAsync(e => e.IdentradaProduto == entrada.IdentradaProduto);

                return CreatedAtAction(
                    nameof(GetEntradaProduto),
                    new { id = entrada.IdentradaProduto },
                    (entradaCadastrada)
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO CADASTRAR ENTRADA DE PRODUTO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutEntradaProduto(int id, EntradaProdutoDTO dados)
        {
            var entrada = await _context.EntradaProdutos
                .FirstOrDefaultAsync(e => e.IdentradaProduto == id);

            if (entrada == null)
            {
                return NotFound(new { mensagem = "ENTRADA DE PRODUTO NÃO LOCALIZADA!" });
            }

            if (entrada.Iditem != dados.IdItem)
            {
                return BadRequest(new { mensagem = "NÃO É POSSÍVEL TROCAR O PRODUTO DA ENTRADA. EXCLUA E CADASTRE OUTRA ENTRADA." });
            }

            if (dados.QuantidadeEntrada <= 0 || dados.ValorUnitarioEntrada <= 0)
            {
                return BadRequest(new { mensagem = "INFORME UMA QUANTIDADE E UM VALOR UNITÁRIO MAIORES QUE ZERO!" });
            }

            if (dados.DataEntradaProduto.Year < 1000)
            {
                return BadRequest(new { mensagem = "INFORME UMA DATA DE ENTRADA VÁLIDA!" });
            }

            var produto = await _context.Produtos
                .Include(p => p.IditemNavigation)
                .Include(p => p.Estoque)
                .FirstOrDefaultAsync(p => p.Iditem == entrada.Iditem);

            if (produto == null || produto.Estoque == null)
            {
                return Conflict(new { mensagem = "O PRODUTO OU SEU ESTOQUE NÃO FOI LOCALIZADO!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                int quantidadeAntiga = (int)(decimal)entrada.QuantidadeEntrada;
                int quantidadeNova = dados.QuantidadeEntrada;
                int diferenca = quantidadeNova - quantidadeAntiga;

                if (produto.Estoque.QuantidadeEstoque + diferenca < 0)
                {
                    return Conflict(new { mensagem = "A ALTERAÇÃO DEIXARIA O ESTOQUE NEGATIVO!" });
                }

                produto.Estoque.QuantidadeEstoque += diferenca;

                entrada.DataEntradaProduto = dados.DataEntradaProduto;
                entrada.QuantidadeEntrada = dados.QuantidadeEntrada;
                entrada.Unidade = dados.Unidade ?? produto.Unidade ?? "UN";
                entrada.ValorUnitarioEntrada = dados.ValorUnitarioEntrada;


                produto.IditemNavigation.Preco = dados.ValorUnitarioEntrada;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO ALTERAR ENTRADA DE PRODUTO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

       
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntradaProduto(int id)
        {
            var entrada = await _context.EntradaProdutos
                .FirstOrDefaultAsync(e => e.IdentradaProduto == id);

            if (entrada == null)
            {
                return NotFound(new { mensagem = "ENTRADA DE PRODUTO NÃO LOCALIZADA!" });
            }

            var estoque = await _context.Estoques.FirstOrDefaultAsync(e => e.Iditem == entrada.Iditem);

            if (estoque == null || estoque.QuantidadeEstoque < entrada.QuantidadeEntrada)
            {
                return Conflict(new { mensagem = "NÃO É POSSÍVEL EXCLUIR A ENTRADA, POIS O ESTOQUE FICARIA NEGATIVO!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
           
                estoque.QuantidadeEstoque -= (int)(decimal)entrada.QuantidadeEntrada;
                _context.EntradaProdutos.Remove(entrada);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO EXCLUIR ENTRADA DE PRODUTO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
    }
}
