# ============================================
# 📦 Etapa 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar dependencias
COPY RealEstate.API.csproj .
RUN dotnet restore

# Copiar el resto del código fuente
COPY . .

# Compilar en modo Release
RUN dotnet publish -c Release -o /app/publish

# ============================================
# 🚀 Etapa 2: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar binarios del build
COPY --from=build /app/publish .

# Puerto expuesto (el que usas en launchSettings.json o en Program.cs)
EXPOSE 5235

# Cargar variables del entorno si existen
ENV ASPNETCORE_URLS=http://+:5235
ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "RealEstate.API.dll"]
