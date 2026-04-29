FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

# Remove any fallback package references from previous restores
RUN rm -rf /root/.nuget/packages /root/.dotnet

COPY ["Directory.Packages.props", "."]
COPY ["NuGet.Config", "."]
COPY ["Vanilla.Api/Vanilla.Api.csproj", "Vanilla.Api/"]
COPY ["Vanilla.ServiceDefaults/Vanilla.ServiceDefaults.csproj", "Vanilla.ServiceDefaults/"]

# Clean up any generated files that might have metadata caches
RUN rm -rf Vanilla.Api/bin Vanilla.Api/obj Vanilla.ServiceDefaults/bin Vanilla.ServiceDefaults/obj

RUN dotnet restore "Vanilla.Api/Vanilla.Api.csproj" --configfile /src/NuGet.Config

COPY . .
RUN dotnet publish "Vanilla.Api/Vanilla.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Vanilla.Api.dll"]
