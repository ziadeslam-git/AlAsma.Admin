IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322222609_InitialCreate'
)
BEGIN
    CREATE TABLE [Authors] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [Password] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        [ContractStart] datetime2 NULL,
        [ContractEnd] datetime2 NULL,
        [BasicFees] decimal(18,2) NOT NULL DEFAULT 0.0,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Authors] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322222609_InitialCreate'
)
BEGIN
    CREATE TABLE [Sales] (
        [Id] int NOT NULL IDENTITY,
        [BookTitle] nvarchar(200) NOT NULL,
        [AuthorId] int NULL,
        [SalePrice] decimal(18,2) NOT NULL,
        [BasicExpenses] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Quantity] int NOT NULL,
        [StoreLocation] nvarchar(max) NOT NULL,
        [SaleDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Sales] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sales_Authors_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Authors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322222609_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Authors_Code] ON [Authors] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322222609_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Sales_AuthorId] ON [Sales] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322222609_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260322222609_InitialCreate', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260404032208_AddOperationsTable'
)
BEGIN
    CREATE TABLE [Operations] (
        [Id] int NOT NULL IDENTITY,
        [BookTitle] nvarchar(255) NULL,
        [AuthorId] int NULL,
        [OperationName] nvarchar(255) NOT NULL,
        [ExpenseAmount] decimal(18,2) NOT NULL,
        [Quantity] int NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [OperationDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Operations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Operations_Authors_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Authors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260404032208_AddOperationsTable'
)
BEGIN
    CREATE INDEX [IX_Operations_AuthorId] ON [Operations] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260404032208_AddOperationsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260404032208_AddOperationsTable', N'10.0.3');
END;

COMMIT;
GO

