FROM centos:7
ARG HOME=/build
RUN mkdir /build && \
    yum install -y @buildsys-build rpmdevtools yum-plugins-core && \
    rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm && \
    rpmdev-setuptree

WORKDIR /build/rpmbuild
COPY ./deployment/centos-package-x64/pkg-src/jellyfin.spec SPECS
COPY ./deployment/centos-package-x64/pkg-src/ SOURCES

RUN spectool -g -R SPECS/jellyfin.spec && \
    rpmbuild -bs SPECS/jellyfin.spec && \
    yum-builddep  -y SRPMS/jellyfin-*.src.rpm && \
    rpmbuild -bb SPECS/jellyfin.spec;