FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "."]
COPY ["NuGet.Config", "."]
COPY ["Vanilla.Api/Vanilla.Api.csproj", "Vanilla.Api/"]
COPY ["Vanilla.ServiceDefaults/Vanilla.ServiceDefaults.csproj", "Vanilla.ServiceDefaults/"]
RUN dotnet restore "Vanilla.Api/Vanilla.Api.csproj" --configfile /src/NuGet.Config

COPY . .
RUN dotnet publish "Vanilla.Api/Vanilla.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Vanilla.Api.dll"]
