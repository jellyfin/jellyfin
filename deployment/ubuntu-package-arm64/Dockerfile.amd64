FROM ubuntu:bionic
# Docker build arguments
ARG SOURCE_DIR=/jellyfin
ARG PLATFORM_DIR=/jellyfin/deployment/ubuntu-package-arm64
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
RUN rm /etc/apt/sources.list \
 && export CODENAME="$( lsb_release -c -s )" \
 && echo "deb [arch=amd64] http://archive.ubuntu.com/ubuntu/ ${CODENAME} main restricted universe multiverse" >>/etc/apt/sources.list.d/amd64.list \
 && echo "deb [arch=amd64] http://archive.ubuntu.com/ubuntu/ ${CODENAME}-updates main restricted universe multiverse" >>/etc/apt/sources.list.d/amd64.list \
 && echo "deb [arch=amd64] http://archive.ubuntu.com/ubuntu/ ${CODENAME}-backports main restricted universe multiverse" >>/etc/apt/sources.list.d/amd64.list \
 && echo "deb [arch=amd64] http://archive.ubuntu.com/ubuntu/ ${CODENAME}-security main restricted universe multiverse" >>/etc/apt/sources.list.d/amd64.list \
 && echo "deb [arch=arm64] http://ports.ubuntu.com/ ${CODENAME} main restricted universe multiverse" >>/etc/apt/sources.list.d/arm64.list \
 && echo "deb [arch=arm64] http://ports.ubuntu.com/ ${CODENAME}-updates main restricted universe multiverse" >>/etc/apt/sources.list.d/arm64.list \
 && echo "deb [arch=arm64] http://ports.ubuntu.com/ ${CODENAME}-backports main restricted universe multiverse" >>/etc/apt/sources.list.d/arm64.list \
 && echo "deb [arch=arm64] http://ports.ubuntu.com/ ${CODENAME}-security main restricted universe multiverse" >>/etc/apt/sources.list.d/arm64.list \
 && dpkg --add-architecture arm64 \
 && apt-get update \
 && apt-get install -y cross-gcc-dev \
 && TARGET_LIST="arm64" cross-gcc-gensource 6 \
 && cd cross-gcc-packages-amd64/cross-gcc-6-arm64 \
 && ln -fs /usr/share/zoneinfo/America/Toronto /etc/localtime \
 && apt-get install -y gcc-6-source libstdc++6-arm64-cross binutils-aarch64-linux-gnu bison flex libtool gdb sharutils netbase libcloog-isl-dev libmpc-dev libmpfr-dev libgmp-dev systemtap-sdt-dev autogen expect chrpath zlib1g-dev zip libc6-dev:arm64 linux-libc-dev:arm64 libgcc1:arm64 libcurl4-openssl-dev:arm64 libfontconfig1-dev:arm64 libfreetype6-dev:arm64 liblttng-ust0:arm64 libstdc++6:arm64

# Link to docker-build script
RUN ln -sf ${PLATFORM_DIR}/docker-build.sh /docker-build.sh

# Link to Debian source dir; mkdir needed or it fails, can't force dest
RUN mkdir -p ${SOURCE_DIR} && ln -sf ${PLATFORM_DIR}/pkg-src ${SOURCE_DIR}/debian

VOLUME ${ARTIFACT_DIR}/

COPY . ${SOURCE_DIR}/

ENTRYPOINT ["/docker-build.sh"]
