FROM nginx:alpine AS nginxbase

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["DotNet.BlazorWasmApp/DotNet.BlazorWasmApp.csproj", "DotNet.BlazorWasmApp/"]
COPY ["DotNet.Shared/DotNet.Shared.csproj", "DotNet.Shared/"]

RUN dotnet restore "DotNet.Shared/DotNet.Shared.csproj"
RUN dotnet restore "DotNet.BlazorWasmApp/DotNet.BlazorWasmApp.csproj"
COPY . .

WORKDIR "/src/DotNet.BlazorWasmApp"
RUN dotnet build "DotNet.BlazorWasmApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DotNet.BlazorWasmApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM  nginxbase AS final
WORKDIR /usr/share/nginx/html

RUN apk add jq
COPY ["DotNet.BlazorWasmApp/script.sh", "/docker-entrypoint.d/40-script.sh"]

COPY --from=publish /app/publish/wwwroot .
COPY  DotNet.BlazorWasmApp/nginx.conf /etc/nginx/nginx.conf