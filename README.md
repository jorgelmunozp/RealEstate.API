# RealEstate.API

![.NET](https://img.shields.io/badge/.NET-9.0-blue?logo=dotnet)
![React](https://img.shields.io/badge/Frontend-React-blue?logo=react)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-green?logo=mongodb)
![JWT](https://img.shields.io/badge/Auth-JWT-orange?logo=jsonwebtokens)
![License](https://img.shields.io/badge/License-MIT-yellow.svg)

Plataforma para la gestión inmobiliaria compuesta por una API REST (ASP.NET Core) y un Frontend en React, integrada con MongoDB. Incluye JWT, validaciones con FluentValidation, hash seguro con BCrypt.Net, DTOs, mapeadores y middleware de logging/errores.

# 🏡 RealEstate.API

![.NET](https://img.shields.io/badge/.NET-9.0-blue?logo=dotnet)
![React](https://img.shields.io/badge/Frontend-React-blue?logo=react)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-green?logo=mongodb)
![JWT](https://img.shields.io/badge/Auth-JWT-orange?logo=jsonwebtokens)
![License](https://img.shields.io/badge/License-MIT-yellow.svg)

API REST modular para **gestión inmobiliaria**, desarrollada en **ASP.NET Core 9 + MongoDB + React**, con arquitectura limpia, autenticación **JWT**, validación **FluentValidation**, mapeadores DTO/Model, **caché en memoria (IMemoryCache)**, y **hash seguro con BCrypt.Net**.

---

## 🧩 Arquitectura General

```mermaid
flowchart LR
  A[Frontend (React + Redux)] --> B[Axios HTTP Client]
  B --> C[API (ASP.NET Core 9)]
  C --> D[DTO]
  D --> E[Validator (FluentValidation)]
  E --> F[Mapper]
  F --> G[Model]
  G --> H[(MongoDB Atlas)]
  C --> I[IMemoryCache]
  C --> J[JwtService]
  J --> K[Tokens JWT]
```

---

### 🧱 Módulos Principales

| Módulo | Descripción |
|---------|--------------|
| **Auth** | Manejo de login, registro, JWT y refresh tokens. |
| **User** | CRUD de usuarios, validaciones y roles (`user`, `editor`, `admin`). |
| **Owner** | Gestión de dueños de propiedades. |
| **Property** | CRUD de propiedades con filtros, cache y paginación. |
| **PropertyImage** | Administración de imágenes en Base64 o URL. |
| **PropertyTrace** | Historial de transacciones (venta, arriendo, mejora, etc.). |

---

## 🧬 Estructura del Proyecto

```bash
RealEstate.API/
├── Modules/
│   ├── Auth/
│   │   ├── Controller/
│   │   ├── Service/
│   │   ├── Dto/
│   │   ├── Validator/
│   │   └── Interfaces/
│   ├── User/
│   │   ├── Controller/
│   │   ├── Service/
│   │   ├── Dto/
│   │   ├── Mapper/
│   │   ├── Model/
│   │   └── Validator/
│   ├── Owner/
│   ├── Property/
│   ├── PropertyImage/
│   └── PropertyTrace/
├── Infraestructure/
│   ├── Core/
│   └── Logs/
├── Middleware/
├── Program.cs
└── README.md
```

---

## Arquitectura

```mermaid
flowchart LR
  A[Frontend (React)] --> B[Axios]
  B --> C[API (ASP.NET Core)]
  C --> D[DTO]
  D --> E[Validator (FluentValidation)]
  E --> F[Mapper]
  F --> G[Model]
  G --> H[(MongoDB)]
```

- Módulos: Auth, User, Owner, Property, PropertyImage, PropertyTrace.
- Convenciones: DTOs por módulo, validadores, mapeadores y servicios independientes.

---

## Estructura del Proyecto

```bash
RealEstate.API/
├── Modules/
│   ├── Auth/
│   ├── User/
│   ├── Owner/
│   ├── Property/
│   ├── PropertyImage/
│   └── PropertyTrace/
├── Infraestructure/
├── Middleware/
├── Mappings/
├── Program.cs
└── README.md
```

---

## Flujo General de Datos

1. Frontend (React) → Axios
2. DTO → Validator (FluentValidation) → Mapper → Model
3. MongoDB (persistencia)

---

## Tecnologías

| Capa | Tecnología | Descripción |
|------|------------|-------------|
| Frontend | React + Vite | UI moderna y modular |
| Backend | ASP.NET Core 9 | API REST limpia y escalable |
| BD | MongoDB | Almacenamiento NoSQL |
| Auth | JWT + BCrypt.Net | Seguridad y autenticación |
| Validación | FluentValidation | Validación de DTOs |
| Mapper | Manual + normalización | Sincronización camelCase / PascalCase |

---

## Convenciones de API

- Casing JSON: camelCase en toda la API.
- Respuestas de error (wrapper):
  - Estructura: `{ success: false, StatusCode, message, errors: string[], data: null }`.
  - Aplicado a errores globales, ModelState/binding, ValidationException y status sin cuerpo (404/405/415).
- Respuestas de éxito:
  - Property: devuelve wrapper `{ success: true, statusCode, message, data, errors: [] }`.
  - Otros módulos: éxito devuelve DTO plano.

Ejemplo de validación (400):

```json
{
  "success": false,
  "statusCode": 400,
  "message": "Errores de validación",
  "errors": [
    "El nombre de la propiedad es obligatorio",
    "El precio debe ser mayor a 0"
  ],
  "data": null
}
```

---

## Variables de Entorno

```dotenv
# MongoDB
MONGO_CONNECTION=mongodb://localhost:27017
MONGO_DATABASE=RealEstate
MONGO_COLLECTION_PROPERTY=Property
MONGO_COLLECTION_OWNER=Owner
MONGO_COLLECTION_PROPERTYIMAGE=PropertyImage
MONGO_COLLECTION_PROPERTYTRACE=PropertyTrace
MONGO_COLLECTION_USER=User

# JWT
JWT_SECRET=XXXXXXXXXXXXXXXXXXXXXXXX
JWT_ISSUER=RealEstateAPI
JWT_AUDIENCE=UsuariosAPI
JWT_EXPIRY_MINUTES=60

# Frontend (dev)
FRONTEND_URL=http://localhost:3001

# Caché (minutos)
CACHE_TTL_MINUTES=5
```

Notas:
- Si existe `JWT_EXPIRY_MINUTES` se usa; `JWT_EXPIRY` queda como fallback.
- `CACHE_TTL_MINUTES` controla el TTL de caché en memoria por módulo.

---

## Ejecución Local

- Backend
  - `dotnet build`
  - `dotnet run --project RealEstate.API.csproj`
- Frontend
  - `cd RealEstate.App && npm install && npm run dev`

Acceso por defecto:
- API: `http://localhost:5235`
- App: `http://localhost:5173`

---

## Endpoints por Módulo

### Property
- `GET /api/property?name&address&idOwner&minPrice&maxPrice&page=1&limit=6&refresh=false`
- `GET /api/property/{id}`
- `POST /api/property` (JSON) → 201 Created con wrapper
- `PATCH /api/property/{id}`
- `DELETE /api/property/{id}`

Nota: tambifn existe `PUT /api/property/{id}` (protegido, roles `editor,admin`).

Ejemplo de creación (request):

```json
{
  "name": "Casa 123",
  "address": "Calle 1 #2-3",
  "price": 250000,
  "codeInternal": 123,
  "year": 2020,
  "idOwner": "64f0c5d8a1b2c3d4e5f67890"
}
```

Respuesta (201):

```json
{
  "success": true,
  "statusCode": 201,
  "message": "Propiedad creada exitosamente",
  "data": {
    "idProperty": "...",
    "name": "Casa 123",
    "address": "Calle 1 #2-3",
    "price": 250000,
    "codeInternal": 123,
    "year": 2020,
    "idOwner": "64f0c5d8a1b2c3d4e5f67890"
  },
  "errors": []
}
```

### Owner
- `GET /api/owner?name&address&refresh=false`
- `GET /api/owner/{id}`
- `POST /api/owner`
- `PUT /api/owner/{id}`
- `PATCH /api/owner/{id}`
- `DELETE /api/owner/{id}`

Ejemplo de creación (request):

```json
{
  "name": "Jane Roe",
  "address": "Av 123",
  "photo": "<BASE64 opcional>",
  "birthday": "1990-01-01"
}
```

Respuesta (201):

```json
{
  "id": "64f0c5d8a1b2c3d4e5f67890"
}
```

### PropertyImage
- `GET /api/propertyimage?idProperty&enabled&page=1&limit=10&refresh=false`
- `GET /api/propertyimage/{idPropertyImage}`
- `GET /api/propertyimage/property/{propertyId}`
- `POST /api/propertyimage`
- `PATCH /api/propertyimage/{idPropertyImage}`
- `DELETE /api/propertyimage/{idPropertyImage}`

Nota: tambifn existe `PUT /api/propertyimage/{idPropertyImage}` (protegido, roles `editor,admin`).

Ejemplo de creación (request):

```json
{
  "idProperty": "64f0c5d8a1b2c3d4e5f67890",
  "file": "<BASE64>",
  "enabled": true
}
```

Respuesta (201):

```json
{
  "idPropertyImage": "65a1b2c3d4e5f6789064f0c5"
}
```

### PropertyTrace
- `GET /api/propertytrace?idProperty&refresh=false`
- `GET /api/propertytrace/{id}`
- `POST /api/propertytrace` (admite lote)
- `PUT /api/propertytrace/{id}`
- `PATCH /api/propertytrace/{id}`
- `DELETE /api/propertytrace/{id}`

Ejemplo de creación (request) — admite lote:

```json
[
  {
    "idProperty": "64f0c5d8a1b2c3d4e5f67890",
    "dateSale": "2024-01-01",
    "name": "Compra",
    "value": 200000,
    "tax": 10000
  },
  {
    "idProperty": "64f0c5d8a1b2c3d4e5f67890",
    "dateSale": "2024-06-15",
    "name": "Mejora",
    "value": 25000,
    "tax": 1250
  }
]
```

Respuesta (201):

```json
{
  "ids": [
    "65a1b2c3d4e5f6789064f0c5",
    "65a1b2c3d4e5f6789064f0c6"
  ]
}
```

### User
- `GET /api/user?refresh=false` (protegido)
- `GET /api/user/{email}?refresh=false` (protegido)
- `POST /api/user`
- `PUT /api/user/{email}` (protegido)
- `PATCH /api/user/{email}` (protegido)
- `DELETE /api/user/{email}` (protegido)

Ejemplo de actualizacion parcial (PATCH):

```json
{
  "name": "Jane Doe",
  "role": "editor"
}
```

Ejemplo de registro (request):

```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "Secret123!",
  "role": "User"
}
```

Respuesta (201):

```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "role": "User"
}
```

### Auth
- `POST /api/auth/register`
- `POST /api/auth/login`
- En endpoints protegidos usar `Authorization: Bearer <TOKEN>`.

Ejemplo de login (request):

```json
{
  "email": "john@example.com",
  "password": "Secret123!"
}
```

Respuesta (200):

```json
{
  "token": "<JWT>"
}
```

---

## Caché por Módulo

- Implementación: IMemoryCache para listados/consultas frecuentes.
- TTL: `CACHE_TTL_MINUTES` (fallback 5 min).
- refresh: `refresh=true` omite lectura/escritura en caché para obtener datos frescos.

Módulos con caché:
- Property: `GET /api/property` (paginado/filtrado).
- Owner: `GET /api/owner`.
- PropertyImage: `GET /api/propertyimage`.
- PropertyTrace: `GET /api/propertytrace`.
- User: `GET /api/user`, `GET /api/user/{email}` (lecturas no sensibles).

Recomendaciones de TTL:
- Dev/QA: TTL bajo (1) o usa `refresh=true` tras mutaciones.
- Prod: Property/Trace 5–10 min, Owner/Image/User 2–5 min según uso.

---

## Middleware

- Logging: request/response con tiempos y cuerpos (POST/PUT/PATCH).
- Errores: captura global y respuesta unificada (wrapper).
- StatusCodePages: wrapper también para 404/405/415.

---

## Integración Cliente (Axios)

- En éxitos: Property devuelve wrapper; el cliente puede “unwrap” a `data` si prefiere trabajar con DTOs.
- En errores: siempre leer `error.response.data` (formato amigable documentado arriba).

---

## Notas de Seguridad

- CORS "AllowAll" solo para desarrollo. En producción usar `WithOrigins`/dominios permitidos.
- No publicar `.env` con secretos reales. Usar `.env.example` y variables de entorno seguras.

---

## Autorizacion y Roles

Para endpoints protegidos enviar siempre el header `Authorization: Bearer <TOKEN>`.

Roles disponibles: `user`, `editor`, `admin`.

| Operacion | Requisito |
|-----------|-----------|
| GET | Publico, excepto `GET /api/user...` (requiere token) |
| POST | Autenticado (cualquier rol) |
| PUT | Roles `editor,admin` |
| PATCH | Roles `editor,admin` |
| DELETE | Rol `admin` |

Notas:
- El claim de rol usa `ClaimTypes.Role` dentro del JWT.
- Las reglas aplican a todos los modulos salvo donde se indique explicitamente lo contrario.

---

## Pruebas

- Stack sugerido: `NUnit + Moq + FluentAssertions`.
- Cobertura recomendada: CRUD, validación, seguridad (JWT) y cache (hits/misses y refresh).

---

## Password

- `POST /api/password/recover` (anónimo)
  - Body: `{ "email": "user@example.com" }`
  - Envía email con enlace de recuperación si SMTP está configurado. Siempre responde 200 en dev.
- `GET /api/password/reset/{token}` (anónimo)
  - Verifica token y devuelve `{ message, id }` si es válido.
- `PATCH /api/password/update` (anónimo)
  - Body: `{ "token": "<JWT>", "newPassword": "Secret123!" }`
  - Actualiza password del usuario (hash con BCrypt). Token válido 15 min.

SMTP opcional (para envío de correo):

```dotenv
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USER=no-reply@example.com
SMTP_PASS=xxxxxxxxxxxxx
```

---

## Base de Datos (MongoDB)

Conexión y configuración

- La API usa MongoDB vía `MongoDB.Driver` con estas variables:
  - `MONGO_CONNECTION` (p. ej. `mongodb://localhost:27017` o SRV)
  - `MONGO_DATABASE` (p. ej. `RealEstate`)
  - Colecciones por módulo:
    - `MONGO_COLLECTION_PROPERTY`
    - `MONGO_COLLECTION_OWNER`
    - `MONGO_COLLECTION_PROPERTYIMAGE`
    - `MONGO_COLLECTION_PROPERTYTRACE`
    - `MONGO_COLLECTION_USER`
- El arranque registra:
  - `IMongoClient` como singleton con `MONGO_CONNECTION`.
  - `IMongoDatabase` como singleton con `MONGO_DATABASE`.
- Cada servicio resuelve su colección leyendo el nombre desde `IConfiguration`.
  - Ejemplo: `OwnerService` usa `MONGO_COLLECTION_OWNER`; `UserService` usa `MONGO_COLLECTION_USER`.

Modelo de datos

- Identificadores: las entidades usan `string` para `Id`, mapeado a `ObjectId` con atributos BSON.
- Convenciones BSON: se usan `[BsonId]`, `[BsonRepresentation(BsonType.ObjectId)]` y `[BsonElement("...")]` para mantener nombres coherentes entre C# y MongoDB.

Buenas prácticas e índices recomendados

- Unicidad de usuarios: índice único en `Email` de la colección de usuarios.
- Búsquedas comunes: índices en campos filtrables (p. ej., `Property.idOwner`, `Property.price`, `Owner.name`).
- Ejemplo (Mongo Shell):
  - `db.user.createIndex({ Email: 1 }, { unique: true })`
  - `db.property.createIndex({ idOwner: 1 })`
  - `db.property.createIndex({ price: 1 })`

Ejemplos de cadena de conexión

```bash
# Local sin autenticación
MONGO_CONNECTION=mongodb://localhost:27017

# Autenticación usuario/clave (base admin) + opciones de pool/timeout
# SRV (Atlas) con TLS implícito
MONGO_CONNECTION=mongodb+srv://app_user:StrongPass@cluster0.xxxxx.mongodb.net
```

Notas operativas

- Caché en memoria: las lecturas se almacenan con TTL configurable (`CACHE_TTL_MINUTES`).
  - Los servicios invalidan la caché correspondiente en operaciones de escritura.
  - El parámetro `refresh=true` fuerza omitir la caché en lecturas.
- Seguridad: no exponer credenciales en el repositorio; usar variables de entorno o secretos del entorno.
- TLS/SSL: SRV (Atlas) ya usa TLS; para `mongodb://` on-prem, habilitar TLS según tu despliegue.
