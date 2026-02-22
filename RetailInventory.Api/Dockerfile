# Stage 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY RetailInventory.Api/*.csproj RetailInventory.Api/
COPY RetailInventory.Tests/*.csproj RetailInventory.Tests/
COPY RetailInventory.slnx ./

# Restore
RUN dotnet restore RetailInventory.Api/RetailInventory.Api.csproj

# Copy everything else
COPY . .

# Publish
WORKDIR /src/RetailInventory.Api
RUN dotnet publish -c Release -o /app/publish

# Stage 2 - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RetailInventory.Api.dll"]