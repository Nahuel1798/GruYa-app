# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Cache de dependencias
COPY GruYaApi.csproj .
RUN dotnet restore

# Build y publish
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 5082

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "GruYaApi.dll"]
