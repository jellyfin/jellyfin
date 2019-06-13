# Requires binfm_misc registration
# https://github.com/multiarch/qemu-user-static#binfmt_misc-register
ARG DOTNET_VERSION=3.0


FROM mcr.microsoft.com/dotnet/core/sdk:${DOTNET_VERSION} as builder
WORKDIR /repo
COPY . .
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
# TODO Remove or update the sed line when we update dotnet version.
RUN find . -type f -exec sed -i 's/netcoreapp2.1/netcoreapp3.0/g' {} \;
# Discard objs - may cause failures if exists
RUN find . -type d -name obj | xargs -r rm -r
# Build
RUN bash -c "source deployment/common.build.sh && \
    build_jellyfin Jellyfin.Server Release linux-arm /jellyfin"


FROM multiarch/qemu-user-static:x86_64-arm as qemu
FROM mcr.microsoft.com/dotnet/core/runtime:${DOTNET_VERSION}-stretch-slim-arm32v7
COPY --from=qemu /usr/bin/qemu-arm-static /usr/bin
RUN apt-get update \
 && apt-get install --no-install-recommends --no-install-suggests -y ffmpeg \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /cache /config /media \
 && chmod 777 /cache /config /media
COPY --from=builder /jellyfin /jellyfin

ARG JELLYFIN_WEB_VERSION=10.3.5
RUN curl -L https://github.com/jellyfin/jellyfin-web/archive/v${JELLYFIN_WEB_VERSION}.tar.gz | tar zxf - \
 && rm -rf /jellyfin/jellyfin-web \
 && mv jellyfin-web-${JELLYFIN_WEB_VERSION} /jellyfin/jellyfin-web

EXPOSE 8096
VOLUME /cache /config /media
ENTRYPOINT dotnet /jellyfin/jellyfin.dll \
    --datadir /config \
    --cachedir /cache \
    --ffmpeg /usr/bin/ffmpeg
