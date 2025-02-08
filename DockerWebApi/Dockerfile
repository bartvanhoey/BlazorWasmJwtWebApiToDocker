FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DotNet.JwtWebApi/DotNet.JwtWebApi.csproj", "DotNet.JwtWebApi/"]
COPY ["DotNet.Shared/DotNet.Shared.csproj", "DotNet.Shared/"]
RUN dotnet restore "DotNet.JwtWebApi/DotNet.JwtWebApi.csproj"
COPY . .
WORKDIR "/src/DotNet.JwtWebApi"
RUN dotnet build "DotNet.JwtWebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DotNet.JwtWebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotNet.JwtWebApi.dll"]
