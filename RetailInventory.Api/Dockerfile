# -------------------------
# Stage 1 - Build
# -------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (better caching)
COPY RetailInventory.slnx ./
COPY RetailInventory.Api/*.csproj RetailInventory.Api/
COPY RetailInventory.Tests/*.csproj RetailInventory.Tests/

# Restore dependencies
RUN dotnet restore RetailInventory.Api/RetailInventory.Api.csproj

# Copy remaining source
COPY . .

# Publish application
WORKDIR /src/RetailInventory.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false


# -------------------------
# Stage 2 - Runtime
# -------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install kerberos dependency required by Npgsql
RUN apt-get update \
    && apt-get install -y --no-install-recommends libkrb5-3 \
    && rm -rf /var/lib/apt/lists/*

# Copy compiled app
COPY --from=build /app/publish .

# Configure ASP.NET
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RetailInventory.Api.dll"]