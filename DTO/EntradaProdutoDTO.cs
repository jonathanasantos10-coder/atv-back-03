namespace AplicacaoBackAula.DTO
{
    public class EntradaProdutoDTO
    {
        // Identifica o produto que foi recebido.
        public int IdItem { get; set; }

        // Data em que a entrada aconteceu.
        public DateOnly DataEntradaProduto { get; set; }

        // Quantidade inteira de unidades recebidas.
        public int QuantidadeEntrada { get; set; }

        public string? Unidade { get; set; }
        public decimal ValorUnitarioEntrada { get; set; }
    }
}
