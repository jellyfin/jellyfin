FROM debian:9
ARG SOURCEDIR=/repo 
ENV DEB_BUILD_OPTIONS=noddebs

# https://dotnet.microsoft.com/download/linux-package-manager/debian9/sdk-current
RUN apt-get update \
 && apt-get install -y apt-transport-https debhelper gnupg wget devscripts \
 && wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg \
 && mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/ \
 && wget -q https://packages.microsoft.com/config/debian/9/prod.list \
 && mv prod.list /etc/apt/sources.list.d/microsoft-prod.list \
 && chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg \
 && chown root:root /etc/apt/sources.list.d/microsoft-prod.list \
 && apt-get update

WORKDIR ${SOURCEDIR}
COPY . .
COPY ./deployment/debian-x64/pkg-src ./debian

RUN yes | mk-build-deps -i debian/control \
    && dpkg-buildpackage -us -uc

WORKDIR /
