# syntax=docker/dockerfile:1.7

############ build: compile with the full SDK ############
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, using only the files that describe dependencies (cached until they change)
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY [".config/dotnet-tools.json", ".config/"]
COPY ["src/Orders.Api/Orders.Api.csproj", "src/Orders.Api/packages.lock.json", "src/Orders.Api/"]
COPY ["src/Orders.Domain/Orders.Domain.csproj", "src/Orders.Domain/packages.lock.json", "src/Orders.Domain/"]
COPY ["src/Orders.Infrastructure/Orders.Infrastructure.csproj", "src/Orders.Infrastructure/packages.lock.json", "src/Orders.Infrastructure/"]
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore src/Orders.Api/Orders.Api.csproj --locked-mode \
 && dotnet tool restore

# Then the source code (tests and charts are not needed in the image)
COPY src/ src/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/Orders.Api/Orders.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

############ bundle: one executable that applies EF Core migrations ############
FROM build AS bundle
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet ef migrations bundle \
      --project src/Orders.Infrastructure --startup-project src/Orders.Api \
      --configuration Release --self-contained -r linux-x64 -o /efbundle

############ migrator: tiny image that only runs the bundle ############
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled-extra AS migrator
WORKDIR /app
COPY --from=bundle --chown=$APP_UID:$APP_UID /efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["./efbundle"]

############ final: the API image that runs in production ############
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1
EXPOSE 8080
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
