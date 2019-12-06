FROM debian:10
# Docker build arguments
ARG SOURCE_DIR=/jellyfin
ARG PLATFORM_DIR=/jellyfin/deployment/macos
ARG ARTIFACT_DIR=/dist
ARG SDK_VERSION=3.0
# Docker run environment
ENV SOURCE_DIR=/jellyfin
ENV ARTIFACT_DIR=/dist
ENV DEB_BUILD_OPTIONS=noddebs
ENV ARCH=amd64

# Prepare Debian build environment
RUN apt-get update \
 && apt-get install -y apt-transport-https debhelper gnupg wget devscripts mmv libc6-dev libcurl4-openssl-dev libfontconfig1-dev libfreetype6-dev libssl-dev libssl1.1 liblttng-ust0

# Install dotnet repository
# https://dotnet.microsoft.com/download/linux-package-manager/debian9/sdk-current
RUN wget https://download.visualstudio.microsoft.com/download/pr/4f51cfd8-311d-43fe-a887-c80b40358cfd/440d10dc2091b8d0f1a12b7124034e49/dotnet-sdk-3.0.101-linux-x64.tar.gz -O dotnet-sdk.tar.gz \
 && mkdir -p dotnet-sdk \
 && tar -xzf dotnet-sdk.tar.gz -C dotnet-sdk \
 && ln -s $( pwd )/dotnet-sdk/dotnet /usr/bin/dotnet

# Install yarn package manager
RUN wget -q -O- https://dl.yarnpkg.com/debian/pubkey.gpg | apt-key add - \
 && echo "deb https://dl.yarnpkg.com/debian/ stable main" > /etc/apt/sources.list.d/yarn.list \
 && apt update \
 && apt install -y yarn

# Link to docker-build script
RUN ln -sf ${PLATFORM_DIR}/docker-build.sh /docker-build.sh

VOLUME ${ARTIFACT_DIR}/

COPY . ${SOURCE_DIR}/

ENTRYPOINT ["/docker-build.sh"]
