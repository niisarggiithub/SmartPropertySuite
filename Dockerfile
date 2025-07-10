# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore the project
COPY ["SmartPropertySuite/SmartPropertySuite.csproj", "SmartPropertySuite/"]
RUN dotnet restore "SmartPropertySuite/SmartPropertySuite.csproj"

# Copy everything and build
COPY . .
WORKDIR /src/SmartPropertySuite
RUN dotnet publish -c Release -o /app/publish

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 80
EXPOSE 80

# Start the app
ENTRYPOINT ["dotnet", "SmartPropertySuite.dll"]
