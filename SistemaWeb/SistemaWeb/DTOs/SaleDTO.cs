using SistemaWeb.Models;

namespace SistemaWeb.DTOs
{
    public class SaleDTO
    {
        public int SaleId { get; set; }
        public string? CustomerName { get; set; }
        public DateOnly SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ProductNames { get; set; }

        // Lista de detalles
        public IEnumerable<SaleDetailDTO>? SaleDetails { get; set; }
    }
}
