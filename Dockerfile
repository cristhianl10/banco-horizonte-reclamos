FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY src/BancoHorizonte.Api/BancoHorizonte.Api.csproj src/BancoHorizonte.Api/
RUN dotnet restore src/BancoHorizonte.Api/BancoHorizonte.Api.csproj

COPY src/BancoHorizonte.Api/ src/BancoHorizonte.Api/
RUN dotnet publish src/BancoHorizonte.Api/BancoHorizonte.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "BancoHorizonte.Api.dll"]
