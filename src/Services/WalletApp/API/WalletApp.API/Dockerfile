# Use the official .NET 9 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Services/Wallet/API/Wallet.API/Wallet.API.csproj", "src/Services/Wallet/API/Wallet.API/"]
COPY ["src/Services/Wallet/Core/Application/Wallet.Application.csproj", "src/Services/Wallet/Core/Application/"]
COPY ["src/Services/Wallet/Infrastructure/Wallet.Infrastructure.csproj", "src/Services/Wallet/Infrastructure/"]
COPY ["src/Services/Wallet/Core/Domain/Wallet.Domain.csproj", "src/Services/Wallet/Core/Domain/"]
COPY ["src/BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "src/BuildingBlocks/BuildingBlocks/"]
COPY ["src/BuildingBlocks.Messaging/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "src/BuildingBlocks.Messaging/BuildingBlocks.Messaging/"]

RUN dotnet restore "src/Services/Wallet/API/Wallet.API/Wallet.API.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/src/Services/Wallet/API/Wallet.API"
RUN dotnet build "Wallet.API.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "Wallet.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "Wallet.API.dll"]