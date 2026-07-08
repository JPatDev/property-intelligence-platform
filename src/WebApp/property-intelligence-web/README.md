# Property Intelligence Web

React + TypeScript frontend for the Property Intelligence Platform.

## Run locally

Start the API from the repository root:

```powershell
dotnet run --project src\WebApi\PropertyIntelligence.Api\PropertyIntelligence.Api.csproj
```

Start the web app:

```powershell
cd src\WebApp\property-intelligence-web
npm install
npm run dev
```

Open:

```text
http://localhost:5173
```

The development API base URL is configured in `.env.development`:

```text
VITE_API_BASE_URL=http://localhost:5120
```

## Verify

```powershell
npm run lint
npm run build
npm audit
```

Use Node 20 LTS or newer for the cleanest frontend tooling support.
