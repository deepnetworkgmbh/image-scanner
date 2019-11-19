FROM mcr.microsoft.com/dotnet/core/sdk:3.0.100-alpine3.10 AS build
WORKDIR /app/src
COPY /src .
RUN dotnet build kube-scanner.sln -c Release

FROM build AS publish
WORKDIR /app/src
RUN dotnet publish cli/cli.csproj -c Release -o /app/publish --no-restore --no-build

FROM mcr.microsoft.com/dotnet/core/runtime:3.0.0-alpine3.10 AS final
WORKDIR /app
COPY --from=publish /app/publish .

COPY trivy /usr/local/bin/trivy

ENTRYPOINT ["dotnet","cli.dll"]