/*==============================================================*/
/* Base de datos: APIPaisesDB                                   */
/* DBMS:          SQL Server 2019 o superior                    */
/* Sistema:       APIPaises (TI.2.2. Taller APIs públicas en    */
/*                .NET y SQL Server, NRC 36883)                 */
/* Fuente:        ERS §9.1.5, ECUD2-ECUD9, diagrama de clases   */
/*                Servidor_APIPaises                            */
/*==============================================================*/
/* Script GENERADO desde la migración InitialCreate de EF Core: */
/*   cd "04. SERVIDOR/WS_APIPAISES_DOTNET_REST"                 */
/*   dotnet ef migrations script --idempotent                   */
/*       -o "../../03. BDD/APIPaisesDB.sql"                     */
/* (agregue de nuevo esta cabecera). Equivale a ejecutar        */
/* «dotnet ef database update» o «Update-Database».             */
/*                                                              */
/* Crea las tablas Regiones, Paises y Usuarios y carga los      */
/* datos iniciales (HasData de AppDbContext):                   */
/*   - las 6 regiones: América, Europa, Asia, África, Oceanía,  */
/*     Antártida;                                               */
/*   - los usuarios de prueba (usuario / clave):                */
/*       admin / admin123      Administrador                    */
/*       msosa / msosa         Mateo Sosa                       */
/*     con la clave en SHA-256 hexadecimal (64 caracteres).     */
/* Los países no se cargan: se obtienen de la API REST          */
/* Countries con «Importar» (ECUD2).                            */
/*                                                              */
/* Collation Latin1_General_100_CI_AI: no distingue mayúsculas  */
/* ni tildes, así que la búsqueda «quito» encuentra «Quito» y   */
/* el nombre del país es único aunque cambie la tilde (ECUD5).  */
/* Es idempotente: se puede ejecutar varias veces.              */
/*==============================================================*/

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
    WHERE [MigrationId] = N'20261125051330_InitialCreate'
)
BEGIN
    CREATE TABLE [Regiones] (
        [IdRegion] int NOT NULL IDENTITY,
        [Nombre] nvarchar(20) COLLATE Latin1_General_100_CI_AI NOT NULL,
        CONSTRAINT [PK_REGIONES] PRIMARY KEY ([IdRegion])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125051330_InitialCreate'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [IdUsuario] int NOT NULL IDENTITY,
        [Usuario] nvarchar(50) COLLATE Latin1_General_100_CI_AI NOT NULL,
        [ClaveHash] char(64) NOT NULL,
        [Nombre] nvarchar(100) COLLATE Latin1_General_100_CI_AI NOT NULL,
        CONSTRAINT [PK_USUARIOS] PRIMARY KEY ([IdUsuario])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125051330_InitialCreate'
)
BEGIN
    CREATE TABLE [Paises] (
        [Nombre] nvarchar(100) COLLATE Latin1_General_100_CI_AI NOT NULL,
        [Capital] nvarchar(100) COLLATE Latin1_General_100_CI_AI NOT NULL,
        [IdRegion] int NOT NULL,
        [Poblacion] bigint NOT NULL,
        [Moneda] nvarchar(100) COLLATE Latin1_General_100_CI_AI NOT NULL,
        CONSTRAINT [PK_PAISES] PRIMARY KEY ([Nombre]),
        CONSTRAINT [CK_PAISES_POBLACION] CHECK ([Poblacion] >= 0),
        CONSTRAINT [FK_PAISES_REGIONES] FOREIGN KEY ([IdRegion]) REFERENCES [Regiones] ([IdRegion]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125051330_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdRegion', N'Nombre') AND [object_id] = OBJECT_ID(N'[Regiones]'))
        SET IDENTITY_INSERT [Regiones] ON;
    EXEC(N'INSERT INTO [Regiones] ([IdRegion], [Nombre])
    VALUES (1, N''América''),
    (2, N''Europa''),
    (3, N''Asia''),
    (4, N''África''),
    (5, N''Oceanía''),
    (6, N''Antártida'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdRegion', N'Nombre') AND [object_id] = OBJECT_ID(N'[Regiones]'))
        SET IDENTITY_INSERT [Regiones] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125051330_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdUsuario', N'ClaveHash', N'Nombre', N'Usuario') AND [object_id] = OBJECT_ID(N'[Usuarios]'))
        SET IDENTITY_INSERT [Usuarios] ON;
    EXEC(N'INSERT INTO [Usuarios] ([IdUsuario], [ClaveHash], [Nombre], [Usuario])
    VALUES (1, ''240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9'', N''Administrador'', N''admin''),
    (2, ''cb84af049da6d367b89c4565c747db7a50054d15226dd353f36dc6c26bb3fb9e'', N''Mateo Sosa'', N''msosa'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdUsuario', N'ClaveHash', N'Nombre', N'Usuario') AND [object_id] = OBJECT_ID(N'[Usuarios]'))
        SET IDENTITY_INSERT [Usuarios] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125051330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PAISES_POBLACION] ON [Paises] ([Poblacion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125051330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PAISES_REGION] ON [Paises] ([IdRegion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125051330_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [AK_REGIONES_NOMBRE] ON [Regiones] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125051330_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [AK_USUARIOS_USUARIO] ON [Usuarios] ([Usuario]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125051330_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261125051330_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

