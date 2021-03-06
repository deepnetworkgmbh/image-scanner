FROM mcr.microsoft.com/dotnet/core/sdk:3.1.102-alpine3.10 AS build
WORKDIR /app/src
COPY /src .
RUN dotnet build image-scanner.sln -c Release


FROM build AS testrunner
WORKDIR /app/src/tests
ENTRYPOINT ["dotnet","test","--logger:trx;LogFileName=test_results.xml","--no-build","-c","Release","-r","/app/testresults"]


FROM build AS publish-cli
WORKDIR /app/src
RUN dotnet publish cli/cli.csproj -c Release -o /app/publish --no-restore --no-build


FROM mcr.microsoft.com/dotnet/core/runtime:3.1.2-alpine3.10 AS cliapp
WORKDIR /app
COPY --from=publish-cli /app/publish .

COPY /trivy/trivy /usr/local/bin/trivy

ENTRYPOINT ["dotnet","cli.dll"]


FROM build AS publish-webapp
WORKDIR /app/src
RUN dotnet publish webapp/webapp.csproj -c Release -o /app/publish --no-restore --no-build


FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.2-alpine3.10 AS webapp
WORKDIR /app
COPY --from=publish-webapp /app/publish .

COPY /trivy/trivy /usr/local/bin/trivy

# Change to 8080 because 80 requires elevated privileges
ENV ASPNETCORE_URLS="http://*:8080"
EXPOSE 8080

ENV USER=image-scanner-webapp
ENV UID=10001
ENV GID=10001
RUN addgroup "$USER" --gid "$GID" \
    && adduser \
    --disabled-password \
    --gecos "" \
    --home "$(pwd)" \
    --ingroup "$USER" \
    --no-create-home \
    --uid "$UID" \
    "$USER"
RUN chown -R "$USER":"$USER" /app

# disable managed debugging, which requires user access to /tmp directory owned by root
ENV COMPlus_EnableDiagnostics=0

USER "$USER"
ENTRYPOINT ["dotnet","webapp.dll"]