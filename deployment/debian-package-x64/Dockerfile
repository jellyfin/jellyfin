FROM debian:10
# Docker build arguments
ARG SOURCE_DIR=/jellyfin
ARG PLATFORM_DIR=/jellyfin/deployment/debian-package-x64
ARG ARTIFACT_DIR=/dist
ARG SDK_VERSION=3.1
# Docker run environment
ENV SOURCE_DIR=/jellyfin
ENV ARTIFACT_DIR=/dist
ENV DEB_BUILD_OPTIONS=noddebs
ENV ARCH=amd64

# Prepare Debian build environment
RUN apt-get update \
 && apt-get install -y apt-transport-https debhelper gnupg wget npm devscripts mmv libc6-dev libcurl4-openssl-dev libfontconfig1-dev libfreetype6-dev libssl-dev libssl1.1 liblttng-ust0

# Install dotnet repository
# https://dotnet.microsoft.com/download/linux-package-manager/debian9/sdk-current
RUN wget https://download.visualstudio.microsoft.com/download/pr/d731f991-8e68-4c7c-8ea0-fad5605b077a/49497b5420eecbd905158d86d738af64/dotnet-sdk-3.1.100-linux-x64.tar.gz -O dotnet-sdk.tar.gz \
 && mkdir -p dotnet-sdk \
 && tar -xzf dotnet-sdk.tar.gz -C dotnet-sdk \
 && ln -s $( pwd )/dotnet-sdk/dotnet /usr/bin/dotnet

# Link to docker-build script
RUN ln -sf ${PLATFORM_DIR}/docker-build.sh /docker-build.sh

# Link to Debian source dir; mkdir needed or it fails, can't force dest
RUN mkdir -p ${SOURCE_DIR} && ln -sf ${PLATFORM_DIR}/pkg-src ${SOURCE_DIR}/debian

VOLUME ${ARTIFACT_DIR}/

COPY . ${SOURCE_DIR}/

ENTRYPOINT ["/docker-build.sh"]
