using AplicacaoBackAula.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{
   
    [Route("api/[controller]")]
    [ApiController]
    public class AtendimentoController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;

        public AtendimentoController(BdAulaBackendContext context)
        {
            _context = context;
        }



        [HttpGet]
        public async Task<IActionResult> GetAtendimentos()
        {
            var atendimentos = await _context.Atendimentos
                .OrderBy(a => a.Idatendimento)
                .Select(a => new AtendimentoDTO
                {
                    IdAtendimento = a.Idatendimento,
                    IdPessoa = a.Idpessoa,
                    NomePessoa = a.IdpessoaNavigation.Nome,
                    DataAtendimento = a.DataAtendimento,
                    StatusAtendimento = a.StatusAtendimento,

                    Itens = a.Atendimentoitems.Select(ai =>
                        new AtendimentoItemDTO
                        {
                            IdAtendimento = ai.Idatendimento,
                            IdItem = ai.Iditem,
                            Item = ai.IditemNavigation.DescricaoItem,
                            NomeFuncionario = ai.IdfuncionarioNavigation.IdpessoaNavigation.Nome,
                            IdFuncionario = ai.Idfuncionario,
                            Cargo = ai.IdfuncionarioNavigation.Cargo,
                            QuantidadeItens = ai.QuantidadeItem,
                            ValorUnitarioEntrada = (decimal)ai.ValorunitarioEntrada
                        }).ToList()
                })
                .ToListAsync();

            return Ok(atendimentos);
        }
     

        /*
         * esse get funciona. 
        [HttpGet]
          public async Task<IActionResult> GetAtendimentos()
          {

              var atendimentos = await _context.Atendimentos
                 .Include(a => a.Atendimentoitems)
                 .ToListAsync();

              return Ok(atendimentos);
          }
        */


        /*
        esse get funciona, porém é muito grande.
        [HttpGet]
        public async Task<IActionResult> GetAtendimentos()
        {
            var atendimentos = await _context.Atendimentos
                //.Include(a => a.IdpessoaNavigation)
                .Include(a => a.Idpessoa)
                .Include(a => a.Atendimentoitems)
                    .ThenInclude(ai => ai.Iditem)
                .Include(a => a.Atendimentoitems)
                    .ThenInclude(ai => ai.Idfuncionario)
                .OrderBy(a => a.Idatendimento)
                .ToListAsync();

            return Ok(atendimentos);
        }
        */

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAtendimento(int id)
        {
            var atendimentos = await _context.Atendimentos
                .OrderBy(a => a.Idatendimento)
                .Select(a => new AtendimentoDTO
                {
                    IdAtendimento = a.Idatendimento,
                    IdPessoa = a.Idpessoa,
                    NomePessoa = a.IdpessoaNavigation.Nome,
                    DataAtendimento = a.DataAtendimento,
                    StatusAtendimento = a.StatusAtendimento,

                    Itens = a.Atendimentoitems.Select(ai =>
                        new AtendimentoItemDTO
                        {
                            IdAtendimento = ai.Idatendimento,
                            IdItem = ai.Iditem,
                            Item = ai.IditemNavigation.DescricaoItem,
                            NomeFuncionario = ai.IdfuncionarioNavigation.IdpessoaNavigation.Nome,
                            IdFuncionario = ai.Idfuncionario,
                            Cargo = ai.IdfuncionarioNavigation.Cargo,
                            QuantidadeItens = ai.QuantidadeItem,
                            ValorUnitarioEntrada = (decimal)ai.ValorunitarioEntrada
                        }).ToList()
                })
                .FirstOrDefaultAsync(a => a.IdAtendimento == id);
               
            return Ok(atendimentos);
        }




        /* 
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAtendimento(int id)
        {
            var atendimento = await _context.Atendimentos
                .Include(a => a.IdpessoaNavigation)
                .Include(a => a.Atendimentoitems)
                    .ThenInclude(ai => ai.IditemNavigation)
                .Include(a => a.Atendimentoitems)
                    .ThenInclude(ai => ai.IdfuncionarioNavigation)
                        .ThenInclude(f => f.IdpessoaNavigation)
                .FirstOrDefaultAsync(a => a.Idatendimento == id);

            if (atendimento == null)
            {
                return NotFound(new { mensagem = "ATENDIMENTO NÃO LOCALIZADO!" });
            }

            return Ok(atendimento);
        }
        */

        [HttpPost]
        public async Task<IActionResult> PostAtendimento(AtendimentoPost dados)
        {
            if (dados.IdPessoa <= 0 || !await _context.Pessoas.AnyAsync(p => p.Idpessoa == dados.IdPessoa))
            {
                return BadRequest(new { mensagem = "A PESSOA INFORMADA NÃO EXISTE!" });
            }

            if (dados.Itens == null || dados.Itens.Count == 0)
            {
                return BadRequest(new { mensagem = "O ATENDIMENTO PRECISA TER PELO MENOS UM ITEM!" });
            }

            if (dados.DataAtendimento.Year < 1000)
            {
                return BadRequest(new { mensagem = "INFORME UMA DATA DE ATENDIMENTO VÁLIDA!" });
            }

            if (dados.Itens.Select(i => i.IdItem).Distinct().Count() != dados.Itens.Count)
            {
                return BadRequest(new { mensagem = "NÃO É POSSÍVEL REPETIR O MESMO ITEM NO ATENDIMENTO!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var atendimento = new Atendimento
                {
                    Idpessoa = dados.IdPessoa,
                    DataAtendimento = dados.DataAtendimento,
                    StatusAtendimento = dados.StatusAtendimento,

                    Atendimentoitems = dados.Itens.Select(item => new Atendimentoitem
                    {
                        Iditem = item.IdItem,
                        Idfuncionario = item.IdFuncionario,
                        QuantidadeItem = item.QuantidadeItens,
                        ValorunitarioEntrada = item.ValorUnitarioEntrada
                    }).ToList()
                };

                _context.Atendimentos.Add(atendimento);
                await _context.SaveChangesAsync();


                /*  var atendimento = new Atendimento
                  {
                      Idpessoa = dados.IdPessoa,
                      DataAtendimento = dados.DataAtendimento,
                      StatusAtendimento = dados.StatusAtendimento
                  };

                  _context.Atendimentos.Add(atendimento);
                  await _context.SaveChangesAsync();
                  /*
                  foreach (var itemDTO in dados.Itens)
                  {
                      var itemAtendimento = new Atendimentoitem
                      {
                          Idatendimento = atendimento.Idatendimento,
                          Iditem = itemDTO.IdItem,
                          Idfuncionario = itemDTO.IdFuncionario,
                          QuantidadeItem = itemDTO.QuantidadeItens,
                          ValorunitarioEntrada = itemDTO.ValorUnitarioEntrada
                      };

                      _context.Atendimentoitems.Add(itemAtendimento);
                  }
                  */
                await _context.SaveChangesAsync();

              
                await transaction.CommitAsync();

                var atendimentoCadastrado = await _context.Atendimentos
                    .Include(a => a.IdpessoaNavigation)
                    .Include(a => a.Atendimentoitems)
                        .ThenInclude(ai => ai.IditemNavigation)
                    .Include(a => a.Atendimentoitems)
                        .ThenInclude(ai => ai.IdfuncionarioNavigation)
                            .ThenInclude(f => f.IdpessoaNavigation)
                    .FirstOrDefaultAsync(a => a.Idatendimento == atendimento.Idatendimento);

                return Ok(atendimentoCadastrado);
           
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO CADASTRAR ATENDIMENTO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

     
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAtendimento(int id, AtendimentoPost dados)
        {
            var atendimento = await _context.Atendimentos
                .Include(a => a.Atendimentoitems)
                    .ThenInclude(ai => ai.IditemNavigation)
                .FirstOrDefaultAsync(a => a.Idatendimento == id);

            if (atendimento == null)
            {
                return NotFound(new { mensagem = "ATENDIMENTO NÃO LOCALIZADO!" });
            }

            if (dados.IdPessoa <= 0 || !await _context.Pessoas.AnyAsync(p => p.Idpessoa == dados.IdPessoa))
            {
                return BadRequest(new { mensagem = "A PESSOA INFORMADA NÃO EXISTE!" });
            }

            if (dados.Itens == null)
            {
                return BadRequest(new { mensagem = "ENVIE A LISTA DE ITENS. USE UMA LISTA VAZIA PARA REMOVER TODOS OS ITENS." });
            }

            if (dados.DataAtendimento.Year < 1000)
            {
                return BadRequest(new { mensagem = "INFORME UMA DATA DE ATENDIMENTO VÁLIDA!" });
            }

            if (dados.Itens.Select(i => i.IdItem).Distinct().Count() != dados.Itens.Count)
            {
                return BadRequest(new { mensagem = "NÃO É POSSÍVEL REPETIR O MESMO ITEM NO ATENDIMENTO!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                atendimento.Idpessoa = dados.IdPessoa;
                atendimento.DataAtendimento = dados.DataAtendimento;
                atendimento.StatusAtendimento = dados.StatusAtendimento ?? "A";

                var idsNovos = dados.Itens.Select(i => i.IdItem).ToList();

                var itensParaRemover = atendimento.Atendimentoitems
                    .Where(ai => !idsNovos.Contains(ai.Iditem))
                    .ToList();

                foreach (var itemAntigo in itensParaRemover)
                {
                 
                    if (itemAntigo.IditemNavigation.TipoItem == "P")
                    {
                        var estoque = await _context.Estoques
                            .FirstOrDefaultAsync(e => e.Iditem == itemAntigo.Iditem);

                        if (estoque == null)
                        {
                            await transaction.RollbackAsync();
                            return Conflict(new { mensagem = "ESTOQUE NÃO LOCALIZADO PARA DEVOLVER UM ITEM REMOVIDO!" });
                        }

                        estoque.QuantidadeEstoque += itemAntigo.QuantidadeItem;
                    }

                    _context.Atendimentoitems.Remove(itemAntigo);
                }

              
                foreach (var itemDTO in dados.Itens)
                {
                    var itemAntigo = atendimento.Atendimentoitems
                        .FirstOrDefault(ai => ai.Iditem == itemDTO.IdItem);

                    if (itemAntigo != null)
                    {
                        
                        if (itemAntigo.IditemNavigation.TipoItem == "P")
                        {
                            var estoque = await _context.Estoques
                                .FirstOrDefaultAsync(e => e.Iditem == itemAntigo.Iditem);

                            if (estoque == null)
                            {
                                await transaction.RollbackAsync();
                                return Conflict(new { mensagem = "ESTOQUE NÃO LOCALIZADO PARA ALTERAR A QUANTIDADE!" });
                            }

                            int diferenca = itemDTO.QuantidadeItens - itemAntigo.QuantidadeItem;

                            if (diferenca > 0 && estoque.QuantidadeEstoque < diferenca)
                            {
                                await transaction.RollbackAsync();
                                return Conflict(new { mensagem = "NÃO HÁ QUANTIDADE SUFICIENTE NO ESTOQUE PARA O ATENDIMENTO." });
                            }

                         
                            estoque.QuantidadeEstoque -= diferenca;
                        }

                        itemAntigo.Idfuncionario = itemDTO.IdFuncionario;
                        itemAntigo.QuantidadeItem = itemDTO.QuantidadeItens;
                        itemAntigo.ValorunitarioEntrada = itemDTO.ValorUnitarioEntrada;
                    }
                    else
                    {
                        var itemNovo = new Atendimentoitem
                        {
                            Idatendimento = atendimento.Idatendimento,
                            Iditem = itemDTO.IdItem,
                            Idfuncionario = itemDTO.IdFuncionario,
                            QuantidadeItem = itemDTO.QuantidadeItens,
                            ValorunitarioEntrada = itemDTO.ValorUnitarioEntrada
                        };
                        _context.Atendimentoitems.Add(itemNovo);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO ALTERAR ATENDIMENTO! VERIFIQUE TAMBÉM A RESPOSTA DA TRIGGER DE ESTOQUE.",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

   
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAtendimento(int id)
        {
            var atendimento = await _context.Atendimentos
                .Include(a => a.Atendimentoitems)
                    .ThenInclude(ai => ai.IditemNavigation)
                .FirstOrDefaultAsync(a => a.Idatendimento == id);

            if (atendimento == null)
            {
                return NotFound(new { mensagem = "ATENDIMENTO NÃO LOCALIZADO!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var itemAtendimento in atendimento.Atendimentoitems)
                {
                    if (itemAtendimento.IditemNavigation.TipoItem == "P")
                    {
                        var estoque = await _context.Estoques
                            .FirstOrDefaultAsync(e => e.Iditem == itemAtendimento.Iditem);

                        if (estoque == null)
                        {
                            await transaction.RollbackAsync();
                            return Conflict(new { mensagem = "ESTOQUE NÃO LOCALIZADO PARA DEVOLVER OS PRODUTOS!" });
                        }

                        estoque.QuantidadeEstoque += itemAtendimento.QuantidadeItem;
                    }
                }

              
                _context.Atendimentoitems.RemoveRange(atendimento.Atendimentoitems);
                await _context.SaveChangesAsync();

                _context.Atendimentos.Remove(atendimento);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO EXCLUIR ATENDIMENTO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

       
        [HttpPost("{id}/itens")]
        public async Task<IActionResult> PostItemAtendimento(int id, AtendimentoItemPOST dados)
        {
            var atendimento = await _context.Atendimentos.FirstOrDefaultAsync(a => a.Idatendimento == id);
            if (atendimento == null)
            {
                return NotFound(new { mensagem = "ATENDIMENTO NÃO LOCALIZADO!" });
            }

            if (await _context.Atendimentoitems.AnyAsync(a => a.Idatendimento == id && a.Iditem == dados.IdItem))
            {
                return Conflict(new { mensagem = "ESSE ITEM JÁ ESTÁ NO ATENDIMENTO. USE O EDIT PARA ALTERÁ-LO!" });
            }

            var erro = await ConferirItemAtendimento(dados);
            if (erro != null) return BadRequest(new { mensagem = erro });

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var itemAtendimento = new Atendimentoitem
                {
                    Idatendimento = id,
                    Iditem = dados.IdItem,
                    Idfuncionario = dados.IdFuncionario,
                    QuantidadeItem = dados.QuantidadeItens,
                    ValorunitarioEntrada = dados.ValorUnitarioEntrada
                };

                _context.Atendimentoitems.Add(itemAtendimento);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

              
                return CreatedAtAction(nameof(GetAtendimento), new { id }, new { mensagem = "ITEM ADICIONADO AO ATENDIMENTO!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO ADICIONAR ITEM AO ATENDIMENTO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPut("{id}/itens/{idItem}")]
        public async Task<IActionResult> PutItemAtendimento(int id, int idItem, AtendimentoItemPOST dados)
        {
            if (idItem != dados.IdItem)
            {
                return BadRequest(new { mensagem = "O ID DO ITEM NA URL DEVE SER IGUAL AO ID ENVIADO NO JSON!" });
            }

            var itemAtendimento = await _context.Atendimentoitems
                .Include(ai => ai.IditemNavigation)
                .FirstOrDefaultAsync(ai => ai.Idatendimento == id && ai.Iditem == idItem);

            if (itemAtendimento == null)
            {
                return NotFound(new { mensagem = "ITEM NÃO LOCALIZADO NESTE ATENDIMENTO!" });
            }

            var erro = await ConferirItemAtendimento(dados);
            if (erro != null) return BadRequest(new { mensagem = erro });

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (itemAtendimento.IditemNavigation.TipoItem == "P")
                {
                    var estoque = await _context.Estoques.FirstOrDefaultAsync(e => e.Iditem == idItem);
                    if (estoque == null)
                    {
                        await transaction.RollbackAsync();
                        return Conflict(new { mensagem = "ESTOQUE NÃO LOCALIZADO!" });
                    }

                    int diferenca = dados.QuantidadeItens - itemAtendimento.QuantidadeItem;
                    if (diferenca > 0 && estoque.QuantidadeEstoque < diferenca)
                    {
                        await transaction.RollbackAsync();
                        return Conflict(new { mensagem = "NÃO HÁ ESTOQUE SUFICIENTE PARA AUMENTAR A QUANTIDADE!" });
                    }

                    estoque.QuantidadeEstoque -= diferenca;
                }

                itemAtendimento.Idfuncionario = dados.IdFuncionario;
                itemAtendimento.QuantidadeItem = dados.QuantidadeItens;
                itemAtendimento.ValorunitarioEntrada = dados.ValorUnitarioEntrada;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO ALTERAR ITEM DO ATENDIMENTO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }


        [HttpDelete("{id}/itens/{idItem}")]
        public async Task<IActionResult> DeleteItemAtendimento(int id, int idItem)
        {
            var itemAtendimento = await _context.Atendimentoitems
                .Include(ai => ai.IditemNavigation)
                .FirstOrDefaultAsync(ai => ai.Idatendimento == id && ai.Iditem == idItem);

            if (itemAtendimento == null)
            {
                return NotFound(new { mensagem = "ITEM NÃO LOCALIZADO NESTE ATENDIMENTO!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (itemAtendimento.IditemNavigation.TipoItem == "P")
                {
                    var estoque = await _context.Estoques.FirstOrDefaultAsync(e => e.Iditem == idItem);
                    if (estoque == null)
                    {
                        await transaction.RollbackAsync();
                        return Conflict(new { mensagem = "ESTOQUE NÃO LOCALIZADO!" });
                    }

                    estoque.QuantidadeEstoque += itemAtendimento.QuantidadeItem;
                }

                _context.Atendimentoitems.Remove(itemAtendimento);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO REMOVER ITEM DO ATENDIMENTO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        
        private async Task<string?> ConferirItemAtendimento(AtendimentoItemPOST dados)
        {
            if (dados.IdItem <= 0 || dados.IdFuncionario <= 0)
            {
                return "INFORME O ITEM E O FUNCIONÁRIO RESPONSÁVEL!";
            }

            if (dados.QuantidadeItens <= 0 || dados.ValorUnitarioEntrada < 0)
            {
                return "A QUANTIDADE DEVE SER MAIOR QUE ZERO E O VALOR NÃO PODE SER NEGATIVO!";
            }

            var item = await _context.Items.FirstOrDefaultAsync(i => i.Iditem == dados.IdItem);
            if (item == null)
            {
                return "O ITEM INFORMADO NÃO EXISTE!";
            }

            if (item.AtivoProdutoServico != "S")
            {
                return "O ITEM ESTÁ INATIVO E NÃO PODE SER ADICIONADO AO ATENDIMENTO!";
            }

            if (!await _context.Funcionarios.AnyAsync(f => f.Idpessoa == dados.IdFuncionario))
            {
                return "O FUNCIONÁRIO INFORMADO NÃO EXISTE!";
            }

            if (item.TipoItem == "P" && !await _context.Estoques.AnyAsync(e => e.Iditem == dados.IdItem))
            {
                return "O PRODUTO NÃO TEM UM REGISTRO DE ESTOQUE!";
            }

            return null;
        }

    }
}
