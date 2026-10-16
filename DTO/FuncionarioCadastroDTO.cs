namespace AplicacaoBackAula.DTO
{
    public class FuncionarioCadastroDTO : PessoaEnderecoDTO
    {
        public string? Cargo { get; set; }
        public DateOnly DataAdmissao { get; set; }
    }
}
