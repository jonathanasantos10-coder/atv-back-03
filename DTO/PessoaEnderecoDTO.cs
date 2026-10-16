namespace AplicacaoBackAula.DTO
{
    public class PessoaEnderecoDTO
    {
        public string? Nome { get; set; }
        public long Cpf { get; set; }
        public DateOnly DataNascimento { get; set; }
        public string? Sexo { get; set; }
        public string? Email { get; set; }
        public long? Telefone { get; set; }
        public string? AtivoPessoa { get; set; }

        
        public List<EnderecoDTO> Enderecos { get; set; } = new List<EnderecoDTO>();
    }
}
