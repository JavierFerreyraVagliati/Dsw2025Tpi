# Trabajo Práctico Integrador
## Desarrollo de Software
### Backend
## Ramas del repositorio
- `main`: versión estable, de producción.
- `development`: rama de integración. Es la rama por defecto.
- Ambas están protegidas: todo cambio entra por Pull Request con 1 aprobación de otro integrante.
## Introducción
Se desea desarrollar una plataforma de comercio electrónico (E-commerce). 
En esta primera etapa el objetivo es construir el módulo de Órdenes, permitiendo la gestión completa de éstas.

## Visión General del Producto
Del relevamiento preliminar se identificaron los siguientes requisitos:
- Los visitantes pueden consultar los productos sin necesidad de estar registrados o iniciar sesión.
- Para realizar un pedido se requiere el inicio de sesión.
- Una orden, para ser aceptada, debe incluir la información básica del cliente, envío y facturación.
- Antes de registrar la orden se debe verificar la disponibilidad de stock (o existencias) de los productos.
- Si la orden es exitosa hay que actualizar el stock de cada producto.
- Se deben poder consultar órdenes individuales o listar varias con posibilidad de filtrado.
- Será necesario el cambio de estado de una orden a medida que avanza en su ciclo de vida.
- Los administradores solo pueden gestionar los productos (alta, modificación y baja) y actualizar el estado de la orden.
- Los clientes pueden crear y consultar órdenes.

[Documento completo](https://frtutneduar.sharepoint.com/:b:/s/DSW2025/ETueAd4rTe1Gilj_Yfi64RYB5oz9s2dOamxKSfMFPREbiA?e=azZcwg) 

## Alcance para el Primer Parcial
> [!IMPORTANT]
> Del apartado `IMPLEMENTACIÓN` (Pag. 7), completo hasta el punto `6` (inclusive)


### Características de la Solución

## Convención de ramas

| Tipo | Sale de | PR hacia | Ejemplo |
|---|---|---|---|
| feature/ | development | development | feature/dockerfile-api |
| hotfix/ | main | main y luego development | hotfix/swagger-produccion |
| test/ | development | development | test/unit-create-order |
| Pase a producción | — | PR de development a main | — |

- `feature/nombre-funcionalidad`: nuevas funcionalidades o tareas del sprint.
- `hotfix/descripcion`: correcciones urgentes sobre producción.
- `test/tarea-realizada`: pruebas automatizadas y cambios de calidad (Sprint 3).
- El nombre de la rama corresponde a su tarjeta de Trello.
- No se permiten commits directos sobre main ni development.

- Lenguaje: C# 12.0
- Plataforma: .NET 8

## Pruebas automatizadas

La solución tiene una suite de pruebas que sigue la pirámide de testing:

| Nivel | Proyecto | Qué prueba |
|---|---|---|
| Unitarias | `tests/Dsw2025Tpi.UnitTests` | La lógica de los servicios de Application, con el repositorio simulado (Moq). No usan base de datos. |
| Integración | `tests/Dsw2025Tpi.IntegrationTests` | Los endpoints reales de la API (controllers, middlewares, JWT y EF Core) levantada en memoria con `WebApplicationFactory` y SQLite en memoria. |
| End-to-End | `tests/Dsw2025Tpi.IntegrationTests/OrderEndToEndTests.cs` | Un flujo completo de compra encadenando llamadas HTTP. |

### Cómo ejecutarlas

Desde la raíz del repositorio:

```bash
dotnet test
```

Para ver el nombre y el resultado de cada prueba:

```bash
dotnet test --logger "console;verbosity=normal"
```

Para correr un solo proyecto:

```bash
dotnet test tests/Dsw2025Tpi.UnitTests
dotnet test tests/Dsw2025Tpi.IntegrationTests
```

No hace falta SQL Server ni ninguna configuración extra: las pruebas de integración crean su propia base SQLite en memoria.

### Escenarios cubiertos

**Unitarias**
- Creación de productos: alta válida, SKU duplicado, stock o precio inválido.
- Creación de órdenes: total y descuento de stock, stock insuficiente, cliente inexistente.
- Listado de productos: solo activos, paginación, búsqueda por nombre, SKU o código interno.

**Integración**
- `POST /api/products`: 401/403 sin permisos, 201 con persistencia, 409 por SKU duplicado.
- `POST /api/orders`: 401 sin token, 201 con orden y stock persistidos, 400 por stock insuficiente.
- `GET /api/products` y `/api/products/admin`: acceso público, filtros por query string, inactivos solo para admin.

### Infraestructura de integración

`CustomWebApplicationFactory` levanta la API en el entorno `Testing` y reemplaza SQL Server por SQLite en memoria. Ofrece helpers para escribir pruebas nuevas:

- `CreateAnonymousClient()` y `CreateClientWithRole("admin")`: clientes HTTP sin token o con un JWT válido.
- `SeedProductAsync(...)` y `SeedCustomerAsync(...)`: carga de datos de prueba.
- `ExecuteDbContextAsync(...)`: consultas directas a la base para verificar resultados.
- `ResetDatabaseAsync()`: deja la base limpia antes de cada prueba.

Las clases de integración usan `[Collection(IntegrationTestsCollection.Name)]` para compartir una sola API en memoria y ejecutarse de a una.

### Quality Gate en el pipeline

El workflow `.github/workflows/deploy.yml` ejecuta todas las pruebas en cada push y pull request hacia `development` y `main`:

```
Compilar  →  Ejecutar pruebas (Quality Gate)  →  Imagen Docker  →  Deploy en Azure
```

Si falla una sola prueba, el pipeline se detiene: no se construye la imagen ni se despliega.

