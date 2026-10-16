namespace AplicacaoBackAula.DTO
{
  
    public class ItemDTO
    {
        public int IdCategoria { get; set; }
        public string? Item { get; set; }
        public string? DescricaoItem { get; set; }
        public decimal Preco { get; set; }
        public string? AtivoProdutoServico { get; set; }
    }
}
