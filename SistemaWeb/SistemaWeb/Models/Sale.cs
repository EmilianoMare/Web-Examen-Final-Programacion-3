namespace SistemaWeb.Models
{
    // Clase para representar la tabla ventas de sql
    public class Sale
    {
        public int SaleId { get; set; }
        public string? CustomerName { get; set; }
        public DateOnly SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ProductNames { get; set; }

        // Lista de detalles
        public IEnumerable<SaleDetail>? SaleDetails { get; set; }
    }
}
