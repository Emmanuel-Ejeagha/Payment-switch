# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/Settlement/Settlement.API/Settlement.API.csproj", "src/Services/Settlement/Settlement.API/"]
COPY ["src/Services/Settlement/Settlement.Infrastructure/Settlement.Infrastructure.csproj", "src/Services/Settlement/Settlement.Infrastructure/"]
COPY ["src/Services/Settlement/Settlement.Application/Settlement.Application.csproj", "src/Services/Settlement/Settlement.Application/"]
COPY ["src/Services/Settlement/Settlement.Domain/Settlement.Domain.csproj", "src/Services/Settlement/Settlement.Domain/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Shared/BuildingBlocks.Shared.csproj", "src/BuildingBlocks/BuildingBlocks.Shared/"]
RUN dotnet restore "./src/Services/Settlement/Settlement.API/Settlement.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Settlement/Settlement.API"
RUN dotnet build "./Settlement.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Settlement.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Settlement.API.dll"]