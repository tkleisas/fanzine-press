# syntax=docker/dockerfile:1.6
# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Version info supplied by the deploy script (git describe + git rev-parse).
# When building manually, these defaults keep the footer showing "dev".
ARG APP_VERSION=dev
ARG GIT_SHA=unknown

# Restore as a distinct layer for better caching
COPY src/FanzinePress.Web/FanzinePress.Web.csproj src/FanzinePress.Web/
RUN dotnet restore src/FanzinePress.Web/FanzinePress.Web.csproj

# Copy the rest and publish.
# /p:Version sets the display version, /p:SourceRevisionId is appended to
# AssemblyInformationalVersion after '+' so the runtime can parse it out.
COPY src/ src/
RUN dotnet publish src/FanzinePress.Web/FanzinePress.Web.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:Version=${APP_VERSION} \
    /p:SourceRevisionId=${GIT_SHA}

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install Google Chrome (stable) for PuppeteerSharp PDF rendering.
# Ubuntu 24.04's "chromium" package is only a snap shim that can't run in containers,
# so we add Google's official apt repo instead.
# fonts-liberation + fonts-noto* give decent Latin/Greek coverage.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        wget \
        gnupg \
        ca-certificates \
    && wget -qO /usr/share/keyrings/google-chrome.gpg.asc https://dl.google.com/linux/linux_signing_key.pub \
    && gpg --dearmor < /usr/share/keyrings/google-chrome.gpg.asc > /usr/share/keyrings/google-chrome.gpg \
    && rm /usr/share/keyrings/google-chrome.gpg.asc \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/google-chrome.gpg] http://dl.google.com/linux/chrome/deb/ stable main" \
        > /etc/apt/sources.list.d/google-chrome.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
        google-chrome-stable \
        fonts-liberation \
        fonts-noto \
        fonts-noto-color-emoji \
        tini \
    && rm -rf /var/lib/apt/lists/*

# Non-root user for the app
RUN groupadd --system --gid 1001 fanzine \
    && useradd  --system --uid 1001 --gid 1001 --home /app --shell /usr/sbin/nologin fanzine \
    && mkdir -p /data \
    && chown -R fanzine:fanzine /app /data

COPY --from=build --chown=fanzine:fanzine /app/publish ./

USER fanzine

# Data directory for SQLite — mount a host volume here for persistence.
VOLUME ["/data"]

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Data Source=/data/fanzinepress.db" \
    FANZINE_CHROMIUM_PATH=/usr/bin/google-chrome \
    FANZINE_BEHIND_PROXY=true \
    FANZINE_PATH_BASE=/fanzine-press \
    FANZINE_DATA_PROTECTION_KEYS=/data/dp-keys \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

# tini as PID 1 reaps any chromium zombies PuppeteerSharp leaves behind.
ENTRYPOINT ["/usr/bin/tini", "--", "dotnet", "FanzinePress.Web.dll"]
