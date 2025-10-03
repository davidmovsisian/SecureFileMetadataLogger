# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# working directory
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# copy project files
COPY ["Logger/Logger.csproj", "Logger/"]
COPY ["Common/Common.csproj", "Common/"]
# restore projects dependencies
RUN dotnet restore "./Logger/Logger.csproj"
# copy source files
COPY . .
WORKDIR "/src/Logger"
# build project
RUN dotnet build "./Logger.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Logger.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Deployment and run stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Logger.dll"]