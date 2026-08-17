FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY server/HelpDesk.Api/HelpDesk.Api.csproj server/HelpDesk.Api/
RUN dotnet restore server/HelpDesk.Api/HelpDesk.Api.csproj

COPY server/HelpDesk.Api/ server/HelpDesk.Api/
RUN dotnet publish server/HelpDesk.Api/HelpDesk.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
CMD ["sh", "-c", "dotnet HelpDesk.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
