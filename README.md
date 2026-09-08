# Circles

Circles is a communication and identity platform for organizational contexts such
as sports clubs and teams. This is an all-C# solution:

- **`Circles.API`** — an **ASP.NET Core Web API** (FastEndpoints) built on
  **EF Core** and **Azure SQL** (SQL Server), structured as a **modular monolith**.
  It stays in the solution to serve the future native mobile app and third-party
  integrations.
- **`Circles.Web`** — a **Blazor Server** frontend (mobile-first, Scandinavian
  design) that talks to the same Application/Infrastructure layers directly and
  uses **ASP.NET Core cookie authentication** against the existing
  `UserAccount` + BCrypt store.

Both projects share the `Circles.Domain`, `Circles.Application` and
`Circles.Infrastructure` layers, so there is no duplicated model or DTO code.

The first use case is **Danmarks IF**, a fictional Swedish sports club, which the
project seeds with realistic demo data.

---

## The domain model

The single most important idea in Circles is that **User Account, Person and
Membership are three distinct concepts**. Conflating them is the mistake this
model is specifically designed to avoid.

| Concept | Answers the question | Notes |
| --- | --- | --- |
| **Person** | *Who is this human being?* | Identity only. A person can exist with **no** way to log in. |
| **UserAccount** | *How does someone authenticate?* | Email + password hash. Links to a Person via a **nullable** `PersonId`. |
| **Membership** | *What does this human belong to, and in what role?* | Time-based association of a Person with a Circle. |

### Why the separation matters

- **A Person may exist without a UserAccount.** Children are registered as players
  long before they ever have a login. In the seed data, *Alexander Andersson* (10)
  and *Lisa Berg* are people with memberships but **no** user account.
- **Authentication is not identity.** A `UserAccount` is only a set of
  credentials; it points *at* a Person so that, once logged in, the system can
  resolve the human and derive what they may do.
- **Membership is not identity either.** People join and leave circles over time;
  the Person stays the same.

### Entities

- **Person** — `Id, FirstName, LastName, CreatedAt`
- **UserAccount** — `Id, Email, PasswordHash, PersonId (FK, nullable), CreatedAt`
- **Relationship** — `Id, FromPersonId, ToPersonId, Type, ValidFrom, ValidUntil?`
  - explicit, time-based links between people:
    `GuardianOf`, `ChildOf`, `LeaderOf`, `ContactPerson`
- **Organization** — `Id, Name, Slug, CreatedAt` — owns its circles
- **Circle** — `Id, OrganizationId, ParentCircleId?, Name, Slug, Type, CreatedAt`
  - types: `Team`, `Board`, `Officials`, `General`; nestable via `ParentCircleId`
- **Membership** — `Id, PersonId, CircleId, Role, ValidFrom, ValidUntil?`
  - roles: `Player, Guardian, Coach, Leader, Administrator, Member`
- **RolePermission** — maps a role to a `PermissionType`
  - permissions: `ReadPosts, CreateDiscussion, Comment, CreatePoll, Vote,
    CreateTask, AdministerMembers, PublishAnnouncements, ViewMemberList,
    ViewHistoricalInfo`
- **MagicLinkToken** — `Id, Token, UserAccountId, CreatedAt, ExpiresAt, ConsumedAt?`
  - single-use, time-limited token for passwordless login (see
    [Authentication & onboarding](#authentication--onboarding)); transient by
    nature, carries no history

### Time-based and historical by design

Both **memberships** and **relationships** carry `ValidFrom` / `ValidUntil`.

> Memberships are **never deleted** (neither hard- nor soft-deleted). When a
> membership ends, its `ValidUntil` is set and the row is kept, so **historical
> membership remains fully representable**.

### Organizations own their circles

Circles belong to the organization, not to the people currently in them. When a
coach leaves, their membership expires — **the circle (e.g. the team) persists**.

---

## The authorization model

Access is **never** stored as a flag on a user. It is always *derived* from the
domain model:

```
Person → active membership  OR  derived relationship access
       → circle → role → permission → resource
```

`IAuthorizationService` (implemented in `Circles.Application`) answers:

- `CanPersonAccessCircleAsync(personId, circleId)` — true if the person has an
  active direct membership **or** derived access to the circle.
- `GetPersonPermissionsInCircleAsync(personId, circleId)` — the effective set of
  permissions, combining all active memberships and any derived access.
- `GetAccessibleCircleIdsAsync(personId)` — every circle the person can reach.

### Derived guardian access

A **guardian of a child who is an active member of a circle automatically gets
derived, read-only access to that circle** — without any membership of their own.

In the seed data, *Johan Andersson* is the guardian of *Alexander*, who plays in
**P2016**. Johan therefore gains **derived** access to P2016
(`ReadPosts`, `ViewMemberList`) even though he has **no direct membership** there.
His only direct membership is in *Funktionärer*.

The role → permission mapping lives in data (`role_permissions`) rather than in
code, so it is auditable and adjustable. See
`Circles.Infrastructure/Seeding/RolePermissionMap.cs`.

---

## Authentication & onboarding

Authentication is deliberately separate from identity. A **`UserAccount`** only
carries login credentials; the human it authenticates is a **`Person`**, and a
person can exist with no account at all (children do). Signing in resolves the
`UserAccount`, then the linked `Person`, from which memberships and permissions
are derived by the authorization model above.

### How it works

- **JWT bearer tokens.** On successful login the API issues a signed JWT
  (via `FastEndpoints.Security`) carrying the account id, linked person id and
  email as claims. Clients send it as `Authorization: Bearer <token>`.
- **Password hashing.** Passwords are stored as salted **BCrypt** hashes
  (`IPasswordHasher` → `BCryptPasswordHasher`). Plaintext is never persisted.
- **Onboarding.** People (including account-less children) exist first; an
  account is *claimed* for one of them. `POST /api/auth/register` creates a
  `UserAccount` and optionally links it to an existing `Person` (rejecting a
  person who already has an account, or a duplicate email).
- **Passwordless "magic link".** A guardian who never set a password can sign in
  by requesting a single-use, 15-minute link by email
  (`POST /api/auth/magic-link`) and redeeming it
  (`POST /api/auth/magic-link/consume`). Requests never reveal whether an email
  has an account (no enumeration); in **Development** the token is echoed back so
  the flow is testable without an email/SMS provider. Magic link tokens are the
  one transient entity in the model — unlike memberships/relationships they carry
  no history and may be pruned once expired or consumed.

### Securing endpoints

All data endpoints require a valid JWT. Only `/health` and the `/api/auth/*`
endpoints are anonymous. Authorization for *what* a caller can see inside a
circle is still enforced by the backend authorization model — never by hidden UI.

### Configuration

The JWT signing key is read from `Auth:JwtSigningKey` (env var
`Auth__JwtSigningKey`) with a token lifetime of `Auth:TokenLifetimeHours`
(default 12h). A development fallback key keeps the prototype runnable out of the
box; **production must supply its own key.**

### Demo credentials

Every seeded account shares the demo password **`Cirkles123!`** — e.g.
`johan@example.com`, `anna@example.com`, `erik@example.com`,
`maria@example.com`. Alexander and Lisa (children) have **no** account by default.

---

## Project structure (modular monolith)

```
Circles.sln
src/
  Circles.Domain          # Entities, enums, domain interfaces (no dependencies)
  Circles.Infrastructure  # EF Core DbContext, configurations, migrations, seeding
  Circles.Application     # Authorization service, query services, DTOs
  Circles.API             # ASP.NET Core Web API: FastEndpoints, startup, DI
    Features/             # Vertical slices — one folder per feature area
      Auth/              #   Register, Login, RequestMagicLink, ConsumeMagicLink, Me
      Persons/           #   ListPersons, GetPersonCircles, GetPersonPermissions
      Organizations/     #   ListOrganizations, GetOrganizationCircles
      Circles/           #   GetCircleMembers
      Health/            #   Health
    Auth/                # JWT token service, claim constants, claim helpers
  Circles.Web             # Blazor Server frontend (cookie auth, mobile-first UI)
    Components/
      Pages/             #   Login, Hem, Cirkel, Profil, NotFound
      Layout/            #   MainLayout, BottomNav
    Auth/                # Cookie claims builder + ClaimsPrincipal extensions
    Shared/              # Swedish enum labels (Labels.cs)
    wwwroot/app.css      # Scandinavian design system
```

Dependency direction: `API/Web → Application → Infrastructure → Domain`
(Domain depends on nothing). Both `Circles.API` and `Circles.Web` are host
projects that sit on top of the shared Application layer.

### API layer: FastEndpoints (REPR pattern)

The API is built with **[FastEndpoints](https://fast-endpoints.com/)** (v8.3.0) rather
than MVC controllers. Each endpoint is a single self-contained class following the
**REPR** pattern (Request → Endpoint → Response) and lives in its own file under
`Features/<Area>/`, so the request contract, route, and handler for one operation
are always together. Endpoints delegate to `CirclesQueryService` in the
Application layer and reuse the same DTOs as before.

### Technology stack

- **.NET 10.0** (latest)
- **Blazor Server** (interactive server render mode) for the frontend, with
  **ASP.NET Core cookie authentication**
- **EF Core 10.0.0** with **Microsoft.EntityFrameworkCore.SqlServer 10.0.0**
- **FastEndpoints 8.3.0** + **FastEndpoints.Security 8.3.0** (JWT) + **FastEndpoints.Swagger 8.3.0**
- **BCrypt.Net-Next 4.0.3** for password hashing
- **Azure SQL** / **SQL Server 2022+** (connection resiliency enabled via
  `EnableRetryOnFailure`, which also handles the resume delay when an Azure SQL
  **serverless** database wakes from auto-pause)

---

## Running locally

### Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download)
- A reachable **SQL Server** instance. Any of:
  - **Azure SQL Database** (including the serverless tier), or
  - local **SQL Server 2022+**, or
  - the official container: `docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<StrongPassword>" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`

### 1. Create the database

The API creates the schema automatically via migrations on startup, but the
**database itself** must exist first:

```sql
CREATE DATABASE circles;
```

(For Azure SQL, create the database from the portal / CLI. Against a local
instance you can run the statement with `sqlcmd`.)

### 2. Configure the connection string

The application supports both **local SQL Server** and **Azure SQL**.

#### Local SQL Server (for sandbox/CI verification)

The default in `src/Circles.API/appsettings.json` targets a local instance:

```
Server=localhost,1433;Database=circles;User Id=sa;Password=<YourPassword>;TrustServerCertificate=True;Encrypt=True
```

#### Azure SQL (recommended for development and production)

`appsettings.Development.json` contains a placeholder Azure SQL connection string.
Replace `<your-server>` with your actual server name and set it via one of:

**Option A: User Secrets (recommended for local development)**

```bash
dotnet user-secrets init --project src/Circles.API
dotnet user-secrets set "ConnectionStrings:Circles" "Server=tcp:<your-server>.database.windows.net,1433;Initial Catalog=circles-dev;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;" --project src/Circles.API
```

**Option B: Environment variable**

```bash
export ConnectionStrings__Circles="Server=tcp:<your-server>.database.windows.net,1433;Initial Catalog=circles-dev;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;"
```

**Option C: Edit `appsettings.Development.json` directly** (if your connection string
is not sensitive, e.g., using managed identity with no credentials)

> **Note:** Prefer **passwordless authentication** (`Authentication=Active Directory Default`)
> with a managed identity or your Azure CLI/Visual Studio credentials. This avoids
> embedding passwords in connection strings.

### 3. Run

**Backend API** (for the future mobile app / integrations, and Swagger):

```bash
dotnet run --project src/Circles.API
```

On startup the API **applies EF Core migrations** and **seeds the Danmarks IF demo
data** automatically (both are idempotent). Swagger UI is available in the
Development environment at `/swagger`.

**Blazor frontend** (the web app users actually log into):

```bash
dotnet run --project src/Circles.Web
```

`Circles.Web` reads the same `ConnectionStrings:Circles` value and also runs
migrate + seed on startup, so it can be started on its own. It listens on
`https://localhost:7099` (and `http://localhost:5235`). Log in with any of the
[demo credentials](#demo-credentials) below.

> In Rider, the **Full Stack (Backend + Frontend)** compound run configuration
> starts both `Circles.API` and `Circles.Web` together.

> To manage migrations manually you need the EF tools:
> `dotnet tool install --global dotnet-ef --version 10.0.0`
> then e.g.
> `dotnet ef migrations add <Name> --project src/Circles.Infrastructure --startup-project src/Circles.API`

---

## Seed data (Danmarks IF)

**Organization:** Danmarks IF

**Circles (hierarchy):**

```
Danmarks IF (root)
├── P2013        (Team)
├── P2011        (Team)
├── F2016        (Team)
├── Styrelsen    (Board)
└── Funktionärer (General / Officials)
```

**People & accounts:**

| Person | User account | Note |
| --- | --- | --- |
| Johan Andersson | `johan@example.com` | Guardian of Alexander |
| Alexander Andersson | *(none)* | Child, 10 years old |
| Lisa Berg | *(none)* | Child |
| Anna Berg | `anna@example.com` | Guardian of Lisa |
| Erik Svensson | `erik@example.com` | Coach |
| Maria Lindgren | `maria@example.com` | Club administrator |

> Demo accounts use a placeholder password hash — this seed is **not** a real
> credential store.

**Relationships (active):** Johan `GuardianOf` Alexander · Anna `GuardianOf` Lisa

**Memberships (active):**

| Person | Circle | Role |
| --- | --- | --- |
| Alexander | P2016 | Player |
| Lisa | F2016 | Player |
| Erik | P2016 | Coach |
| Maria | Danmarks IF (root) | Administrator |
| Johan | Funktionärer | Member |

Johan has **no direct membership in P2016**; his access there is **derived** from
being Alexander's guardian.

---

## REST API endpoints

| Method & path | Auth | Description |
| --- | --- | --- |
| `GET /health` | — | Health check |
| `POST /api/auth/register` | — | Onboarding: create account, optionally link to a person |
| `POST /api/auth/login` | — | Password login → JWT |
| `POST /api/auth/magic-link` | — | Request a passwordless login link |
| `POST /api/auth/magic-link/consume` | — | Redeem a magic link → JWT |
| `GET /api/auth/me` | 🔒 | The currently authenticated caller |
| `GET /api/persons` | 🔒 | List all persons (with account info) |
| `GET /api/persons/{id}/circles` | 🔒 | Circles the person can access (Direct / Derived) |
| `GET /api/persons/{id}/permissions/{circleId}` | 🔒 | Person's permissions in a circle |
| `GET /api/organizations` | 🔒 | List organizations |
| `GET /api/organizations/{id}/circles` | 🔒 | Circle hierarchy (nested tree) |
| `GET /api/circles/{id}/members` | 🔒 | Active members of a circle |

🔒 = requires `Authorization: Bearer <token>`.

### Quick verification

```bash
# Log in as a demo account (shared password: Cirkles123!)
TOKEN=$(curl -s -X POST localhost:5000/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"johan@example.com","password":"Cirkles123!"}' \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["token"])')

# Who am I?
curl localhost:5000/api/auth/me -H "Authorization: Bearer $TOKEN"

# List people — note Alexander and Lisa have no account
curl localhost:5000/api/persons -H "Authorization: Bearer $TOKEN"

# Johan's circles — Funktionärer (Direct) and P2016 (Derived, as guardian)
curl localhost:5000/api/persons/<johan-id>/circles -H "Authorization: Bearer $TOKEN"

# Passwordless login (Development echoes the token back)
curl -X POST localhost:5000/api/auth/magic-link \
  -H 'Content-Type: application/json' -d '{"email":"anna@example.com"}'
```

Enums are serialized as strings throughout the API for readability.
