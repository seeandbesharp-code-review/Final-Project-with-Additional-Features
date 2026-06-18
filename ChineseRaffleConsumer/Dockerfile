FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ChineseRaffleConsumer.csproj .
RUN dotnet restore ChineseRaffleConsumer.csproj

COPY . .
RUN dotnet publish ChineseRaffleConsumer.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ChineseRaffleConsumer.dll"]