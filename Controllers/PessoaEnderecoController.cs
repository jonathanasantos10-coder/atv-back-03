using AplicacaoBackAula.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{
    // Controller para cadastrar pessoas e seus endereços.
    [Route("api/[controller]")]
    [ApiController]
    public class PessoaEnderecoController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;

        public PessoaEnderecoController(BdAulaBackendContext context)
        {
            _context = context;
        }

        // LISTAR TODAS AS PESSOAS E OS ENDEREÇOS
        [HttpGet]
        public async Task<IActionResult> GetPessoasEnderecos()
        {
            var pessoas = await _context.Pessoas
                .Include(p => p.Enderecos)
                .ToListAsync();

            
            return Ok(pessoas);
        }

        // BUSCAR UMA PESSOA PELO ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPessoaEndereco(int id)
        {
            var pessoa = await _context.Pessoas
                .Include(p => p.Enderecos)
                .FirstOrDefaultAsync(p => p.Idpessoa == id);

            if (pessoa == null)
            {
                return NotFound(new { mensagem = "PESSOA NÃO LOCALIZADA!" });
            }

            return Ok(pessoa);
        }

        [HttpPost]
        public async Task<IActionResult> PostPessoaEndereco(PessoaEnderecoDTO dados)
        {
            if (string.IsNullOrWhiteSpace(dados.Nome) || string.IsNullOrWhiteSpace(dados.Email))
            {
                return BadRequest(new { mensagem = "INFORME O NOME E O E-MAIL DA PESSOA!" });
            }

            if (dados.Cpf <= 0 || dados.Telefone == null || dados.Telefone.Value <= 0)
            {
                return BadRequest(new { mensagem = "INFORME UM CPF E UM TELEFONE VÁLIDOS!" });
            }

            if (dados.DataNascimento.Year < 1000)
            {
                return BadRequest(new { mensagem = "INFORME UMA DATA DE NASCIMENTO VÁLIDA!" });
            }

            if (dados.Enderecos == null || dados.Enderecos.Count == 0)
            {
                return BadRequest(new { mensagem = "CADASTRE PELO MENOS UM ENDEREÇO!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pessoa = new Pessoa
                {
                    Nome = dados.Nome ?? "",
                    Cpf = dados.Cpf,
                    DataNascimento = dados.DataNascimento,
                    Sexo = dados.Sexo ?? "N",
                    Email = dados.Email ?? "",
                    Telefone = dados.Telefone,
                    AtivoPessoa = dados.AtivoPessoa ?? "S"
                };

                _context.Pessoas.Add(pessoa);
                await _context.SaveChangesAsync();

                foreach (var enderecoDTO in dados.Enderecos)
                {
                    var endereco = new Endereco
                    {
                        Idpessoa = pessoa.Idpessoa,
                        Cep = enderecoDTO.Cep ?? "",
                        Numero = enderecoDTO.Numero ?? "",
                        Complemento = enderecoDTO.Complemento ?? ""
                    };

                    _context.Enderecos.Add(endereco);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var pessoaCadastrada = await _context.Pessoas
                    .Include(p => p.Enderecos)
                    .FirstOrDefaultAsync(p => p.Idpessoa == pessoa.Idpessoa);

                return CreatedAtAction(
                    nameof(GetPessoaEndereco),
                    new { id = pessoa.Idpessoa },
                    (pessoaCadastrada)
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO CADASTRAR PESSOA!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPessoaEndereco(int id, PessoaEnderecoDTO dados)
        {
            var pessoa = await _context.Pessoas
                .Include(p => p.Enderecos)
                .FirstOrDefaultAsync(p => p.Idpessoa == id);

            if (pessoa == null)
            {
                return NotFound(new { mensagem = "PESSOA NÃO LOCALIZADA!" });
            }

            if (dados.Enderecos == null)
            {
                return BadRequest(new { mensagem = "ENVIE A LISTA DE ENDEREÇOS!" });
            }

            if (dados.DataNascimento.Year < 1000)
            {
                return BadRequest(new { mensagem = "INFORME UMA DATA DE NASCIMENTO VÁLIDA!" });
            }


            foreach (var enderecoDTO in dados.Enderecos)
            {
                if (enderecoDTO.Idendereco != 0 &&
                    !pessoa.Enderecos.Any(e => e.Idendereco == enderecoDTO.Idendereco))
                {
                    return BadRequest(new { mensagem = "UM DOS ENDEREÇOS NÃO PERTENCE A ESTA PESSOA!" });
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                pessoa.Nome = dados.Nome;
                pessoa.Cpf = dados.Cpf;
                pessoa.DataNascimento = dados.DataNascimento;
                pessoa.Sexo = dados.Sexo;
                pessoa.Email = dados.Email;
                pessoa.Telefone = dados.Telefone;
                pessoa.AtivoPessoa = dados.AtivoPessoa;

                var enderecosOriginais = pessoa.Enderecos.ToList();
                var idsQueContinuam = new List<int>();

                foreach (var enderecoDTO in dados.Enderecos)
                {
                    if (enderecoDTO.Idendereco == 0)
                    {
                  
                        var enderecoNovo = new Endereco
                        {
                            Idpessoa = pessoa.Idpessoa,
                            Cep = enderecoDTO.Cep,
                            Numero = enderecoDTO.Numero,
                            Complemento = enderecoDTO.Complemento
                        };
                        _context.Enderecos.Add(enderecoNovo);
                    }
                    else
                    {
                        var enderecoExistente = pessoa.Enderecos
                            .First(e => e.Idendereco == enderecoDTO.Idendereco);

                        enderecoExistente.Cep = enderecoDTO.Cep;
                        enderecoExistente.Numero = enderecoDTO.Numero;
                        enderecoExistente.Complemento = enderecoDTO.Complemento;
                        idsQueContinuam.Add(enderecoExistente.Idendereco);
                    }
                }

                var enderecosParaExcluir = enderecosOriginais
                    .Where(e => !idsQueContinuam.Contains(e.Idendereco))
                    .ToList();

                _context.Enderecos.RemoveRange(enderecosParaExcluir);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO ALTERAR PESSOA!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePessoaEndereco(int id)
        {
            var pessoa = await _context.Pessoas
                .Include(p => p.Enderecos)
                .FirstOrDefaultAsync(p => p.Idpessoa == id);

            if (pessoa == null)
            {
                return NotFound(new { mensagem = "PESSOA NÃO LOCALIZADA!" });
            }

            if (await _context.Funcionarios.AnyAsync(f => f.Idpessoa == id) ||
                await _context.Atendimentos.AnyAsync(a => a.Idpessoa == id))
            {
                return Conflict(new { mensagem = "A PESSOA POSSUI VÍNCULOS COM FUNCIONÁRIO OU ATENDIMENTO E NÃO PODE SER EXCLUÍDA!" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Enderecos.RemoveRange(pessoa.Enderecos);
                _context.Pessoas.Remove(pessoa);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO EXCLUIR PESSOA!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
    }
}
