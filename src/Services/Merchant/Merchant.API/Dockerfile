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
COPY ["src/Services/Merchant/Merchant.API/Merchant.API.csproj", "src/Services/Merchant/Merchant.API/"]
COPY ["src/Services/Merchant/Merchant.Infrastructure/Merchant.Infrastructure.csproj", "src/Services/Merchant/Merchant.Infrastructure/"]
COPY ["src/Services/Merchant/Merchant.Application/Merchant.Application.csproj", "src/Services/Merchant/Merchant.Application/"]
COPY ["src/Services/Merchant/Merchant.Domain/Merchant.Domain.csproj", "src/Services/Merchant/Merchant.Domain/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Shared/BuildingBlocks.Shared.csproj", "src/BuildingBlocks/BuildingBlocks.Shared/"]
RUN dotnet restore "./src/Services/Merchant/Merchant.API/Merchant.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Merchant/Merchant.API"
RUN dotnet build "./Merchant.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Merchant.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Merchant.API.dll"]