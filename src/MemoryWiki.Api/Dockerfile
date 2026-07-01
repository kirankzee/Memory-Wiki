# syntax=docker/dockerfile:1
# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore (leverage layer caching with central package management)
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/MemoryWiki.Domain/MemoryWiki.Domain.csproj src/MemoryWiki.Domain/
COPY src/MemoryWiki.Shared/MemoryWiki.Shared.csproj src/MemoryWiki.Shared/
COPY src/MemoryWiki.Contracts/MemoryWiki.Contracts.csproj src/MemoryWiki.Contracts/
COPY src/MemoryWiki.Application/MemoryWiki.Application.csproj src/MemoryWiki.Application/
COPY src/MemoryWiki.Infrastructure/MemoryWiki.Infrastructure.csproj src/MemoryWiki.Infrastructure/
COPY src/MemoryWiki.Api/MemoryWiki.Api.csproj src/MemoryWiki.Api/
RUN dotnet restore src/MemoryWiki.Api/MemoryWiki.Api.csproj

COPY . .
RUN dotnet publish src/MemoryWiki.Api/MemoryWiki.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MemoryWiki.Api.dll"]
