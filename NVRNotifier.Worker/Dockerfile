#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NVRNotifier.Worker/NVRNotifier.Worker.csproj", "NVRNotifier.Worker/"]
RUN dotnet restore "./NVRNotifier.Worker/NVRNotifier.Worker.csproj"
COPY . .
WORKDIR "/src/NVRNotifier.Worker"
RUN dotnet build "./NVRNotifier.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NVRNotifier.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NVRNotifier.Worker.dll"]