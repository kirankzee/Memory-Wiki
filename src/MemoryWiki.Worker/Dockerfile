# syntax=docker/dockerfile:1
# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/MemoryWiki.Domain/MemoryWiki.Domain.csproj src/MemoryWiki.Domain/
COPY src/MemoryWiki.Shared/MemoryWiki.Shared.csproj src/MemoryWiki.Shared/
COPY src/MemoryWiki.Contracts/MemoryWiki.Contracts.csproj src/MemoryWiki.Contracts/
COPY src/MemoryWiki.Application/MemoryWiki.Application.csproj src/MemoryWiki.Application/
COPY src/MemoryWiki.Infrastructure/MemoryWiki.Infrastructure.csproj src/MemoryWiki.Infrastructure/
COPY src/MemoryWiki.Worker/MemoryWiki.Worker.csproj src/MemoryWiki.Worker/
RUN dotnet restore src/MemoryWiki.Worker/MemoryWiki.Worker.csproj

COPY . .
RUN dotnet publish src/MemoryWiki.Worker/MemoryWiki.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MemoryWiki.Worker.dll"]
