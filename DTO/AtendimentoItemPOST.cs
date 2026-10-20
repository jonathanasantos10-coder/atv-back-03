namespace AplicacaoBackAula.DTO
{
    public class AtendimentoItemPOST
    {   public int IdAtendimento { get; set; }
        public int IdItem { get; set; }
        public int IdFuncionario { get; set; }
        public int QuantidadeItens { get; set; }
        public decimal ValorUnitarioEntrada { get; set; }
    }
}
