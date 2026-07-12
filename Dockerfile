FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AIFormationPlatform.slnx .
COPY src/AIFormationPlatform.Web/*.csproj src/AIFormationPlatform.Web/
COPY src/AIFormationPlatform.Tests/*.csproj src/AIFormationPlatform.Tests/
RUN dotnet restore

COPY src/ src/
RUN dotnet publish src/AIFormationPlatform.Web/*.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AIFormationPlatform.Web.dll"]

