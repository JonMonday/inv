# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first (for better caching)
COPY InvServer.Api/InvServer.Api.csproj InvServer.Api/
COPY InvServer.Core/InvServer.Core.csproj InvServer.Core/
COPY InvServer.Infrastructure/InvServer.Infrastructure.csproj InvServer.Infrastructure/

# Restore ONLY the API project (this avoids broken solution refs)
RUN dotnet restore InvServer.Api/InvServer.Api.csproj

# Copy the rest and publish
COPY . .
RUN dotnet publish InvServer.Api/InvServer.Api.csproj -c Release -o /app/publish

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InvServer.Api.dll"]
