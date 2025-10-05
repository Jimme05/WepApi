# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Render sets PORT automatically
ENV ASPNETCORE_URLS=http://+:${PORT}

COPY --from=build /app ./
EXPOSE 8080

ENTRYPOINT ["dotnet", "SimpleAuthBasicApi.dll"]
