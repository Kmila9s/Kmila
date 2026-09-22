# syntax=docker/dockerfile:1
#
# Kmila — web version (App/Kmila/Kmila.Web), a .NET 9 Blazor Server app.
# Multi-stage: full SDK to restore/publish, slim ASP.NET runtime to serve.
#
# Build context MUST be the repo root — Kmila.Shared references the repo-root
# sibling projects (Debugger/Interpreter/LibraryCompiler/Parser/TimeMachine)
# via ../../../…, so they have to be inside the context.
#
#   docker build -t kmila-web .
#   docker run --rm -p 8080:8080 -v kmila-data:/data kmila-web
#
# The MAUI workload is NOT required: Kmila.Web is a single net9.0 head and a
# net9.0 consumer of the multi-targeted Kmila.Shared builds only its net9.0
# target (UseMaui is false there). Publish the .csproj, never the .sln.

# ─────────────────────────── build stage ───────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1) Restore layer: copy only the .csproj files of the dependency closure
#    first, so `dotnet restore` is cached until a csproj actually changes.
COPY ["App/Kmila/Kmila.Web/Kmila.Web.csproj", "App/Kmila/Kmila.Web/"]
COPY ["App/Kmila/Kmila.Shared/Kmila.Shared.csproj", "App/Kmila/Kmila.Shared/"]
COPY ["Debugger/Debugger.csproj", "Debugger/"]
COPY ["Interpreter/Interpreter.csproj", "Interpreter/"]
COPY ["LibraryCompiler/LibraryCompiler.csproj", "LibraryCompiler/"]
COPY ["Parser/Parser.csproj", "Parser/"]
COPY ["TimeMachine/TimeMachine.csproj", "TimeMachine/"]
# Kmila.Shared and the transitive libs multi-target
# net9.0;net9.0-android;net9.0-ios;net9.0-maccatalyst. The MAUI TFMs need
# workloads (ios/maccatalyst can't even be installed on Linux), and `restore`
# would otherwise walk every TFM. Forcing TargetFrameworks=net9.0 as a GLOBAL
# MSBuild property collapses the whole graph to net9.0 only — no workloads, no
# csproj edits (global properties override the project-file value).
RUN dotnet restore "App/Kmila/Kmila.Web/Kmila.Web.csproj" -p:TargetFrameworks=net9.0

# 2) Copy the sources of just those projects and publish.
COPY App/Kmila/Kmila.Web/   App/Kmila/Kmila.Web/
COPY App/Kmila/Kmila.Shared/ App/Kmila/Kmila.Shared/
COPY Debugger/       Debugger/
COPY Interpreter/    Interpreter/
COPY LibraryCompiler/ LibraryCompiler/
COPY Parser/         Parser/
COPY TimeMachine/    TimeMachine/
# -f net9.0 selects the single TFM to publish (the plural <TargetFrameworks>
# property makes the project "cross-targeting" even with one value, so publish
# needs an explicit framework); -p:TargetFrameworks=net9.0 keeps the referenced
# libs from pulling their MAUI TFMs during the publish build.
RUN dotnet publish "App/Kmila/Kmila.Web/Kmila.Web.csproj" \
        -c Release -o /app/publish -f net9.0 \
        --no-restore -p:TargetFrameworks=net9.0 -p:UseAppHost=false

# ────────────────────────── runtime stage ──────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# curl is only needed for the container HEALTHCHECK below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Persistent state lives under the app user's HOME (the DB path is hardcoded
# to SpecialFolder.ApplicationData = $HOME/.config/KmilaData; user assets /
# library cache go there too, the API sandbox to $HOME/.kmila, and
# data-protection keys to $HOME/.aspnet). Pin HOME to /data and give it to the
# non-root app user. A fresh named volume mounted at /data inherits this
# directory's ownership, so the non-root user can write to it.
RUN mkdir -p /data && chown $APP_UID:$APP_UID /data

# HOME feeds $HOME/.kmila (API sandbox) and $HOME/.aspnet (data-protection
# keys). XDG_CONFIG_HOME is set EXPLICITLY because SpecialFolder.ApplicationData
# (where the SQLite DB + user assets live) resolves via the passwd database on
# .NET/Linux, and the non-root `app` UID has no passwd home entry → it would
# otherwise come back empty and the DB dir would be created as the relative
# ./KmilaData under /app (read-only) and crash. Both land under the /data volume.
ENV HOME=/data \
    XDG_CONFIG_HOME=/data/.config \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_gcServer=0

WORKDIR /app
COPY --from=build /app/publish ./

USER $APP_UID
EXPOSE 8080
VOLUME ["/data"]

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=5 \
    CMD curl -fsS http://localhost:8080/ || exit 1

# Pre-create the config dir before launching. Environment.GetFolderPath uses
# the default (verify-exists) option, so SpecialFolder.ApplicationData resolves
# to "" until $XDG_CONFIG_HOME actually exists — on a fresh /data volume it
# doesn't, which would send the SQLite DB to a relative ./KmilaData under the
# read-only /app and crash. Creating it here (the app user owns /data) makes
# ApplicationData resolve to /data/.config so the DB + assets land in the volume.
ENTRYPOINT ["/bin/sh", "-c", "mkdir -p \"$XDG_CONFIG_HOME\" \"$HOME/.aspnet\" \"$HOME/.kmila\" && exec dotnet Kmila.Web.dll"]
