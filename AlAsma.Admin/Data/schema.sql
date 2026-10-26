/* ============================================================
   AlAsma.Admin — Database schema (SQL Server)
   Tables: Authors, Sales, Operations (+ __EFMigrationsHistory)

   - Safe to run on an EMPTY database and also on a database that already
     has the schema (every object is created only if it does not exist).
   - Run it against the database you want (select it first, or uncomment the
     CREATE DATABASE / USE lines below).
   - Equivalent to the EF Core migrations: InitialCreate + AddOperationsTable.
   ============================================================ */

-- CREATE DATABASE AlAsmaDb;
-- GO
-- USE AlAsmaDb;
-- GO

/* ---------- 1) Authors (SuperAdmin / Admin / Author users) ---------- */
IF OBJECT_ID(N'dbo.Authors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Authors (
        Id            int           NOT NULL IDENTITY(1,1),
        Name          nvarchar(100) NOT NULL,
        Code          nvarchar(100) NOT NULL,
        Password      nvarchar(max) NOT NULL,           -- BCrypt hash
        Role          nvarchar(max) NOT NULL,           -- SuperAdmin | Admin | Author
        ContractStart datetime2     NULL,
        ContractEnd   datetime2     NULL,
        BasicFees     decimal(18,2) NOT NULL CONSTRAINT DF_Authors_BasicFees DEFAULT (0),
        IsDeleted     bit           NOT NULL CONSTRAINT DF_Authors_IsDeleted DEFAULT (0),
        CreatedAt     datetime2     NOT NULL,
        UpdatedAt     datetime2     NOT NULL,
        CONSTRAINT PK_Authors PRIMARY KEY (Id)
    );
END
GO

/* ---------- 2) Sales ---------- */
IF OBJECT_ID(N'dbo.Sales', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sales (
        Id            int           NOT NULL IDENTITY(1,1),
        BookTitle     nvarchar(200) NOT NULL,
        AuthorId      int           NULL,
        SalePrice     decimal(18,2) NOT NULL,
        BasicExpenses decimal(18,2) NOT NULL,
        TotalAmount   decimal(18,2) NOT NULL,           -- max(0, price*qty - expenses*qty), calculated by the app
        Quantity      int           NOT NULL,
        StoreLocation nvarchar(max) NOT NULL,
        SaleDate      datetime2     NOT NULL CONSTRAINT DF_Sales_SaleDate DEFAULT (GETUTCDATE()),
        CreatedAt     datetime2     NOT NULL,
        UpdatedAt     datetime2     NOT NULL,
        CONSTRAINT PK_Sales PRIMARY KEY (Id),
        CONSTRAINT FK_Sales_Authors_AuthorId FOREIGN KEY (AuthorId) REFERENCES dbo.Authors (Id) ON DELETE NO ACTION
    );
END
GO

/* ---------- 3) Operations ---------- */
IF OBJECT_ID(N'dbo.Operations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Operations (
        Id            int           NOT NULL IDENTITY(1,1),
        BookTitle     nvarchar(255) NULL,
        AuthorId      int           NULL,
        OperationName nvarchar(255) NOT NULL,
        ExpenseAmount decimal(18,2) NOT NULL,
        Quantity      int           NOT NULL,
        TotalAmount   decimal(18,2) NOT NULL,           -- ExpenseAmount * Quantity, calculated by the app
        OperationDate datetime2     NOT NULL CONSTRAINT DF_Operations_OperationDate DEFAULT (GETUTCDATE()),
        CreatedAt     datetime2     NOT NULL,
        UpdatedAt     datetime2     NOT NULL,
        CONSTRAINT PK_Operations PRIMARY KEY (Id),
        CONSTRAINT FK_Operations_Authors_AuthorId FOREIGN KEY (AuthorId) REFERENCES dbo.Authors (Id) ON DELETE NO ACTION
    );
END
GO

/* ---------- 4) Indexes ----------
   EXEC(...) keeps each CREATE INDEX out of the batch compile step, so the script
   does not fail with "index already exists" when the index is already there. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Authors_Code' AND object_id = OBJECT_ID(N'dbo.Authors'))
    EXEC (N'CREATE UNIQUE INDEX IX_Authors_Code ON dbo.Authors (Code);');
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sales_AuthorId' AND object_id = OBJECT_ID(N'dbo.Sales'))
    EXEC (N'CREATE INDEX IX_Sales_AuthorId ON dbo.Sales (AuthorId);');
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Operations_AuthorId' AND object_id = OBJECT_ID(N'dbo.Operations'))
    EXEC (N'CREATE INDEX IX_Operations_AuthorId ON dbo.Operations (AuthorId);');
GO

/* ---------- 5) EF Core migrations history ----------
   Lets the application recognise that the schema already exists
   (otherwise Database.Migrate() would try to create the tables again). */
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory (
        MigrationId    nvarchar(150) NOT NULL,
        ProductVersion nvarchar(32)  NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260322222609_InitialCreate')
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES (N'20260322222609_InitialCreate', N'10.0.3');
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260404032208_AddOperationsTable')
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES (N'20260404032208_AddOperationsTable', N'10.0.3');
GO
