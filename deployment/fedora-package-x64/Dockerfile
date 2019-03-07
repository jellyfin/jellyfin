FROM fedora:29
# Docker build arguments
ARG SOURCE_DIR=/jellyfin
ARG PLATFORM_DIR=/jellyfin/deployment/fedora-package-x64
ARG ARTIFACT_DIR=/dist
ARG SDK_VERSION=2.2
# Docker run environment
ENV SOURCE_DIR=/jellyfin
ENV ARTIFACT_DIR=/dist

# Prepare Fedora build environment
RUN dnf update -y \
 && dnf install -y @buildsys-build rpmdevtools dnf-plugins-core libcurl-devel fontconfig-devel freetype-devel openssl-devel glibc-devel libicu-devel \
 && dnf copr enable -y @dotnet-sig/dotnet \
 && rpmdev-setuptree \
 && dnf install -y dotnet-sdk-${SDK_VERSION} dotnet-runtime-${SDK_VERSION} \
 && ln -sf ${PLATFORM_DIR}/docker-build.sh /docker-build.sh \
 && mkdir -p ${SOURCE_DIR}/SPECS \
 && ln -s ${PLATFORM_DIR}/pkg-src/jellyfin.spec ${SOURCE_DIR}/SPECS/jellyfin.spec \
 && mkdir -p ${SOURCE_DIR}/SOURCES \
 && ln -s ${PLATFORM_DIR}/pkg-src ${SOURCE_DIR}/SOURCES

VOLUME ${ARTIFACT_DIR}/

COPY . ${SOURCE_DIR}/

ENTRYPOINT ["/docker-build.sh"]
