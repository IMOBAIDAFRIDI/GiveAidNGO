# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/GiveAid.Web/GiveAid.Web.csproj src/GiveAid.Web/
RUN dotnet restore src/GiveAid.Web/GiveAid.Web.csproj
COPY src/ src/
RUN dotnet publish src/GiveAid.Web/GiveAid.Web.csproj -c Release -o /app /p:UseAppHost=false

# Run
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production \
    PORT=10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "GiveAid.Web.dll"]
