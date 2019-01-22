FROM fedora:29
ARG HOME=/build
RUN mkdir /build && \
    dnf install -y @buildsys-build rpmdevtools dnf-plugins-core && \
    dnf copr enable -y @dotnet-sig/dotnet && \
    rpmdev-setuptree

WORKDIR /build/rpmbuild
COPY ./deployment/fedora-package-x64/pkg-src/jellyfin.spec SPECS
COPY ./deployment/fedora-package-x64/pkg-src/ SOURCES

RUN spectool -g -R SPECS/jellyfin.spec && \
    rpmbuild -bs SPECS/jellyfin.spec && \
    dnf build-dep -y SRPMS/jellyfin-*.src.rpm && \
    rpmbuild -bb SPECS/jellyfin.spec;