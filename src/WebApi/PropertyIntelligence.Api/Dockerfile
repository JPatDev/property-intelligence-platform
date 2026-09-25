# Build from the repository root so module project references resolve unchanged.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY dotnet-tools.json ./dotnet-tools.json
COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Modules/ src/Modules/
COPY src/WebApi/ src/WebApi/
RUN dotnet restore src/WebApi/PropertyIntelligence.Api/PropertyIntelligence.Api.csproj
RUN dotnet publish src/WebApi/PropertyIntelligence.Api/PropertyIntelligence.Api.csproj \
    --configuration Release --no-restore --output /out/api /p:UseAppHost=false

# Build all existing module migrations into the same versioned release image.
RUN dotnet tool restore --tool-manifest /source/dotnet-tools.json
RUN mkdir -p /out/migrations && \
    for module in Properties Claims Documents Communications Playbooks Workflow; do \
      dotnet tool run dotnet-ef migrations bundle \
        --project "src/Modules/$module/PropertyIntelligence.Modules.$module" \
        --context "${module}DbContext" --configuration Release \
        --target-runtime linux-x64 --output "/out/migrations/$module" || exit 1; \
    done

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /out/api/ ./
COPY --from=build /out/migrations/ ./migrations/
COPY scripts/deployment/container-entrypoint.sh ./container-entrypoint.sh
# Public RDS trust roots for Npgsql SSL Mode=VerifyFull connections.
ADD --chmod=0644 https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem /app/certs/rds-global-bundle.pem
RUN mkdir -p /app/logs && chown -R app:app /app/logs && chmod +x /app/migrations/* /app/container-entrypoint.sh
USER app
EXPOSE 8080
ENTRYPOINT ["/app/container-entrypoint.sh"]
