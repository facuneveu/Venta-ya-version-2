# VentaYa — Gestor de pedidos para pequeños comercios

Proyecto final de **Aplicaciones Móviles (P3)**: app móvil (.NET MAUI) + API REST (ASP.NET Core) + base de datos relacional (SQL Server).

| Carpeta | Contenido |
|---|---|
| `backend/VentaYa.Api` | API REST en ASP.NET Core (.NET 10) con Entity Framework Core y JWT |
| `backend/VentaYa.Tests` | Tests unitarios y de servicios (xUnit) |
| `mobile/VentaYa.App.Core` | Lógica de la app móvil: cliente de la API, sesión y ViewModels de todas las pantallas (probados con xUnit) |
| `mobile/` | App móvil .NET MAUI: pantallas XAML *(en desarrollo; requiere el workload MAUI)* |
| `docs/` | Plan, documentación, DER, casos de uso y diagrama de navegación |

## Ejecutar el backend en local

Requisitos: .NET SDK 10 y SQL Server LocalDB (viene con Visual Studio).

```bash
cd backend/VentaYa.Api
dotnet run --urls http://localhost:5080
```

- Abrí <http://localhost:5080/swagger> para probar la API.
- La base `VentaYa` se crea sola en LocalDB al iniciar (migraciones automáticas).
- Para registrarte y usar los endpoints protegidos: `POST /api/auth/registro` → copiá el `token` → botón **Authorize** de Swagger.

## Tests

```bash
cd backend
dotnet test
```

Pruebas de la API con Postman (75 requests, 124 aserciones): ver [docs/postman/LEEME.md](docs/postman/LEEME.md).

```bash
npx newman run docs/postman/VentaYa.postman_collection.json -e docs/postman/VentaYa.local.postman_environment.json
```

## Configuración en producción (MonsterASP)

Copiá `appsettings.Production.example.json` como `appsettings.Production.json` (queda fuera de git) y completá la cadena de conexión y una clave JWT propia de 32+ caracteres.
