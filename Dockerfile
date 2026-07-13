FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/AIFormationPlatform.Core/*.csproj src/AIFormationPlatform.Core/
COPY src/AIFormationPlatform.Infrastructure/*.csproj src/AIFormationPlatform.Infrastructure/
COPY src/AIFormationPlatform.Web/*.csproj src/AIFormationPlatform.Web/
RUN dotnet restore src/AIFormationPlatform.Web/AIFormationPlatform.Web.csproj
COPY src/ src/
RUN dotnet publish src/AIFormationPlatform.Web/AIFormationPlatform.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AIFormationPlatform.Web.dll"]
