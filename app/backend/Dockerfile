FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY COEPulse.API/COEPulse.API.csproj COEPulse.API/
RUN dotnet restore COEPulse.API/COEPulse.API.csproj

COPY COEPulse.API/ COEPulse.API/
RUN dotnet publish COEPulse.API/COEPulse.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "COEPulse.API.dll"]
