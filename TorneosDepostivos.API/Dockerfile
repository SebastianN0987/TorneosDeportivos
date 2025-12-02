FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TorneosDepostivos.API/TorneosDeportivos.API.csproj", "TorneosDepostivos.API/"]
COPY ["TorneoDeportivo.Modelos/TorneoDeportivo.Modelos.csproj", "TorneoDeportivo.Modelos/"]
RUN dotnet restore "./TorneosDepostivos.API/TorneosDeportivos.API.csproj"
COPY . .
WORKDIR "/src/TorneosDepostivos.API"
RUN dotnet build "./TorneosDeportivos.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TorneosDeportivos.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TorneosDeportivos.API.dll"]
