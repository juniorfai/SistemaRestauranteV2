# API de Restaurante Master. Se construye desde la raíz del repositorio:
#   docker build -f SistemaRestaurante.API/Dockerfile -t restaurante-api .

# ---------- Compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Primero solo los proyectos, para aprovechar la caché de la restauración de paquetes.
COPY SistemaRestaurante.Domain/SistemaRestaurante.Domain.csproj SistemaRestaurante.Domain/
COPY SistemaRestaurante.DataAccess/SistemaRestaurante.DataAccess.csproj SistemaRestaurante.DataAccess/
COPY SistemaRestaurante.API/SistemaRestaurante.API.csproj SistemaRestaurante.API/
RUN dotnet restore SistemaRestaurante.API/SistemaRestaurante.API.csproj

COPY SistemaRestaurante.Domain/ SistemaRestaurante.Domain/
COPY SistemaRestaurante.DataAccess/ SistemaRestaurante.DataAccess/
COPY SistemaRestaurante.API/ SistemaRestaurante.API/
RUN dotnet publish SistemaRestaurante.API/SistemaRestaurante.API.csproj \
    --configuration Release --no-restore --output /app /p:UseAppHost=false

# ---------- Ejecución ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# curl solo para la comprobación de salud.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

# Usuario sin privilegios que traen las imágenes de .NET 8.
USER $APP_UID
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -fsS http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "SistemaRestaurante.API.dll"]
