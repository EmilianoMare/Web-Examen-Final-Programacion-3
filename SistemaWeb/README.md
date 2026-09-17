# SistemaWeb

Sistema web para registrar y consultar compras y gastos personales. Permite filtrar los gastos por mes y consultar el total gastado durante un período.

## Tecnologías

- ASP.NET Core MVC sobre .NET 8
- C#
- SQL Server Express
- Microsoft.Data.SqlClient
- Bootstrap
- jQuery
- DataTables
- SweetAlert2

## Funcionalidades

- Registrar un gasto con uno o varios conceptos.
- Calcular el importe total a partir del precio y la cantidad.
- Consultar gastos por mes.
- Consultar el total gastado en un mes.
- Editar gastos existentes.
- Eliminar gastos.
- Mostrar los conceptos asociados a cada gasto.
- Validar los datos antes de guardarlos.

## Requisitos

- .NET SDK 8.
- SQL Server Express con una instancia llamada `SQLEXPRESS`.
- Base de datos `DemoDB`.
- Visual Studio o Visual Studio Code.

## Configuración de la base de datos

La cadena de conexión se encuentra en [appsettings.json](./SistemaWeb/appsettings.json):

```json
"DefaultConnection": "Data Source=(local)\\SQLEXPRESS;Database=DemoDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

Para instalar la base de datos desde cero, ejecutar el script
[Database/DemoDB.sql](./Database/DemoDB.sql) en SQL Server Management Studio, Azure Data Studio o con `sqlcmd`.

La base de datos debe contener las tablas:

- `Sale`
- `SaleDetail`

También deben existir el tipo de tabla y los procedimientos almacenados utilizados por el repositorio:

```text
dbo.SaleDetailType
dbo.sp_createSale
dbo.sp_editSale
dbo.sp_getSale
dbo.sp_getSales
dbo.sp_deleteSale
```

## Ejecutar el proyecto

Desde la raíz del repositorio:

```powershell
dotnet restore
dotnet build .\SistemaWeb.sln
dotnet run --project .\SistemaWeb\SistemaWeb.csproj --urls http://localhost:5202
```

Luego abrir:

```text
http://localhost:5202
```

La pantalla principal de compras y gastos está disponible en:

```text
http://localhost:5202/Sale/Index
```

## API

La API utiliza JSON y está implementada en [SaleController.cs](./SistemaWeb/Controllers/SaleController.cs).

### Obtener gastos de un mes

```http
GET /Sale/GetAll?month=2026-09
```

Ejemplo de respuesta:

```json
[
  {
    "saleId": 1,
    "customerName": "Supermercado",
    "saleDate": "2026-09-16",
    "totalAmount": 8000.00,
    "productNames": "Alimentos, Bebidas",
    "saleDetails": null
  }
]
```

En la interfaz, estos campos se muestran como:

- `customerName`: comercio o beneficiario.
- `productNames`: conceptos del gasto.
- `totalAmount`: importe.

### Obtener el total mensual

```http
GET /Sale/GetMonthlyTotal?month=2026-09
```

Respuesta:

```json
{
  "total": 8000.00
}
```

### Obtener un gasto con sus detalles

```http
GET /Sale/Get?SaleId=1
```

### Crear un gasto

```http
POST /Sale/Create
Content-Type: application/json
```

```json
{
  "saleId": 0,
  "customerName": "Supermercado",
  "totalAmount": 0,
  "saleDetails": [
    {
      "productName": "Alimentos",
      "price": 1500.00,
      "quantity": 2
    }
  ]
}
```

El servidor calcula el total usando `price * quantity`.

### Editar un gasto

```http
PUT /Sale/Edit
Content-Type: application/json
```

Utiliza el mismo formato de creación, incluyendo el `saleId` existente.

### Eliminar un gasto

```http
DELETE /Sale/Delete?SaleId=1
```

## Consumir la API desde Android

Para una aplicación Android de solo lectura:

- Emulador de Android Studio:

  ```text
  http://10.0.2.2:5202/
  ```

- Dispositivo físico:

  ```text
  http://IP-DE-LA-PC:5202/
  ```

La computadora y el teléfono deben estar en la misma red. La aplicación Android debe tener permiso de Internet:

```xml
<uses-permission android:name="android.permission.INTERNET" />
```

Durante el desarrollo local con HTTP puede ser necesario permitir tráfico sin cifrado. Para producción se recomienda utilizar HTTPS.

Los endpoints de consulta son:

```text
GET /Sale/GetAll?month=YYYY-MM
GET /Sale/GetMonthlyTotal?month=YYYY-MM
```

## Arquitectura

El proyecto utiliza una separación por responsabilidades:

- `Controllers`: recibe las solicitudes HTTP y devuelve respuestas.
- `Services`: contiene validaciones y reglas de negocio.
- `Data`: accede a SQL Server mediante procedimientos almacenados.
- `Models`: representa las entidades del sistema.
- `DTOs`: define los datos intercambiados con el cliente.
- `Views`: contiene la interfaz MVC.
- `wwwroot`: contiene JavaScript, CSS y librerías del frontend.

Archivos principales:

- [Program.cs](./SistemaWeb/Program.cs)
- [SaleController.cs](./SistemaWeb/Controllers/SaleController.cs)
- [SaleServices.cs](./SistemaWeb/Services/SaleServices.cs)
- [SaleRepository.cs](./SistemaWeb/Data/SaleRepository.cs)
- [Index.cshtml](./SistemaWeb/Views/Sale/Index.cshtml)
- [AddEdit.cshtml](./SistemaWeb/Views/Sale/AddEdit.cshtml)

## Solución de problemas

### Error de conexión a SQL Server

Verificar que el servicio de SQL Server Express esté iniciado y que la base `DemoDB` exista.

### Error `Could not find stored procedure`

Verificar que el procedimiento almacenado mencionado en el error exista dentro de `DemoDB`.

### Error de DataTables Ajax

Probar directamente:

```text
http://localhost:5202/Sale/GetAll?month=2026-09
```

Si devuelve `500`, revisar los logs de la aplicación y la conexión con SQL Server.

### Android no puede conectarse

- Usar `10.0.2.2` en el emulador, no `localhost`.
- Usar la IP local de la PC en un dispositivo físico.
- Confirmar que la API esté ejecutándose.
- Verificar que el firewall permita el puerto `5202`.
