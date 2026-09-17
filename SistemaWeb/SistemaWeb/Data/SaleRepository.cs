using System.Data;
using Microsoft.Data.SqlClient;
using SistemaWeb.Models;

namespace SistemaWeb.Data
{
    //Clase Venta Repositorio y hacemos la llamada al servicio de SqlConnetionFactory
    public class SaleRepository(SqlConnectionFactory _connection)
    {
        // Datatable para el detalle de ventas
        private DataTable CreateDataTableDetail(IEnumerable<SaleDetail> details) // Lista del detalle de la venta
        {
            DataTable table = new DataTable(); //Creamos una tabla
            //A esta tabla vamos a agregarle las columnas que necesitamos
            table.Columns.Add("ProductName", typeof(string));
            table.Columns.Add("Price", typeof(decimal));
            table.Columns.Add("Quantity", typeof(int));

            // Vamos a recorrer cada uno de los elementos que se encuentren en la variable detalle para almacenarlo dentro de la tabla que estamos creando
            foreach (var item in details) table.Rows.Add(item.ProductName, item.Price, item.Quantity);
            return table;
                    
        }

        

        public void Create(Sale sale)
        {
            // Recibimos el detalle que esta convertido en una tabla llamando el método CreateDataTableDetail y vamos a pasarle los parametros de venta los detalles
            var detailsTable = CreateDataTableDetail(sale.SaleDetails!);

            
            using var conn = _connection.CreateConnection();// Abrimos la conexión hacia la base de datos
            using var cmd = conn.CreateCommand(); // creamos el comando para poder ejecutar el procedimiento almacenado
            cmd.CommandText = "sp_createSale";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@CustomerName", SqlDbType.VarChar).Value = sale.CustomerName;
            var totalParameter = cmd.Parameters.Add("@TotalAmount", SqlDbType.Decimal);
            totalParameter.Precision = 18;
            totalParameter.Scale = 2;
            totalParameter.Value = sale.TotalAmount;
            var detailsParameter = cmd.Parameters.Add("@Details", SqlDbType.Structured);
            detailsParameter.TypeName = "dbo.SaleDetailType";
            detailsParameter.Value = detailsTable;

            conn.Open();

            cmd.ExecuteNonQuery();


        }

        public void Edit(Sale sale)
        {

            var detailsTable = CreateDataTableDetail(sale.SaleDetails!);

            using var conn = _connection.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_editSale";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@SaleId", SqlDbType.Int).Value = sale.SaleId;
            cmd.Parameters.Add("@CustomerName", SqlDbType.VarChar).Value = sale.CustomerName;
            var totalParameter = cmd.Parameters.Add("@TotalAmount", SqlDbType.Decimal);
            totalParameter.Precision = 18;
            totalParameter.Scale = 2;
            totalParameter.Value = sale.TotalAmount;
            var detailsParameter = cmd.Parameters.Add("@Details", SqlDbType.Structured);
            detailsParameter.TypeName = "dbo.SaleDetailType";
            detailsParameter.Value = detailsTable;

            conn.Open();

            cmd.ExecuteNonQuery();

        }

        public void Delete(int SaleId)
        {


            using var conn = _connection.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_deleteSale";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@SaleId", SqlDbType.Int).Value = SaleId;

            conn.Open();

            cmd.ExecuteNonQuery();

        }

        // Obetener todas las ventas
        public IEnumerable<Sale> GetAll(DateOnly? month = null)
        {

            using var conn = _connection.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_getSales";
            cmd.CommandType = CommandType.StoredProcedure;
            if (month.HasValue)
            {
                cmd.Parameters.Add("@Year", SqlDbType.Int).Value = month.Value.Year;
                cmd.Parameters.Add("@Month", SqlDbType.Int).Value = month.Value.Month;
            }

            conn.Open();

            var reader = cmd.ExecuteReader(); // Ejecutamos el procedimiento a traves de un reader para poder leer el resultado

            var sales = new List<Sale>();

            while (reader.Read())
            {
                sales.Add(new Sale()
                {
                    SaleId = Convert.ToInt32(reader["SaleId"]),
                    CustomerName = reader["CustomerName"].ToString(),
                    SaleDate = DateOnly.FromDateTime(Convert.ToDateTime(reader["SaleDate"])),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                    ProductNames = reader["ProductNames"] == DBNull.Value ? null : reader["ProductNames"].ToString()
                });
            }

            return sales;

        }

        public decimal GetMonthlyTotal(DateOnly month)
        {
            using var conn = _connection.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT COALESCE(SUM(TotalAmount), 0)
                FROM dbo.Sale
                WHERE SaleDate >= @StartDate AND SaleDate < @EndDate
                """;
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = month.ToDateTime(TimeOnly.MinValue);
            cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = month.AddMonths(1).ToDateTime(TimeOnly.MinValue);

            conn.Open();
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }


        // Traer solo una venta y sus detalles
        public Sale? Get(int SaleId)
        {
            using var conn = _connection.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_getSale";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@SaleId", SqlDbType.Int).Value = SaleId;

            conn.Open();

            var reader = cmd.ExecuteReader();

            Sale? sale = null;

            if (reader.Read())
            {
                sale = new Sale()
                {
                    SaleId = Convert.ToInt32(reader["SaleId"]),
                    CustomerName = reader["CustomerName"].ToString(),
                    SaleDate = DateOnly.FromDateTime(Convert.ToDateTime(reader["SaleDate"])),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                    SaleDetails = new List<SaleDetail>()
                };
            }

            if (sale is not null && reader.NextResult())
            {
                var saleDetails = new List<SaleDetail>();

                while (reader.Read())
                {
                    saleDetails.Add(new SaleDetail()
                    {
                        SaleDetailId = Convert.ToInt32(reader["SaleDetailId"]),
                        ProductName = reader["ProductName"].ToString(),
                        Price = Convert.ToDecimal(reader["Price"]),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        SubTotal = Convert.ToDecimal(reader["SubTotal"])
                    });
                }

                sale.SaleDetails = saleDetails;
            }

            return sale;

        }

    }
}
