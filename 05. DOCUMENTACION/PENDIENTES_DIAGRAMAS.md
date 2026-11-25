# Pendientes en los diagramas de PowerDesigner (TI.2.2)

El ERS y el ECUD ya describen la versión .NET con SQL Server. Los diagramas de `01. UML/03. DIAGRAMAS` y el espacio de trabajo `DOTNET_SQLSERVER_APIPAISES_SOSA.sws` siguen siendo los de TI.2.1. Se corrigen en PowerDesigner, no editando los archivos a mano.

| Archivo | Qué cambiar |
|---|---|
| Todos los `.oom` | El lenguaje objeto del modelo es Java. Cambiarlo a C# (Model > Properties > Object Language) |
| `03_SECUENCIA/Iniciar_Sesion.oom` | La línea de vida `MySQL_APIPaisesDB` pasa a `SQLServer_APIPaisesDB` |
| `04_CLASES/Servidor_APIPaises.oom` | Agregar `AppDbContext` (EF Core), `Configuracion` (lee `appsettings.json`), `TokenFilter` como filtro de MVC, `JsonResponseView` y `ErrorInesperadoMiddleware`. Los DAO conservan sus nombres y métodos (con sufijo `Async`) |
| `05_ARQUITECTURA/Arquitectura_MVC_APIPaises.oom` | Servidor ASP.NET Core (Kestrel) en lugar de Payara; SQL Server 2025 en Docker; REST Countries v5 con API key; clientes de consola, WPF, ASP.NET Core MVC y .NET MAUI |
| `DOTNET_SQLSERVER_APIPAISES_SOSA.sws` | El nombre interno del espacio de trabajo es `JAVA_MYSQL_APIPAISES_SOSA`; renombrarlo desde PowerDesigner |

Los demás puntos de `../TI.2.1.-PAO202651_JAVA_MYSQL_NRC36883/05. DOCUMENTACION/CORRECCIONES_ERS_ECUD.md` sobre los diagramas (importar todos, sin Universidades) también aplican aquí.
