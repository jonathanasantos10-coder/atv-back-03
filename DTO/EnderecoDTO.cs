namespace AplicacaoBackAula.DTO
{
    public class EnderecoDTO
    {
        // Se for um endereço novo, deixe Idendereco como 0.
        public int Idendereco { get; set; }
        public string? Cep { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
    }
}
