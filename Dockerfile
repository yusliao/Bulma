FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/BatteryPackingMES.Api/BatteryPackingMES.Api.csproj", "src/BatteryPackingMES.Api/"]
COPY ["src/BatteryPackingMES.Infrastructure/BatteryPackingMES.Infrastructure.csproj", "src/BatteryPackingMES.Infrastructure/"]
COPY ["src/BatteryPackingMES.Core/BatteryPackingMES.Core.csproj", "src/BatteryPackingMES.Core/"]
RUN dotnet restore "src/BatteryPackingMES.Api/BatteryPackingMES.Api.csproj"
COPY . .
WORKDIR "/src/src/BatteryPackingMES.Api"
RUN dotnet build "BatteryPackingMES.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BatteryPackingMES.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BatteryPackingMES.Api.dll"] 