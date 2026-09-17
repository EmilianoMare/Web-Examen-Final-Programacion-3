/*
    SistemaWeb - Base de datos de compras y gastos
    SQL Server / SQL Server Express

    Los nombres Sale y SaleDetail se mantienen por compatibilidad
    con la API existente. En el sistema representan gastos y sus
    conceptos asociados.
*/

USE master;
GO

IF DB_ID(N'DemoDB') IS NULL
BEGIN
    CREATE DATABASE DemoDB;
END;
GO

USE DemoDB;
GO

/*
    Tabla principal de gastos.
*/
IF OBJECT_ID(N'dbo.Sale', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sale
    (
        SaleId       int IDENTITY(1, 1) NOT NULL,
        CustomerName varchar(100) NOT NULL,
        SaleDate     date NOT NULL
            CONSTRAINT DF_Sale_SaleDate DEFAULT (CONVERT(date, GETDATE())),
        TotalAmount  decimal(18, 2) NOT NULL,

        CONSTRAINT PK_Sale PRIMARY KEY CLUSTERED (SaleId),
        CONSTRAINT CK_Sale_TotalAmount_NonNegative CHECK (TotalAmount >= 0)
    );
END;
GO

/*
    Detalle de cada gasto.
    ProductName se conserva por compatibilidad y representa el
    concepto del gasto.
*/
IF OBJECT_ID(N'dbo.SaleDetail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SaleDetail
    (
        SaleDetailId int IDENTITY(1, 1) NOT NULL,
        SaleId       int NOT NULL,
        ProductName  varchar(100) NOT NULL,
        Price        decimal(18, 2) NOT NULL,
        Quantity     int NOT NULL,
        SubTotal     AS (Price * Quantity) PERSISTED,

        CONSTRAINT PK_SaleDetail PRIMARY KEY CLUSTERED (SaleDetailId),
        CONSTRAINT FK_SaleDetail_Sale
            FOREIGN KEY (SaleId) REFERENCES dbo.Sale (SaleId)
            ON DELETE CASCADE,
        CONSTRAINT CK_SaleDetail_Price_Positive CHECK (Price > 0),
        CONSTRAINT CK_SaleDetail_Quantity_Positive CHECK (Quantity > 0)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Sale_SaleDate'
      AND object_id = OBJECT_ID(N'dbo.Sale')
)
BEGIN
    CREATE INDEX IX_Sale_SaleDate
        ON dbo.Sale (SaleDate)
        INCLUDE (CustomerName, TotalAmount);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SaleDetail_SaleId'
      AND object_id = OBJECT_ID(N'dbo.SaleDetail')
)
BEGIN
    CREATE INDEX IX_SaleDetail_SaleId
        ON dbo.SaleDetail (SaleId);
END;
GO

/*
    Tipo de tabla para enviar varios conceptos de un gasto
    desde la API en una sola operación.
*/
IF TYPE_ID(N'dbo.SaleDetailType') IS NULL
BEGIN
    EXEC(N'
        CREATE TYPE dbo.SaleDetailType AS TABLE
        (
            ProductName varchar(100) NOT NULL,
            Price       decimal(18, 2) NOT NULL,
            Quantity    int NOT NULL
        );
    ');
END;
GO

/*
    Registra un gasto y todos sus conceptos dentro de una transacción.
*/
CREATE OR ALTER PROCEDURE dbo.sp_createSale
    @CustomerName varchar(100),
    @TotalAmount decimal(18, 2),
    @Details dbo.SaleDetailType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NULLIF(LTRIM(RTRIM(@CustomerName)), '') IS NULL
        THROW 50001, 'El comercio o beneficiario es obligatorio.', 1;

    IF NOT EXISTS (SELECT 1 FROM @Details)
        THROW 50002, 'El gasto debe tener al menos un concepto.', 1;

    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @SaleId int;

        INSERT INTO dbo.Sale (CustomerName, TotalAmount)
        VALUES (@CustomerName, @TotalAmount);

        SET @SaleId = CONVERT(int, SCOPE_IDENTITY());

        INSERT INTO dbo.SaleDetail (SaleId, ProductName, Price, Quantity)
        SELECT @SaleId, ProductName, Price, Quantity
        FROM @Details;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/*
    Modifica un gasto y reemplaza sus conceptos.
*/
CREATE OR ALTER PROCEDURE dbo.sp_editSale
    @SaleId int,
    @CustomerName varchar(100),
    @TotalAmount decimal(18, 2),
    @Details dbo.SaleDetailType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @SaleId <= 0
        THROW 50003, 'El identificador del gasto no es válido.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Sale WHERE SaleId = @SaleId)
        THROW 50004, 'El gasto no existe.', 1;

    IF NOT EXISTS (SELECT 1 FROM @Details)
        THROW 50005, 'El gasto debe tener al menos un concepto.', 1;

    BEGIN TRANSACTION;

    BEGIN TRY
        UPDATE dbo.Sale
        SET CustomerName = @CustomerName,
            TotalAmount = @TotalAmount
        WHERE SaleId = @SaleId;

        DELETE FROM dbo.SaleDetail
        WHERE SaleId = @SaleId;

        INSERT INTO dbo.SaleDetail (SaleId, ProductName, Price, Quantity)
        SELECT @SaleId, ProductName, Price, Quantity
        FROM @Details;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/*
    Elimina un gasto y sus conceptos relacionados.
*/
CREATE OR ALTER PROCEDURE dbo.sp_deleteSale
    @SaleId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @SaleId <= 0
        THROW 50006, 'El identificador del gasto no es válido.', 1;

    BEGIN TRANSACTION;

    BEGIN TRY
        DELETE FROM dbo.SaleDetail
        WHERE SaleId = @SaleId;

        DELETE FROM dbo.Sale
        WHERE SaleId = @SaleId;

        IF @@ROWCOUNT = 0
            THROW 50007, 'El gasto no existe.', 1;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/*
    Obtiene los gastos. @Year y @Month son opcionales y permiten
    filtrar el listado desde la API.
*/
CREATE OR ALTER PROCEDURE dbo.sp_getSales
    @Year int = NULL,
    @Month int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Month IS NOT NULL AND (@Month < 1 OR @Month > 12)
        THROW 50008, 'El mes no es válido.', 1;

    SELECT
        s.SaleId,
        s.CustomerName,
        s.SaleDate,
        s.TotalAmount,
        STRING_AGG(sd.ProductName, ', ')
            WITHIN GROUP (ORDER BY sd.SaleDetailId) AS ProductNames
    FROM dbo.Sale AS s
    LEFT JOIN dbo.SaleDetail AS sd
        ON sd.SaleId = s.SaleId
    WHERE @Year IS NULL
       OR (YEAR(s.SaleDate) = @Year AND MONTH(s.SaleDate) = @Month)
    GROUP BY
        s.SaleId,
        s.CustomerName,
        s.SaleDate,
        s.TotalAmount
    ORDER BY s.SaleDate DESC, s.SaleId DESC;
END;
GO

/*
    Obtiene un gasto y, en un segundo resultado, sus conceptos.
*/
CREATE OR ALTER PROCEDURE dbo.sp_getSale
    @SaleId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SaleId,
        CustomerName,
        SaleDate,
        TotalAmount
    FROM dbo.Sale
    WHERE SaleId = @SaleId;

    SELECT
        SaleDetailId,
        ProductName,
        Price,
        Quantity,
        SubTotal
    FROM dbo.SaleDetail
    WHERE SaleId = @SaleId
    ORDER BY SaleDetailId;
END;
GO

/*
    Comprobación opcional de instalación.
*/
SELECT
    DB_NAME() AS DatabaseName,
    OBJECT_ID(N'dbo.Sale', N'U') AS SaleTableId,
    OBJECT_ID(N'dbo.SaleDetail', N'U') AS SaleDetailTableId,
    OBJECT_ID(N'dbo.sp_getSales', N'P') AS GetExpensesProcedureId;
GO
