namespace AplicacaoBackAula.DTO
{
    public class AtendimentoItemDTO
    {
        public int IdAtendimento { get; set; }
        public int IdItem { get; set; }
        public string Item { get; set; }
        public int IdFuncionario { get; set; }
        public string NomeFuncionario { get; set; }
        public string Cargo { get; set; }
        public int QuantidadeItens { get; set; }
        public decimal ValorUnitarioEntrada { get; set; }

    }

    /* public class AtendimentoItemDTO
     {
         public int IdItem { get; set; }
         public int IdFuncionario { get; set; }
         public int QuantidadeItens { get; set; }
         public decimal ValorUnitarioEntrada { get; set; }
     }
    */
}
