# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this folder is

University workshop TI.2.2 (ESPE, NRC 36883): **APIPaises** in .NET + SQL Server. It is a port of TI.2.1 (`../TI.2.1.-PAO202651_JAVA_MYSQL_NRC36883`, Java + MySQL): same REST contract, same messages, same screens per client. Only the technology changes. Code, identifiers, DB columns and user-facing messages are in Spanish. Not a git repo. Do not commit or push.

Decisions taken with the user on 2026-11-25:
- Shape: one REST server plus four clients (console, WPF, ASP.NET Core MVC web, .NET MAUI Android), like TI.2.1. Not the single MVC app of the assignment.
- Framework: `net10.0` (the machine only has the .NET 10 SDK), not the assignment's .NET 8.
- Scope: identical to TI.2.1. No `SubRegion`, `Area` or Universidades, although the assignment mentions them.
- Data access: EF Core (`AppDbContext`, migration `InitialCreate`) behind DAO classes that keep the Java names.

When behaviour or text must change, mirror it in TI.2.1 unless the user says otherwise; the two TIs are meant to be equivalent.

## Projects and commands

| Folder | Project | Kind | Tests |
|---|---|---|---|
| `03. BDD` | `docker-compose.yml`, `APIPaisesDB.sql` | SQL Server 2025 on `localhost,1433`, `sa`/`Root1234!` | — |
| `04. SERVIDOR` | `WS_APIPAISES_DOTNET_REST` | ASP.NET Core, `http://*:5090/api` | `dotnet test pruebas` (60, the DAO ones need the DB) and `bash pruebas/pruebas_api.sh` |
| `04. CLICON` | `CONSOLE_APIPAISES_DOTNET_REST` | console | `dotnet run --project pruebas` (22) |
| `04. CLIESC` | `ESC_APIPAISES_DOTNET_REST` | WPF, `net10.0-windows` | `dotnet test pruebas` (31) |
| `04. CLIWEB` | `WEB_APIPAISES_DOTNET_REST` | ASP.NET Core MVC, `http://*:5091/` | `dotnet test pruebas` (17) |
| `04. CLIMOV` | `MOV_APIPAISES_DOTNET_REST` | .NET MAUI, `net10.0-android` | `dotnet run --project pruebas` (18) |

Each test project lives in `pruebas/` inside its client (course convention) and is excluded from the app build. Console and MAUI tests are dependency-free console apps that link the app's non-UI `.cs` files; the others are xUnit.

- **Database:** `cd "03. BDD" && docker compose up -d`. The `init` service creates `APIPaisesDB` with collation `Latin1_General_100_CI_AI` and runs `APIPaisesDB.sql`. `docker compose down -v` resets it. TI.1.2/TI.1.4 (EurekaBank .NET) use the same port 1433: run one at a time.
- **Schema changes:** edit `Data/AppDbContext.cs`, then `dotnet ef migrations add <Nombre>` and regenerate the script with `dotnet ef migrations script --idempotent -o "../../03. BDD/APIPaisesDB.sql"`, putting the header comment back. `dotnet-ef` 10.0.15 is installed globally. Seed data (6 regions, users `admin`/`admin123` and `msosa`/`msosa`) is `HasData` in the context, so the script and `dotnet ef database update` both load it.
- **Server:** `dotnet run` from the server folder (port 5090 from `appsettings.json` `Urls`). It must listen on `http://*:5090`: on Windows `localhost` tries `::1` first, and with an IPv4-only bind every client connection waits about 2 s.
- **REST Countries v5 API key:** copy `appsettings.Local.example.json` to `appsettings.Local.json` (not versioned) or set `RestCountries__ApiKey`. Without a key every call to the external API answers 502, as in TI.2.1.
- **End-to-end tests:** `bash pruebas/pruebas_api.sh` (30 cases without a key). With a REST Countries v5 (real or fake) use `API_EXTERNA=si` (39 cases). That run leaves Alemania and the imported countries saved, so a second run fails "Registrar Alemania" with 409 until Alemania is deleted; TI.2.1's script behaves the same.
- **Console:** `dotnet run -- [url]`; default URL in `cliente.properties` (`servidor.url`) or env `SERVIDOR_URL`. Script it with `printf '%s\n' 1 admin admin123 ... | CONSOLE_APIPAISES_DOTNET_REST.exe`.
- **WPF:** server address in `cliente.properties` (`ws.host`, `ws.port`, `ws.context`) next to the exe, or env `CLIENTE_CONFIG`. Palette and sizes copy TI.2.1's Swing `Estilos` (Arial, `#1F4E79`, login 640×480, main 1100×760). It can be driven with UI Automation from PowerShell for screenshots.
- **Web:** server address in `appsettings.json` section `ServidorRest`. CSS and `procesando.js` are copied verbatim from TI.2.1's JSP client. CSRF is a ported synchronizer-token filter (`Filtros/CsrfFilter`, field `_csrf`), not ASP.NET antiforgery. Views are in `Views/Vista`; `_ViewImports` must register the tag helpers or `<partial>` renders literally.
- **MAUI:** server URL in `Config/RestConfig.cs` (`http://10.0.2.2:5090/api/` for the emulator). The project base (platforms, styles, `CampoTexto`, `BotonCarga`, `AvisoError`, `Icono`) was copied from the EurekaBank MAUI client and renamed (`Eureka*` keys → `Ap*`). Build from **PowerShell** through the junction `C:\movpaises22` (Windows 260-char path limit): `dotnet build C:\movpaises22\MOV_APIPAISES_DOTNET_REST.csproj -f net10.0-android -c Release`, then `adb install -r` the `-Signed.apk`. The Debug APK uses Fast Deployment and needs `-t:Install`.
  - The user's AVD `pixel_7_-_api_36_0` is almost full (Android refuses installs). Use the AVD `apipaises_ti22` created for this project (8 GB data): `C:\Android\emulator\emulator.exe -avd apipaises_ti22 -no-window`.
  - In MAUI on Android, a translucent full-screen layer makes the content under it render black. The bottom sheet (`Componentes/Hoja`) therefore does not dim the background.

## Server architecture

Same layering as TI.2.1, ported to ASP.NET Core:
- `Controllers` (`LoginController`, `PaisesController`, `ReportesController`, base `ControladorBase` that maps `ApiExternaException` to 502/404 and DB errors to 500) → `DAO` (`UsuarioDAO`, `RegionDAO`, `PaisDAO` over `Data/AppDbContext`) and `Services` (`ConsumoAPIPaises`, `TokenService`, `HashUtil`).
- `Filtros/TokenFilter`: global MVC filter; only actions marked `[SinToken]` (login) skip it. It runs after routing, so unknown routes answer 404 and wrong methods 405 without a token, like JAX-RS.
- `Vista/JsonResponseView` builds `{estado, mensaje, datos}`; `Vista/ErrorInesperadoMiddleware` formats 404/405/500 and other bare status codes. Invalid JSON → 400 «Los datos enviados no son válidos».
- `ConsumoAPIPaises` is the only class that knows REST Countries v5 and parses it with Newtonsoft.Json (the assignment asks for it). Contract and messages: `../TI.2.1.-PAO202651_JAVA_MYSQL_NRC36883/05. DOCUMENTACION/PLAN_DESARROLLO.md` §3 and `Models/Mensajes.cs`.

## Documents

- `01. UML/01. ERS/ERS_DOTNET_SQLSERVER_APIPAISES_SOSA.docx` and `01. UML/02. ECUD/ECUD_DOTNET_SQLSERVER_APIPAISES_SOSA.docx` were adapted from the TI.2.1 copies on 2026-11-25 with Word COM find/replace (formatting kept, indexes updated): .NET 10/ASP.NET Core/EF Core/SQL Server/Visual Studio instead of Java/Payara/MySQL/NetBeans, REST Countries v5 instead of v3.1, Universidades removed from scope, ECUD2 token without expiry. Word COM works on this machine (`New-Object -ComObject Word.Application`); PowerShell scripts must not contain curly quotes inside string literals.
- PowerDesigner files (`*.oom`, `*.prj`, `*.sws`) are tool-generated: never hand-edit them. They still say Java (object language) and `MySQL_APIPaisesDB`; see `05. DOCUMENTACION/PENDIENTES_DIAGRAMAS.md`.
- `02. MER/MER_APIPAISES.md`: ER model matching the migration.
