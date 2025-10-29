# ===============================
# Etapa 1: Build
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY RealEstate.API.csproj ./
RUN dotnet restore

# Copiar todo el código
COPY . .

# Publicar SOLO el proyecto API
RUN dotnet publish RealEstate.API.csproj -c Release -o /app/publish

# ===============================
# Etapa 2: Runtime
# ===============================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5235
ENV ASPNETCORE_URLS=http://+:5235
ENTRYPOINT ["dotnet", "RealEstate.API.dll"]
