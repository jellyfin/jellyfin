# Requires binfm_misc registration
# https://github.com/multiarch/qemu-user-static#binfmt_misc-register
ARG DOTNET_VERSION=3.0


FROM multiarch/qemu-user-static:x86_64-aarch64 as qemu
FROM alpine as qemu_extract
COPY --from=qemu /usr/bin qemu-aarch64-static.tar.gz
RUN tar -xzvf qemu-aarch64-static.tar.gz


FROM microsoft/dotnet:${DOTNET_VERSION}-sdk-stretch as builder
WORKDIR /repo
COPY . .
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
# TODO Remove or update the sed line when we update dotnet version.
RUN find . -type f -exec sed -i 's/netcoreapp2.1/netcoreapp3.0/g' {} \;
# Discard objs - may cause failures if exists
RUN find . -type d -name obj | xargs -r rm -r
# Build
RUN dotnet publish \
    -r linux-arm64 \
    --configuration release \
    --output /jellyfin \
    Jellyfin.Server


FROM microsoft/dotnet:${DOTNET_VERSION}-runtime-stretch-slim-arm64v8
COPY --from=qemu_extract qemu-aarch64-static /usr/bin
RUN apt-get update \
 && apt-get install --no-install-recommends --no-install-suggests -y ffmpeg \
 && mkdir -p /cache /config /media \
 && chmod 777 /cache /config /media
COPY --from=builder /jellyfin /jellyfin
EXPOSE 8096
VOLUME /cache /config /media
ENTRYPOINT dotnet /jellyfin/jellyfin.dll --datadir /config --cachedir /cache
