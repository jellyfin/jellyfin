FROM microsoft/dotnet:2.2-sdk-bionic
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
 && apt-get install -y apt-transport-https debhelper gnupg wget devscripts mmv libc6-dev libcurl4-openssl-dev libfontconfig1-dev libfreetype6-dev \
 && ln -sf ${PLATFORM_DIR}/docker-build.sh /docker-build.sh \
 && mkdir -p ${SOURCE_DIR} && ln -sf ${PLATFORM_DIR}/pkg-src ${SOURCE_DIR}/debian

VOLUME ${ARTIFACT_DIR}/

COPY . ${SOURCE_DIR}/

ENTRYPOINT ["/docker-build.sh"]
