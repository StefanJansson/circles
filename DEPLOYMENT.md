# Deployment — Circles (Azure App Service)

Circles består av två deploybara appar som körs som separata **Azure App Services (Linux, .NET 10)**:

| App | Projekt | Startkommando (Azure) |
|-----|---------|------------------------|
| `circles-web-dev` | `src/Circles.Web/Circles.Web.csproj` | `dotnet Circles.Web.dll` |
| `circles-api-dev` | `src/Circles.API/Circles.API.csproj` | `dotnet Circles.API.dll` |

Deploy sker automatiskt via GitHub Actions vid push till `main` (workflow-filerna ligger i `.github/workflows/`).

---

## 1. Startup Command (viktigast — löser "Azure default-sidan")

På **Linux App Service** måste startkommandot sättas explicit, annars vet Azure inte
vilken DLL som är entry point och visar sin default-sida.

Sätt i portalen under **App Service → Configuration → General settings → Startup Command**:

- **circles-web-dev:** `dotnet Circles.Web.dll`
- **circles-api-dev:** `dotnet Circles.API.dll`

> Detta krävs eftersom en publish-mapp kan innehålla flera `.dll`-filer. Startkommandot
> pekar ut rätt applikations-DLL. GitHub Actions-workflowen publicerar numera bara rätt
> projekt (`dotnet publish src/Circles.Web/Circles.Web.csproj ...`), men startkommandot
> ska ändå sättas för att vara explicit och robust.

---

## 2. App Settings (miljövariabler) som MÅSTE sättas

Sätt under **App Service → Configuration → Application settings** för **båda** apparna:

| Namn | Värde | Kommentar |
|------|-------|-----------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Väljer `appsettings.Production.json` |
| `ConnectionStrings__Circles` | *(se format nedan)* | Dubbelt understreck = nästlad config-nyckel |

> **Dubbelt understreck (`__`)** är hur Azure/.NET mappar en miljövariabel till en nästlad
> konfigurationsnyckel. `ConnectionStrings__Circles` motsvarar `ConnectionStrings:Circles`
> i `appsettings.json`.

Alternativt kan connection stringen sättas under **Configuration → Connection strings**
(typ: **SQLAzure**) — då exponeras den som `ConnectionStrings__Circles` automatiskt.

Appen läser connection stringen i denna prioritetsordning (se `Program.cs`):

1. `ConnectionStrings:Circles` (dvs. `ConnectionStrings__Circles`)
2. `AZURE_SQL_CONNECTIONSTRING` (Azures standardnamn om du använder deras SQL-integration)
3. En lokal SQL Server-fallback (endast för lokal utveckling)

Tomma/whitespace-värden behandlas som "inte satt".

### API:et behöver dessutom

| Namn | Värde | Kommentar |
|------|-------|-----------|
| `Auth__JwtSigningKey` | *(en hemlig nyckel, minst 32 tecken)* | Krävs i produktion för JWT-signering |

---

## 3. Connection string-format för Azure SQL

### Rekommenderat: passwordless med managed identity

Aktivera **System-assigned managed identity** på App Service och ge den `db_datareader` +
`db_datawriter` (samt rätt att köra migrations, t.ex. `db_ddladmin`) i Azure SQL-databasen.
Ingen hemlighet behöver då lagras:

```
Server=tcp:veumsql.database.windows.net,1433;Initial Catalog=circles-dev;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;
```

### Alternativ: SQL-autentisering (användarnamn/lösenord)

```
Server=tcp:veumsql.database.windows.net,1433;Initial Catalog=circles-dev;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

> Ingen connection string med lösenord ska checkas in i git. `appsettings.Production.json`
> har därför en tom `ConnectionStrings:Circles` — det verkliga värdet sätts i Azure.

---

## 4. Migrering och seed

Båda apparna kör `Database.MigrateAsync()` + `DataSeeder.SeedAsync()` vid uppstart.
Migreringar appliceras alltså automatiskt mot den konfigurerade databasen första gången
en app startar mot den. Managed identity (eller SQL-användaren) behöver därför rättigheter
att ändra schema (skapa tabeller) första gången.

---

## 5. Snabb checklista vid ny deploy

1. Startup Command satt för respektive app (`dotnet Circles.Web.dll` / `dotnet Circles.API.dll`).
2. `ASPNETCORE_ENVIRONMENT=Production` satt.
3. `ConnectionStrings__Circles` satt (eller Connection strings-sektionen).
4. `Auth__JwtSigningKey` satt (endast API:et).
5. Managed identity aktiverad och har DB-rättigheter (om passwordless).
6. Push till `main` → GitHub Actions bygger och deployar.
7. Verifiera: `https://<app>.azurewebsites.net/health` (API) svarar, och webben visar
   inloggningssidan istället för Azure default-sidan.
