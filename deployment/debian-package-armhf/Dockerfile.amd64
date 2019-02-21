FROM debian:9
# Docker build arguments
ARG SOURCE_DIR=/jellyfin
ARG PLATFORM_DIR=/jellyfin/deployment/debian-package-armhf
ARG ARTIFACT_DIR=/dist
ARG SDK_VERSION=2.2
# Docker run environment
ENV SOURCE_DIR=/jellyfin
ENV ARTIFACT_DIR=/dist
ENV DEB_BUILD_OPTIONS=noddebs
ENV ARCH=amd64

# Prepare Debian build environment
RUN apt-get update \
 && apt-get install -y apt-transport-https debhelper gnupg wget devscripts mmv 

# Install dotnet repository
# https://dotnet.microsoft.com/download/linux-package-manager/debian9/sdk-current
RUN wget https://download.visualstudio.microsoft.com/download/pr/69937b49-a877-4ced-81e6-286620b390ab/8ab938cf6f5e83b2221630354160ef21/dotnet-sdk-2.2.104-linux-x64.tar.gz -O dotnet-sdk.tar.gz \
 && mkdir -p dotnet-sdk \
 && tar -xzf dotnet-sdk.tar.gz -C dotnet-sdk \
 && ln -s $( pwd )/dotnet-sdk/dotnet /usr/bin/dotnet

# Prepare the cross-toolchain
RUN dpkg --add-architecture armhf \
 && apt-get update \
 && apt-get install -y cross-gcc-dev \
 && TARGET_LIST="armhf" cross-gcc-gensource 6 \
 && cd cross-gcc-packages-amd64/cross-gcc-6-armhf \
 && apt-get install -y gcc-6-source libstdc++6-armhf-cross binutils-arm-linux-gnueabihf bison flex libtool gdb sharutils netbase libcloog-isl-dev libmpc-dev libmpfr-dev libgmp-dev systemtap-sdt-dev autogen expect chrpath zlib1g-dev zip libc6-dev:armhf linux-libc-dev:armhf libgcc1:armhf libcurl4-openssl-dev:armhf libfontconfig1-dev:armhf libfreetype6-dev:armhf liblttng-ust0:armhf libstdc++6:armhf

# Link to docker-build script
RUN ln -sf ${PLATFORM_DIR}/docker-build.sh /docker-build.sh

# Link to Debian source dir; mkdir needed or it fails, can't force dest
RUN mkdir -p ${SOURCE_DIR} && ln -sf ${PLATFORM_DIR}/pkg-src ${SOURCE_DIR}/debian

VOLUME ${ARTIFACT_DIR}/

COPY . ${SOURCE_DIR}/

ENTRYPOINT ["/docker-build.sh"]
