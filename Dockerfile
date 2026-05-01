# syntax=docker/dockerfile:1.7

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy csproj files first for better layer caching on restore
COPY Core.sln ./
COPY Core.API/API.csproj            Core.API/
COPY Application/Application.csproj Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Domain/Domain.csproj           Domain/
COPY Common/Common.csproj           Common/

RUN dotnet restore Core.API/API.csproj

# Copy the rest and publish
COPY Core.API/        Core.API/
COPY Application/     Application/
COPY Infrastructure/  Infrastructure/
COPY Domain/          Domain/
COPY Common/          Common/

RUN dotnet publish Core.API/API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Non-root user (image already ships an `app` user/group, but recreate explicitly to be safe)
RUN addgroup -S app 2>/dev/null || true && \
    adduser  -S -G app app 2>/dev/null || true

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish ./
RUN chown -R app:app /app

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "API.dll"]
