FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TelemetryIngestionService.Api/TelemetryIngestionService.Api.csproj", "TelemetryIngestionService.Api/"]
COPY ["TelemetryIngestionService.Infrastructure/TelemetryIngestionService.Infrastructure.csproj", "TelemetryIngestionService.Infrastructure/"]
COPY ["TelemetryIngestionService.Domain/TelemetryIngestionService.Domain.csproj", "TelemetryIngestionService.Domain/"]
RUN dotnet restore "TelemetryIngestionService.Api/TelemetryIngestionService.Api.csproj"
COPY . .
WORKDIR "/src/TelemetryIngestionService.Api"
RUN dotnet build "TelemetryIngestionService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TelemetryIngestionService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TelemetryIngestionService.Api.dll"]
