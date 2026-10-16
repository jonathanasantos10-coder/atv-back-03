using AplicacaoBackAula.DTO;
using AplicacaoBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{
  
    [Route("api/[controller]")]
    [ApiController]
    public class FuncionarioController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;

        public FuncionarioController(BdAulaBackendContext context)
        {
            _context = context;
        }

    
        [HttpGet]
        public async Task<IActionResult> GetFuncionarios()
        {
            var funcionarios = await _context.Funcionarios
                .Include(f => f.IdpessoaNavigation)
                .ThenInclude(p => p.Enderecos)
                .ToListAsync();

            return Ok(funcionarios);
        }

       
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFuncionario(int id)
        {
            var funcionario = await _context.Funcionarios
                .Include(f => f.IdpessoaNavigation)
                .ThenInclude(p => p.Enderecos)
                .FirstOrDefaultAsync(f => f.Idpessoa == id);

            if (funcionario == null)
            {
                return NotFound(new { mensagem = "FUNCIONÁRIO NÃO LOCALIZADO!" });
            }

            return Ok(funcionario);
        }

     
        [HttpPost]
        public async Task<IActionResult> PostFuncionario(FuncionarioCadastroDTO dados)
        {
            if (string.IsNullOrWhiteSpace(dados.Nome) || string.IsNullOrWhiteSpace(dados.Email) ||
                string.IsNullOrWhiteSpace(dados.Cargo))
            {
                return BadRequest(new { mensagem = "INFORME NOME, E-MAIL E CARGO!" });
            }

            if (dados.Cpf <= 0 || dados.Telefone == null || dados.Telefone.Value <= 0)
            {
                return BadRequest(new { mensagem = "INFORME CPF E TELEFONE VÁLIDOS!" });
            }

            if (dados.DataNascimento.Year < 1000 || dados.DataAdmissao.Year < 1000)
            {
                return BadRequest(new { mensagem = "INFORME DATAS DE NASCIMENTO E ADMISSÃO VÁLIDAS!" });
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
                    Nome = dados.Nome,
                    Cpf = dados.Cpf,
                    DataNascimento = dados.DataNascimento,
                    Sexo = dados.Sexo,
                    Email = dados.Email,
                    Telefone = dados.Telefone,
                    AtivoPessoa = dados.AtivoPessoa 
                };

                _context.Pessoas.Add(pessoa);
                await _context.SaveChangesAsync();

                var funcionario = new Funcionario
                {
                    Idpessoa = pessoa.Idpessoa,
                    Cargo = dados.Cargo,
                    DataAdmissao = dados.DataAdmissao
                };

                _context.Funcionarios.Add(funcionario);

                foreach (var enderecoDTO in dados.Enderecos)
                {
                    var endereco = new Endereco
                    {
                        Idpessoa = pessoa.Idpessoa,
                        Cep = enderecoDTO.Cep,
                        Numero = enderecoDTO.Numero,
                        Complemento = enderecoDTO.Complemento
                    };
                    _context.Enderecos.Add(endereco);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var funcionarioCadastrado = await _context.Funcionarios
                    .Include(f => f.IdpessoaNavigation)
                    .ThenInclude(p => p.Enderecos)
                    .FirstOrDefaultAsync(f => f.Idpessoa == funcionario.Idpessoa);

                return CreatedAtAction(
                    nameof(GetFuncionario),
                    new { id = funcionario.Idpessoa },
                    (funcionarioCadastrado)
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO CADASTRAR FUNCIONÁRIO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutFuncionario(int id, FuncionarioCadastroDTO dados)
        {
            var funcionario = await _context.Funcionarios
                .Include(f => f.IdpessoaNavigation)
                .ThenInclude(p => p.Enderecos)
                .FirstOrDefaultAsync(f => f.Idpessoa == id);

            if (funcionario == null)
            {
                return NotFound(new { mensagem = "FUNCIONÁRIO NÃO LOCALIZADO!" });
            }

            if (dados.Enderecos == null)
            {
                return BadRequest(new { mensagem = "ENVIE A LISTA DE ENDEREÇOS!" });
            }

            if (dados.DataNascimento.Year < 1000 || dados.DataAdmissao.Year < 1000)
            {
                return BadRequest(new { mensagem = "INFORME DATAS DE NASCIMENTO E ADMISSÃO VÁLIDAS!" });
            }

            foreach (var enderecoDTO in dados.Enderecos)
            {
                if (enderecoDTO.Idendereco != 0 &&
                    !funcionario.IdpessoaNavigation.Enderecos.Any(e => e.Idendereco == enderecoDTO.Idendereco))
                {
                    return BadRequest(new { mensagem = "UM DOS ENDEREÇOS NÃO PERTENCE A ESTE FUNCIONÁRIO!" });
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pessoa = funcionario.IdpessoaNavigation;
                pessoa.Nome = dados.Nome;
                pessoa.Cpf = dados.Cpf;
                pessoa.DataNascimento = dados.DataNascimento;
                pessoa.Sexo = dados.Sexo;
                pessoa.Email = dados.Email;
                pessoa.Telefone = dados.Telefone;
                pessoa.AtivoPessoa = dados.AtivoPessoa;

                funcionario.Cargo = dados.Cargo;
                funcionario.DataAdmissao = dados.DataAdmissao;

                var enderecosOriginais = pessoa.Enderecos.ToList();
                var idsQueContinuam = new List<int>();

                foreach (var enderecoDTO in dados.Enderecos)
                {
                    if (enderecoDTO.Idendereco == 0)
                    {
                        _context.Enderecos.Add(new Endereco
                        {
                            Idpessoa = pessoa.Idpessoa,
                            Cep = enderecoDTO.Cep,
                            Numero = enderecoDTO.Numero,
                            Complemento = enderecoDTO.Complemento
                        });
                    }
                    else
                    {
                        var endereco = pessoa.Enderecos.First(e => e.Idendereco == enderecoDTO.Idendereco);
                        endereco.Cep = enderecoDTO.Cep;
                        endereco.Numero = enderecoDTO.Numero;
                        endereco.Complemento = enderecoDTO.Complemento;
                        idsQueContinuam.Add(endereco.Idendereco);
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
                    mensagem = "ERRO AO ALTERAR FUNCIONÁRIO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        // EXCLUIR O VÍNCULO DE FUNCIONÁRIO. A PESSOA FICA CADASTRADA.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuncionario(int id)
        {
            var funcionario = await _context.Funcionarios
                .FirstOrDefaultAsync(f => f.Idpessoa == id);

            if (funcionario == null)
            {
                return NotFound(new { mensagem = "FUNCIONÁRIO NÃO LOCALIZADO!" });
            }

            if (await _context.Atendimentoitems.AnyAsync(a => a.Idfuncionario == id))
            {
                return Conflict(new { mensagem = "O FUNCIONÁRIO POSSUI ITENS DE ATENDIMENTO REGISTRADOS E NÃO PODE SER EXCLUÍDO!" });
            }

            try
            {
                _context.Funcionarios.Remove(funcionario);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensagem = "ERRO AO EXCLUIR FUNCIONÁRIO!",
                    erro = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
    }
}
