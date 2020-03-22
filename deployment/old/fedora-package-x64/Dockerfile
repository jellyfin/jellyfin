FROM fedora:31
# Docker build arguments
ARG SOURCE_DIR=/jellyfin
ARG PLATFORM_DIR=/jellyfin/deployment/fedora-package-x64
ARG ARTIFACT_DIR=/dist
ARG SDK_VERSION=3.1
# Docker run environment
ENV SOURCE_DIR=/jellyfin
ENV ARTIFACT_DIR=/dist

# Prepare Fedora environment
RUN dnf update -y

# Install build dependencies
RUN dnf install -y @buildsys-build rpmdevtools git dnf-plugins-core libcurl-devel fontconfig-devel freetype-devel openssl-devel glibc-devel libicu-devel nodejs-yarn

# Install DotNET SDK
RUN rpm --import https://packages.microsoft.com/keys/microsoft.asc \
 && curl -o /etc/yum.repos.d/microsoft-prod.repo https://packages.microsoft.com/config/fedora/$(rpm -E %fedora)/prod.repo \
 && dnf install -y dotnet-sdk-${SDK_VERSION} dotnet-runtime-${SDK_VERSION}

# Create symlinks and directories
RUN ln -sf ${PLATFORM_DIR}/docker-build.sh /docker-build.sh \
 && mkdir -p ${SOURCE_DIR}/SPECS \
 && ln -s ${PLATFORM_DIR}/pkg-src/jellyfin.spec ${SOURCE_DIR}/SPECS/jellyfin.spec \
 && mkdir -p ${SOURCE_DIR}/SOURCES \
 && ln -s ${PLATFORM_DIR}/pkg-src ${SOURCE_DIR}/SOURCES

VOLUME ${ARTIFACT_DIR}/

COPY . ${SOURCE_DIR}/

ENTRYPOINT ["/docker-build.sh"]
