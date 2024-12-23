FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CaseRelayAPI.csproj", "."]
COPY [".env", "."]
COPY ["appsettings*.json", "."]
RUN dotnet restore "CaseRelayAPI.csproj"
COPY . .
RUN dotnet build "CaseRelayAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CaseRelayAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build .env .
COPY --from=build appsettings*.json .
ENTRYPOINT ["dotnet", "CaseRelayAPI.dll"]
