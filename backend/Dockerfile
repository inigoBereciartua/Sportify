# Use the ASP.NET runtime image as the base for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Use the SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the .csproj file first to optimize layer caching
COPY backend.csproj ./
RUN dotnet restore

# Copy the remaining source files
COPY . .

# Publish the app to the out directory in release mode
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/out

# Use the runtime image again to run the app
FROM base AS final
WORKDIR /app
COPY --from=build /app/out .

# Specify the entry point to run the app
ENTRYPOINT ["dotnet", "backend.dll"]
