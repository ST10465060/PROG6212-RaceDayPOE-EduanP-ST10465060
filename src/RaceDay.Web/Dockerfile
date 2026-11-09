# ---------------------------------------------------------------
# Stage 1: build
# The SDK image has the compiler and is roughly 800 MB. We only
# need it to produce the output, so it does not ship to production.
# ---------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy only the project files first. Docker caches each layer, so as
# long as the dependencies have not changed, the restore below is
# skipped on later builds even when the source code changes.
COPY src/RaceDay.Web/RaceDay.Web.csproj src/RaceDay.Web/
COPY src/RaceDay.Contracts/RaceDay.Contracts.csproj src/RaceDay.Contracts/

RUN dotnet restore src/RaceDay.Web/RaceDay.Web.csproj

# Now bring in the rest of the source and publish.
COPY src/RaceDay.Web/ src/RaceDay.Web/
COPY src/RaceDay.Contracts/ src/RaceDay.Contracts/

# --no-restore because the step above already did it.
RUN dotnet publish src/RaceDay.Web/RaceDay.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------------------------------------------------------------
# Stage 2: runtime
# The ASP.NET image has no compiler and is far smaller. Only the
# published output is copied across, so none of the source code
# or build tooling ends up in the final image.
# ---------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Run as the built-in non-root user. If the container is ever
# compromised, the attacker does not get root inside it.
USER $APP_UID

# Kestrel defaults to port 5000 in a container, so we tell it
# explicitly which port to bind. This must match the EXPOSE line
# and the right-hand side of the -p flag in the run command.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]