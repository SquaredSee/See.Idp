FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Install curl for the HEALTHCHECK instruction below.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy all project files first so dotnet restore can resolve project references.
COPY ["See.Idp.Web/See.Idp.Web.csproj", "See.Idp.Web/"]
COPY ["See.Idp.Core/See.Idp.Core.csproj", "See.Idp.Core/"]
COPY ["See.Idp.Infrastructure/See.Idp.Infrastructure.csproj", "See.Idp.Infrastructure/"]
RUN dotnet restore "./See.Idp.Web/See.Idp.Web.csproj"
COPY . .
WORKDIR "/src/See.Idp.Web"
RUN dotnet build "./See.Idp.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./See.Idp.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "See.Idp.Web.dll"]
