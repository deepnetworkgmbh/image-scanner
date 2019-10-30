FROM mcr.microsoft.com/dotnet/core/sdk:3.0-alpine3.9 AS build
WORKDIR /app/src
COPY /kube-scanner .
RUN dotnet build kube-scanner.sln -c Release

FROM build AS publish
WORKDIR /app/src
RUN dotnet publish kube-scanner.csproj -c Release -o /app/publish --no-restore --no-build

FROM mcr.microsoft.com/dotnet/core/runtime:3.0-alpine3.9 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet","kube-scanner.dll"]