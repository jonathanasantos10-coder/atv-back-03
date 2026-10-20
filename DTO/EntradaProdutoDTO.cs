namespace AplicacaoBackAula.DTO
{
    public class EntradaProdutoDTO
    {
        public int IdItem { get; set; }
        public DateOnly DataEntradaProduto { get; set; }
        public int QuantidadeEntrada { get; set; }
        public string? Unidade { get; set; }
        public decimal ValorUnitarioEntrada { get; set; }
    }
}
