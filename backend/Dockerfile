# ─── Build Stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY Accounting.sln .
COPY src/Accounting.Core/Accounting.Core.csproj src/Accounting.Core/
COPY src/Accounting.Application/Accounting.Application.csproj src/Accounting.Application/
COPY src/Accounting.Infrastructure/Accounting.Infrastructure.csproj src/Accounting.Infrastructure/
COPY src/Accounting.API/Accounting.API.csproj src/Accounting.API/

RUN dotnet restore src/Accounting.API/Accounting.API.csproj

# Copy all source
COPY src/ src/

# Build and publish
RUN dotnet publish src/Accounting.API/Accounting.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ─── Runtime Stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

# EXPOSE is documentation only; Render assigns PORT dynamically.
# Do NOT set ASPNETCORE_URLS here — it would override the PORT env var
# binding in Program.cs and cause the health check to fail on Render.
EXPOSE 8080

ENTRYPOINT ["dotnet", "Accounting.API.dll"]

