namespace AplicacaoBackAula.DTO
{
    public class AtendimentoPost
    {
        public int IdAtendimento { get; set; }
        public int IdPessoa { get; set; }
        public DateOnly DataAtendimento { get; set; }
        public string StatusAtendimento { get; set; }
        public List<AtendimentoItemPOST> Itens { get; set; } = new();
    }
}
