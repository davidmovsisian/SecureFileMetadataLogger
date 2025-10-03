# Base runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
# working directory
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# copy project files
COPY ["Watcher/Watcher.csproj", "Watcher/"]
COPY ["Common/Common.csproj", "Common/"]
# restore projects dependencies
RUN dotnet restore "./Watcher/Watcher.csproj"
# copy source files
COPY . .
WORKDIR "/src/Watcher"
# build project
RUN dotnet build "./Watcher.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Watcher.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Deployment and run stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Watcher.dll"]