# Use the official .NET 9 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Services/Order/API/Order.API/Order.API.csproj", "src/Services/Order/API/Order.API/"]
COPY ["src/Services/Order/Core/Application/Order.Application.csproj", "src/Services/Order/Core/Application/"]
COPY ["src/Services/Order/Infrastructure/Order.Infrastructure.csproj", "src/Services/Order/Infrastructure/"]
COPY ["src/Services/Order/Core/Domain/Order.Domain.csproj", "src/Services/Order/Core/Domain/"]
COPY ["src/BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "src/BuildingBlocks/BuildingBlocks/"]
COPY ["src/BuildingBlocks.Messaging/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "src/BuildingBlocks.Messaging/BuildingBlocks.Messaging/"]

RUN dotnet restore "src/Services/Order/API/Order.API/Order.API.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/src/Services/Order/API/Order.API"
RUN dotnet build "Order.API.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "Order.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "Order.API.dll"]