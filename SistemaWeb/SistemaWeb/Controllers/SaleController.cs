#region Usings
// Importamos las herramientas necesarias de ASP.NET Core MVC.
// Nos permite utilizar Controller, IActionResult, HttpPost, HttpGet, etc.
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

// Importamos el DTO que utilizamos para recibir los datos de una venta.
using SistemaWeb.DTOs;

// Importamos los Services.
// El Controller utilizará SaleServices para ejecutar la lógica de las ventas.
using SistemaWeb.Services;
#endregion

// Namespace donde se encuentra nuestro Controller.
namespace SistemaWeb.Controllers
{
    // Creamos el Controller de las ventas.
    //
    // SaleController hereda de Controller, por lo que puede utilizar
    // todas las funcionalidades de ASP.NET Core MVC.
    //
    // (SaleServices _service) es un primary constructor.
    // ASP.NET Core le proporciona automáticamente una instancia de SaleServices
    // mediante Dependency Injection.
    //
    // _service será utilizado para llamar a los métodos del Service.
    public class SaleController(SaleServices _service, ILogger<SaleController> _logger) : Controller
    {
        #region --- VISTAS RAZOR ---

        // Método que devuelve la vista para agregar o editar una venta.
        //
        // IActionResult representa el resultado de una acción del Controller.
        public IActionResult AddEdit()
        {
            // Devuelve la vista AddEdit.cshtml.
            return View();
        }

        // Método que devuelve la vista principal de las ventas.
        public IActionResult Index()
        {
            // Devuelve la vista Index.cshtml.
            return View();
        }

        #endregion

        #region --- MÉTODOS HTTP GET (Lectura / Consultas) ---

        // Indica que este método responde a peticiones HTTP GET.
        //
        // GET normalmente se utiliza para OBTENER información.
        [HttpGet]

        // Método encargado de obtener TODAS las ventas.
        public IActionResult GetAll([FromQuery] string? month)
        {
            try
            {
                DateOnly? selectedMonth = null;
                if (!string.IsNullOrWhiteSpace(month) &&
                    (!DateOnly.TryParseExact(month, "yyyy-MM", out var parsedMonth) || parsedMonth.Day != 1))
                    return BadRequest(new { message = "El mes debe tener el formato AAAA-MM." });
                if (!string.IsNullOrWhiteSpace(month))
                    selectedMonth = DateOnly.ParseExact(month, "yyyy-MM");

                // Llamamos al Service para obtener todas las ventas.
                //
                // Ok() convierte el resultado en una respuesta HTTP 200.
                //
                // Si GetAll() devuelve una lista de ventas,
                // ASP.NET Core puede convertirla automáticamente a JSON.
                return Ok(_service.GetAll(selectedMonth));
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales");
                return StatusCode(500, new { message = "No se pudieron cargar las ventas." });
            }
        }

        [HttpGet]
        public IActionResult GetMonthlyTotal([FromQuery] string month)
        {
            try
            {
                if (!DateOnly.TryParseExact($"{month}-01", "yyyy-MM-dd", out var selectedMonth))
                    return BadRequest(new { message = "El mes debe tener el formato AAAA-MM." });

                return Ok(new { total = _service.GetMonthlyTotal(selectedMonth) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly expense total for {Month}", month);
                return StatusCode(500, new { message = "No se pudo calcular el total mensual." });
            }
        }

        // Indica que este método responde a peticiones HTTP GET.
        [HttpGet]

        // Método encargado de obtener UNA venta específica.
        //
        // Recibimos el ID de la venta mediante [FromQuery].
        //
        // Ejemplo:
        // /Sale/Get?SaleId=5
        public IActionResult Get([FromQuery] int SaleId)
        {
            try
            {
                // Enviamos el ID al Service.
                //
                // El Service busca la venta correspondiente.
                var sale = _service.Get(SaleId);
                return sale is null ? NotFound(new { message = "Venta no encontrada." }) : Ok(sale);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sale {SaleId}", SaleId);
                return StatusCode(500, new { message = "No se pudo cargar la venta." });
            }
        }

        #endregion

        #region --- MÉTODOS HTTP POST (Creación) ---

        // Indica que este método responde a peticiones HTTP POST.
        //
        // POST normalmente se utiliza para CREAR nuevos registros.
        [HttpPost]

        // Método encargado de crear una nueva venta.
        //
        // SaleDTO request contiene los datos enviados desde el cliente.
        //
        // [FromBody] indica que ASP.NET Core debe obtener esos datos
        // desde el cuerpo (body) de la petición HTTP.
        public IActionResult Create([FromBody] SaleDTO request)
        {
            try
            {
                // Enviamos los datos recibidos al Service.
                //
                // El Controller NO se encarga de crear directamente
                // la venta en la base de datos.
                //
                // El Service contiene la lógica necesaria para hacerlo.
                _service.Create(request);

                // Si todo salió correctamente, devolvemos HTTP 200 (OK)
                // junto con un mensaje.
                return Ok(new { message = "Gasto guardado correctamente." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale");
                return StatusCode(500, new { message = "No se pudo guardar el gasto." });
            }
        }

        #endregion

        #region --- MÉTODOS HTTP PUT (Edición / Actualización) ---

        // Indica que este método responde a peticiones HTTP PUT.
        //
        // PUT normalmente se utiliza para MODIFICAR un registro existente.
        [HttpPut]

        // Método encargado de modificar una venta.
        //
        // Recibimos los nuevos datos de la venta mediante SaleDTO.
        public IActionResult Edit([FromBody] SaleDTO request)
        {
            try
            {
                // Enviamos los datos al Service para que realice
                // la modificación.
                _service.Edit(request);

                // Si la modificación fue correcta,
                // devolvemos HTTP 200 (OK).
                return Ok(new { message = "Gasto modificado correctamente." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing sale");
                return StatusCode(500, new { message = "No se pudo modificar el gasto." });
            }
        }

        #endregion

        #region --- MÉTODOS HTTP DELETE (Eliminación) ---

        // Indica que este método responde a peticiones HTTP DELETE.
        //
        // DELETE normalmente se utiliza para ELIMINAR registros.
        [HttpDelete]

        // Método encargado de eliminar una venta.
        //
        // [FromQuery] indica que SaleId viene desde la URL
        // como parámetro de consulta.
        //
        // Ejemplo:
        // /Sale/Delete?SaleId=5
        public IActionResult Delete([FromQuery] int SaleId)
        {
            try
            {
                // Enviamos el ID de la venta al Service.
                //
                // El Service será el encargado de realizar
                // la eliminación.
                _service.Delete(SaleId);

                // Si todo salió correctamente,
                // devolvemos HTTP 200.
                return Ok(new { message = "Gasto eliminado correctamente." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sale {SaleId}", SaleId);
                return StatusCode(500, new { message = "No se pudo eliminar el gasto." });
            }
        }

        #endregion
    }
}
