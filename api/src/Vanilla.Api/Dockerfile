FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["NuGet.Config", "."]
COPY ["api/src/Vanilla.Api/Vanilla.Api.csproj", "api/src/Vanilla.Api/"]
COPY ["api/src/Vanilla.Application/Vanilla.Application.csproj", "api/src/Vanilla.Application/"]
COPY ["api/src/Vanilla.Domain/Vanilla.Domain.csproj", "api/src/Vanilla.Domain/"]
COPY ["api/src/Vanilla.Infrastructure/Vanilla.Infrastructure.csproj", "api/src/Vanilla.Infrastructure/"]

RUN dotnet restore "api/src/Vanilla.Api/Vanilla.Api.csproj" --configfile /src/NuGet.Config

COPY . .

WORKDIR /src/api/src/Vanilla.Api
RUN dotnet publish "Vanilla.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Vanilla.Api.dll"]