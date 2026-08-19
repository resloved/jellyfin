# Lightweight fork image: builds only the patched Jellyfin.Server output and
# layers it over the official jellyfin/jellyfin image, so jellyfin-web and
# ffmpeg keep coming from upstream unmodified. See jellyfin-fork-brief.md
# ("Extend the official image" strategy) for why this exists instead of a
# full jellyfin-packaging fork.

ARG BASE_IMAGE=jellyfin/jellyfin:latest

FROM debian:trixie-slim AS build

ARG DOTNET_VERSION=10.0
ARG DOTNET_ARCH=x64
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

WORKDIR /src

RUN apt-get update \
 && apt-get install --no-install-recommends --no-install-suggests --yes \
    curl \
    ca-certificates \
    libicu76 \
 && curl -fsSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel ${DOTNET_VERSION} --install-dir /usr/local/bin

COPY . .

RUN dotnet publish Jellyfin.Server --arch ${DOTNET_ARCH} --configuration Release \
    --output=/out --self-contained \
    -p:DebugSymbols=false -p:DebugType=none

FROM ${BASE_IMAGE}

# Overwrite the server assemblies only; JELLYFIN_WEB_DIR (jellyfin-web) and
# the ffmpeg install from the base image are left untouched.
COPY --from=build /out /jellyfin
