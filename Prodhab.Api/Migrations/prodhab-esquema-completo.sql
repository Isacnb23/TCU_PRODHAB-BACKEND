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
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(200) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Rol] nvarchar(50) NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    CREATE TABLE [Expedientes] (
        [Id] int NOT NULL IDENTITY,
        [NumeroExpediente] nvarchar(50) NULL,
        [Entidad] nvarchar(300) NOT NULL,
        [Anio] int NOT NULL,
        [Estado] nvarchar(30) NOT NULL,
        [PasoActual] int NOT NULL,
        [UsuarioId] int NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NOT NULL,
        [FechaEnvio] datetime2 NULL,
        CONSTRAINT [PK_Expedientes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Expedientes_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    CREATE TABLE [DatosFormularios] (
        [Id] int NOT NULL IDENTITY,
        [ExpedienteId] int NOT NULL,
        [Paso] int NOT NULL,
        [DatosJson] nvarchar(max) NOT NULL,
        [Completado] bit NOT NULL,
        [FechaActualizacion] datetime2 NOT NULL,
        CONSTRAINT [PK_DatosFormularios] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DatosFormularios_Expedientes_ExpedienteId] FOREIGN KEY ([ExpedienteId]) REFERENCES [Expedientes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    CREATE TABLE [HistorialExpedientes] (
        [Id] int NOT NULL IDENTITY,
        [ExpedienteId] int NOT NULL,
        [UsuarioId] int NOT NULL,
        [Accion] nvarchar(100) NOT NULL,
        [Detalle] nvarchar(max) NULL,
        [FechaCambio] datetime2 NOT NULL,
        CONSTRAINT [PK_HistorialExpedientes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistorialExpedientes_Expedientes_ExpedienteId] FOREIGN KEY ([ExpedienteId]) REFERENCES [Expedientes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    CREATE TABLE [Subsanaciones] (
        [Id] int NOT NULL IDENTITY,
        [ExpedienteId] int NOT NULL,
        [Paso] int NOT NULL,
        [Campo] nvarchar(200) NOT NULL,
        [TextoJustificacion] nvarchar(max) NULL,
        [ArchivoRuta] nvarchar(max) NULL,
        [ArchivoNombre] nvarchar(300) NULL,
        [ArchivoMimeType] nvarchar(150) NULL,
        [ArchivoExtension] nvarchar(10) NULL,
        [ArchivoTamanoBytes] bigint NULL,
        [ArchivoHash] nvarchar(64) NULL,
        [UsuarioId] int NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaSubsanacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Subsanaciones] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Subsanacion_JustificacionOArchivo] CHECK (([TextoJustificacion] IS NOT NULL AND LEN([TextoJustificacion]) > 0) OR [ArchivoRuta] IS NOT NULL),
        CONSTRAINT [FK_Subsanaciones_Expedientes_ExpedienteId] FOREIGN KEY ([ExpedienteId]) REFERENCES [Expedientes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DatosFormularios_ExpedienteId_Paso] ON [DatosFormularios] ([ExpedienteId], [Paso]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Expedientes_NumeroExpediente] ON [Expedientes] ([NumeroExpediente]) WHERE [NumeroExpediente] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Expedientes_UsuarioId] ON [Expedientes] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HistorialExpedientes_ExpedienteId] ON [HistorialExpedientes] ([ExpedienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Subsanaciones_ExpedienteId] ON [Subsanaciones] ([ExpedienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_Email] ON [Usuarios] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803185029_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803185029_InitialCreate', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818031653_AgregarObservaciones'
)
BEGIN
    CREATE TABLE [Observaciones] (
        [Id] int NOT NULL IDENTITY,
        [ExpedienteId] int NOT NULL,
        [Paso] int NOT NULL,
        [Texto] nvarchar(2000) NOT NULL,
        [UsuarioId] int NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Observaciones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Observaciones_Expedientes_ExpedienteId] FOREIGN KEY ([ExpedienteId]) REFERENCES [Expedientes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818031653_AgregarObservaciones'
)
BEGIN
    CREATE INDEX [IX_Observaciones_ExpedienteId] ON [Observaciones] ([ExpedienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818031653_AgregarObservaciones'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260818031653_AgregarObservaciones', N'10.0.10');
END;

COMMIT;
GO

