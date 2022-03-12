FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ShadowBot/nuget.config", "ShadowBot/"]
COPY ["ShadowBot/ShadowBot.csproj", "ShadowBot/"]
RUN dotnet restore "ShadowBot/ShadowBot.csproj"
COPY . .
WORKDIR "/src/ShadowBot"
RUN dotnet build "ShadowBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShadowBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShadowBot.dll"]