# Property Intelligence Platform

.NET 10 modular monolith with a React/Vite frontend, PostgreSQL, and PostGIS.

## Repository layout

```text
src/BuildingBlocks/       Shared backend foundations
src/Modules/              Business modules and EF Core migrations
src/WebApi/               ASP.NET Core host and release Dockerfile
src/WebApp/               React/Vite frontend
tests/Modules/            Existing unit and PostgreSQL integration tests
infrastructure/bootstrap/ One-time GitHub OIDC setup
infrastructure/templates/ AWS CloudFormation stacks
infrastructure/parameters/ Environment-specific non-secret settings
scripts/deployment/       Deployment orchestration and validation
.github/workflows/        CI and manually triggered AWS deployments
docs/                     Product, architecture, and operations guides
```

## Local development

Existing project paths and local configuration are unchanged. From the repository root:

```powershell
dotnet restore PropertyIntelligencePlatform.slnx
dotnet build PropertyIntelligencePlatform.slnx
dotnet test PropertyIntelligencePlatform.slnx
dotnet run --project src/WebApi/PropertyIntelligence.Api
```

Integration tests require a local PostgreSQL/PostGIS server. Set
`ConnectionStrings__WorkflowTestAdmin` and `ConnectionStrings__PropertiesTestAdmin`
to an administrative test connection, or use the existing local password-file setup.
The fixtures create and drop isolated test databases. CI supplies a disposable PostGIS service.

For the frontend:

```powershell
cd src/WebApp/property-intelligence-web
npm ci
npm run dev
```

See the [frontend guide](src/WebApp/property-intelligence-web/README.md).

## AWS deployment

CI builds and tests pull requests and pushes to `main`. AWS deployment workflows
are manual and require environment setup. They do not deploy on ordinary pushes.

Start with the [deployment guide](docs/operations/deployment.md) for AWS prerequisites,
GitHub variables, stack order, database setup, and release behavior.

See [product documentation](docs/README.md) and the
[AWS architecture](docs/architecture/aws-saas-architecture.md).
