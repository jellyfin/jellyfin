FROM centos:7
# Docker build arguments
ARG SOURCE_DIR=/jellyfin
ARG PLATFORM_DIR=/jellyfin/deployment/centos-package-x64
ARG ARTIFACT_DIR=/dist
ARG SDK_VERSION=2.2
# Docker run environment
ENV SOURCE_DIR=/jellyfin
ENV ARTIFACT_DIR=/dist

# Prepare CentOS environment
RUN yum update -y \
 && yum install -y epel-release

# Install build dependencies
RUN yum install -y @buildsys-build rpmdevtools yum-plugins-core libcurl-devel fontconfig-devel freetype-devel openssl-devel glibc-devel libicu-devel wget git

# Install recent NodeJS and Yarn
RUN wget -O- https://raw.githubusercontent.com/creationix/nvm/v0.35.0/install.sh | /bin/bash \
 && source "$HOME/.nvm/nvm.sh" \
 && nvm install v8 \
 && npm install -g yarn

# Install DotNET SDK
RUN rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm \
 && rpmdev-setuptree \
 && yum install -y dotnet-sdk-${SDK_VERSION}

# Create symlinks and directories
RUN ln -sf ${PLATFORM_DIR}/docker-build.sh /docker-build.sh \
 && mkdir -p ${SOURCE_DIR}/SPECS \
 && ln -s ${PLATFORM_DIR}/pkg-src/jellyfin.spec ${SOURCE_DIR}/SPECS/jellyfin.spec \
 && mkdir -p ${SOURCE_DIR}/SOURCES \
 && ln -s ${PLATFORM_DIR}/pkg-src ${SOURCE_DIR}/SOURCES

VOLUME ${ARTIFACT_DIR}/

COPY . ${SOURCE_DIR}/

ENTRYPOINT ["/docker-build.sh"]
