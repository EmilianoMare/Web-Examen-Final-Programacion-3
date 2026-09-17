using Microsoft.Data.SqlClient;  // Paquete del nuget


namespace SistemaWeb.Data
{
    public class SqlConnectionFactory
    {
        // Variable de solo lectura
        private readonly string _connectionString;


        // Constructor
        // IConfiguration nos va a permitir acceder al appsettings que es donde agregamos la cadena de conexión.
        public SqlConnectionFactory(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }


        // Método que devuelve SqlConnection del paquete que instalamos
        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}

// Esta clase es para que podamos utilizar la conexión a la base de datos libremente en el proyecto, asi que en el Program especificamos eso...
