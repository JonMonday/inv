# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY InvServer.sln ./
COPY InvServer.Api/InvServer.Api.csproj InvServer.Api/
COPY InvServer.Core/InvServer.Core.csproj InvServer.Core/
COPY InvServer.Infrastructure/InvServer.Infrastructure.csproj InvServer.Infrastructure/
RUN dotnet restore

COPY . .
RUN dotnet publish InvServer.Api/InvServer.Api.csproj -c Release -o /app/publish

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Cloud Run provides PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InvServer.Api.dll"]
