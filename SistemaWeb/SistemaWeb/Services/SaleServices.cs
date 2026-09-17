using SistemaWeb.Data;
using SistemaWeb.DTOs;
using SistemaWeb.Models;
using System.ComponentModel.DataAnnotations;

namespace SistemaWeb.Services
{
    // Esta clase contiene la lógica relacionada con las ventas.
    // Recibe un SaleRepository para poder acceder a la base de datos.
    public class SaleServices(SaleRepository _repo)
    {

        #region CREATE
        // ============================================================
        // CREATE
        // ============================================================
        // Este método sirve para CREAR una nueva venta.
        // Recibe un SaleDTO, valida los datos, convierte el DTO a Model
        // y finalmente manda el Model al Repository.
        public void Create(SaleDTO sale)
        {
            // Verificamos que el nombre del cliente no esté vacío.
            ValidateSale(sale, false);

            // Creamos un objeto Sale (Model)
            // utilizando los datos que recibimos en el SaleDTO.
            var model = new Sale()
            {
                CustomerName = sale.CustomerName,
                TotalAmount = CalculateTotal(sale.SaleDetails!),

                // Convertimos cada SaleDetailDTO en un SaleDetail.
                // Select() recorre todos los detalles de la venta.
                SaleDetails = sale.SaleDetails?.Select(item => new SaleDetail
                {
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity,
                })
            };

            // Enviamos el Model al Repository
            // para que se encargue de guardarlo en la base de datos.
            _repo.Create(model);
        }

        #endregion

        #region EDIT
        // ============================================================
        // EDIT
        // ============================================================
        // Este método sirve para EDITAR una venta existente.
        // Recibe un SaleDTO, valida los datos, lo convierte a Sale
        // y lo manda al Repository.
        public void Edit(SaleDTO sale)
        {
            // Verificamos que el nombre del cliente no esté vacío.
            ValidateSale(sale, true);

            // Creamos el Model Sale utilizando los datos del DTO.
            var model = new Sale()
            {
                // Necesitamos el ID para saber qué venta modificar.
                SaleId = sale.SaleId,

                CustomerName = sale.CustomerName,
                TotalAmount = CalculateTotal(sale.SaleDetails!),

                // Convertimos los detalles del DTO
                // en objetos SaleDetail.
                SaleDetails = sale.SaleDetails?.Select(item => new SaleDetail
                {
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity,
                })
            };

            // Enviamos el Model al Repository
            // para que actualice la venta en la base de datos.
            _repo.Edit(model);
        }
        #endregion

        #region DELETE
        // ============================================================
        // DELETE
        // ============================================================
        // Este método sirve para ELIMINAR una venta.
        // Recibe el ID de la venta que queremos eliminar.
        public void Delete(int SaleId)
        {
            // Verificamos que se haya enviado un ID válido.
            if (SaleId <= 0)
                throw new ValidationException("El identificador de la venta es obligatorio");

            // Enviamos el ID al Repository
            // para que elimine la venta de la base de datos.
            _repo.Delete(SaleId);
        }
        #endregion

        # region GET ALL
        // ============================================================
        // GET ALL
        // ============================================================
        // Este método sirve para OBTENER TODAS las ventas.
        // Devuelve una colección de SaleDTO.
        public IEnumerable<SaleDTO> GetAll(DateOnly? month = null)
        {
            // Pedimos al Repository todas las ventas.
            var sales = _repo.GetAll(month);

            // Convertimos cada objeto Sale que recibimos del Repository
            // en un objeto SaleDTO.
            IEnumerable<SaleDTO> salesDto = sales.Select(item => new SaleDTO
            {
                SaleId = item.SaleId,
                CustomerName = item.CustomerName,
                SaleDate = item.SaleDate,
                TotalAmount = item.TotalAmount,
                ProductNames = item.ProductNames
            });

            // Devolvemos la lista de ventas convertidas a DTO.
            return salesDto;

        }

        public decimal GetMonthlyTotal(DateOnly month) => _repo.GetMonthlyTotal(month);

        #endregion

        #region GET
        // ============================================================
        // GET
        // ============================================================
        // Este método sirve para OBTENER UNA SOLA venta.
        // Recibe el ID de la venta que queremos buscar.
        public SaleDTO? Get(int SaleId)
        {
            // Verificamos que se haya enviado un ID válido.
            if (SaleId <= 0)
                throw new ValidationException("El identificador de la venta es obligatorio");

            // Le pedimos al Repository la venta correspondiente al ID.
            var model = _repo.Get(SaleId);
            if (model is null)
                return null;

            // Convertimos el Model Sale obtenido
            // en un SaleDTO.
            var saleDto = new SaleDTO
            {
                SaleId = model.SaleId,
                CustomerName = model.CustomerName,
                SaleDate = model.SaleDate,
                TotalAmount = model.TotalAmount,

                // Convertimos cada SaleDetail del Model
                // en un SaleDetailDTO.
                SaleDetails = model.SaleDetails?.Select(item => new SaleDetailDTO
                {
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity
                })
            };

            // Devolvemos la venta convertida a DTO.
            return saleDto;
            #endregion
        }

        private static void ValidateSale(SaleDTO sale, bool requireId)
        {
            if (sale is null)
                throw new ValidationException("La venta es obligatoria");
            if (requireId && sale.SaleId <= 0)
                throw new ValidationException("El identificador de la venta es obligatorio");
            if (string.IsNullOrWhiteSpace(sale.CustomerName))
                throw new ValidationException("El nombre del cliente es obligatorio");
            if (sale.SaleDetails is null || !sale.SaleDetails.Any())
                throw new ValidationException("Debe agregar al menos un producto");
            if (sale.SaleDetails.Any(detail =>
                string.IsNullOrWhiteSpace(detail.ProductName) ||
                detail.Price <= 0 ||
                detail.Quantity <= 0))
                throw new ValidationException("El producto, el precio y la cantidad deben ser válidos");
        }

        private static decimal CalculateTotal(IEnumerable<SaleDetailDTO> details) =>
            details.Sum(detail => detail.Price * detail.Quantity);
    }
}
