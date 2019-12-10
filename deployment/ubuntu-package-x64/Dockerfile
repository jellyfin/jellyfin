FROM microsoft/dotnet:3.0-sdk-bionic
# Docker build arguments
ARG SOURCE_DIR=/jellyfin
ARG PLATFORM_DIR=/jellyfin/deployment/ubuntu-package-x64
ARG ARTIFACT_DIR=/dist
# Docker run environment
ENV SOURCE_DIR=/jellyfin
ENV ARTIFACT_DIR=/dist
ENV DEB_BUILD_OPTIONS=noddebs

# Prepare Ubuntu build environment
RUN apt-get update \
 && apt-get install -y apt-transport-https debhelper gnupg wget devscripts mmv libc6-dev libcurl4-openssl-dev libfontconfig1-dev libfreetype6-dev libssl-dev \
 && ln -sf ${PLATFORM_DIR}/docker-build.sh /docker-build.sh \
 && mkdir -p ${SOURCE_DIR} && ln -sf ${PLATFORM_DIR}/pkg-src ${SOURCE_DIR}/debian

# Install npm package manager
RUN wget -q -O- https://deb.nodesource.com/gpgkey/nodesource.gpg.key | apt-key add - \
 && echo "deb https://deb.nodesource.com/node_8.x $(lsb_release -s -c) main" > /etc/apt/sources.list.d/npm.list \
 && apt update \
 && apt install -y nodejs

VOLUME ${ARTIFACT_DIR}/

COPY . ${SOURCE_DIR}/

ENTRYPOINT ["/docker-build.sh"]
