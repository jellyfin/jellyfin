%global         debug_package %{nil}
# Set the dotnet runtime
%if 0%{?fedora}
%global         dotnet_runtime  fedora.%{fedora}-x64
%else
%global         dotnet_runtime  centos-x64
%endif

Name:           jellyfin
Version:        10.8.4
Release:        1%{?dist}
Summary:        The Free Software Media System
License:        GPLv2
URL:            https://jellyfin.org
# Jellyfin Server tarball created by `make -f .copr/Makefile srpm`, real URL ends with `v%%{version}.tar.gz`
Source0:        jellyfin-server-%{version}.tar.gz
Source11:       jellyfin.service
Source12:       jellyfin.env
Source13:       jellyfin.sudoers
Source14:       restart.sh
Source15:       jellyfin.override.conf
Source16:       jellyfin-firewalld.xml
Source17:       jellyfin-server-lowports.conf

%{?systemd_requires}
BuildRequires:  systemd
BuildRequires:  libcurl-devel, fontconfig-devel, freetype-devel, openssl-devel, glibc-devel, libicu-devel
# Requirements not packaged in RHEL 7 main repos, added via Makefile
# https://packages.microsoft.com/rhel/7/prod/
BuildRequires:  dotnet-runtime-6.0, dotnet-sdk-6.0
Requires: %{name}-server = %{version}-%{release}, %{name}-web = %{version}-%{release}

# Temporary (hopefully?) fix for https://github.com/jellyfin/jellyfin/issues/7471
%if 0%{?fedora} >= 36
%global __requires_exclude ^liblttng-ust\\.so\\.0.*$
%endif


%description
Jellyfin is a free software media system that puts you in control of managing and streaming your media.

%package server
# RPMfusion free
Summary:        The Free Software Media System Server backend
Requires(pre):  shadow-utils
Requires:       ffmpeg
Requires:       libcurl, fontconfig, freetype, openssl, glibc, libicu, at, sudo

%description server
The Jellyfin media server backend.

%package server-lowports
# RPMfusion free
Summary:        The Free Software Media System Server backend.  Low-port binding.
Requires:       jellyfin-server

%description server-lowports
The Jellyfin media server backend low port binding package.  This package
enables binding to ports < 1024.  You would install this if you want
the Jellyfin server to bind to ports 80 and/or 443 for example.

%prep
%autosetup -n jellyfin-server-%{version} -b 0


%build
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export PATH=$PATH:/usr/local/bin
# cannot use --output due to https://github.com/dotnet/sdk/issues/22220
dotnet publish --configuration Release --self-contained --runtime %{dotnet_runtime} \
    -p:DebugSymbols=false -p:DebugType=none Jellyfin.Server


%install
# Jellyfin files
%{__mkdir} -p %{buildroot}%{_libdir}/jellyfin %{buildroot}%{_bindir}
%{__cp} -r Jellyfin.Server/bin/Release/net6.0/%{dotnet_runtime}/publish/* %{buildroot}%{_libdir}/jellyfin
ln -srf %{_libdir}/jellyfin/jellyfin %{buildroot}%{_bindir}/jellyfin
%{__install} -D %{SOURCE14} %{buildroot}%{_libexecdir}/jellyfin/restart.sh

# Jellyfin config
%{__install} -D Jellyfin.Server/Resources/Configuration/logging.json %{buildroot}%{_sysconfdir}/jellyfin/logging.json
%{__install} -D %{SOURCE12} %{buildroot}%{_sysconfdir}/sysconfig/jellyfin

# system config
%{__install} -D %{SOURCE16} %{buildroot}%{_prefix}/lib/firewalld/services/jellyfin.xml
%{__install} -D %{SOURCE13} %{buildroot}%{_sysconfdir}/sudoers.d/jellyfin-sudoers
%{__install} -D %{SOURCE15} %{buildroot}%{_sysconfdir}/systemd/system/jellyfin.service.d/override.conf
%{__install} -D %{SOURCE11} %{buildroot}%{_unitdir}/jellyfin.service

# empty directories
%{__mkdir} -p %{buildroot}%{_sharedstatedir}/jellyfin
%{__mkdir} -p %{buildroot}%{_sysconfdir}/jellyfin
%{__mkdir} -p %{buildroot}%{_var}/cache/jellyfin
%{__mkdir} -p %{buildroot}%{_var}/log/jellyfin

# jellyfin-server-lowports subpackage
%{__install} -D -m 0644 %{SOURCE17} %{buildroot}%{_unitdir}/jellyfin.service.d/jellyfin-server-lowports.conf


%files
# empty as this is just a meta-package

%files server
%defattr(644,root,root,755)

# Jellyfin files
%{_bindir}/jellyfin
# Needs 755 else only root can run it since binary build by dotnet is 722
%attr(755,root,root) %{_libdir}/jellyfin/createdump
%attr(755,root,root) %{_libdir}/jellyfin/jellyfin
%{_libdir}/jellyfin/*
%attr(755,root,root) %{_libexecdir}/jellyfin/restart.sh

# Jellyfin config
%config(noreplace) %attr(644,jellyfin,jellyfin) %{_sysconfdir}/jellyfin/logging.json
%config %{_sysconfdir}/sysconfig/jellyfin

# system config
%{_prefix}/lib/firewalld/services/jellyfin.xml
%{_unitdir}/jellyfin.service
%config(noreplace) %attr(600,root,root) %{_sysconfdir}/sudoers.d/jellyfin-sudoers
%config(noreplace) %{_sysconfdir}/systemd/system/jellyfin.service.d/override.conf

# empty directories
%attr(750,jellyfin,jellyfin) %dir %{_sharedstatedir}/jellyfin
%attr(755,jellyfin,jellyfin) %dir %{_sysconfdir}/jellyfin
%attr(750,jellyfin,jellyfin) %dir %{_var}/cache/jellyfin
%attr(-,  jellyfin,jellyfin) %dir %{_var}/log/jellyfin

%license LICENSE


%files server-lowports
%{_unitdir}/jellyfin.service.d/jellyfin-server-lowports.conf

%pre server
getent group jellyfin >/dev/null || groupadd -r jellyfin
getent passwd jellyfin >/dev/null || \
    useradd -r -g jellyfin -d %{_sharedstatedir}/jellyfin -s /sbin/nologin \
    -c "Jellyfin default user" jellyfin
exit 0

%post server
# Move existing configuration cache and logs to their new locations and symlink them.
if [ $1 -gt 1 ] ; then
    service_state=$(systemctl is-active jellyfin.service)
    if [ "${service_state}" = "active" ]; then
        systemctl stop jellyfin.service
    fi
    if [ ! -L %{_sharedstatedir}/jellyfin/config ]; then
        mv %{_sharedstatedir}/jellyfin/config/* %{_sysconfdir}/jellyfin/
        rmdir %{_sharedstatedir}/jellyfin/config
        ln -sf %{_sysconfdir}/jellyfin  %{_sharedstatedir}/jellyfin/config
    fi
    if [ ! -L %{_sharedstatedir}/jellyfin/logs ]; then
        mv %{_sharedstatedir}/jellyfin/logs/* %{_var}/log/jellyfin
        rmdir %{_sharedstatedir}/jellyfin/logs
        ln -sf %{_var}/log/jellyfin  %{_sharedstatedir}/jellyfin/logs
    fi
    if [ ! -L %{_sharedstatedir}/jellyfin/cache ]; then
        mv %{_sharedstatedir}/jellyfin/cache/* %{_var}/cache/jellyfin
        rmdir %{_sharedstatedir}/jellyfin/cache
        ln -sf %{_var}/cache/jellyfin  %{_sharedstatedir}/jellyfin/cache
    fi
    if [ "${service_state}" = "active" ]; then
        systemctl start jellyfin.service
    fi
fi
%systemd_post jellyfin.service

%preun server
%systemd_preun jellyfin.service

%postun server
%systemd_postun_with_restart jellyfin.service

%changelog
* Sat Aug 13 2022 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.8.4; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.8.4
* Mon Aug 01 2022 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.8.3; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.8.3
* Mon Aug 01 2022 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.8.2; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.8.2
* Sun Jun 26 2022 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.8.1; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.8.1
* Fri Jun 10 2022 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.8.0; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.8.0
* Mon Nov 29 2021 Brian J. Murrell <brian@interlinx.bc.ca>
- Add jellyfin-server-lowports.service drop-in in a server-lowports
  subpackage to allow binding to low ports
* Fri Dec 04 2020 Jellyfin Packaging Team <packaging@jellyfin.org>
- Forthcoming stable release
* Mon Jul 27 2020 Jellyfin Packaging Team <packaging@jellyfin.org>
- Forthcoming stable release
* Mon Mar 23 2020 Jellyfin Packaging Team <packaging@jellyfin.org>
- Forthcoming stable release
* Fri Oct 11 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.5.0; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.5.0
* Sat Aug 31 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.4.0; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.4.0
* Wed Jul 24 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.3.7; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.3.7
* Sat Jul 06 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.3.6; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.3.6
* Sun Jun 09 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.3.5; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.3.5
* Thu Jun 06 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.3.4; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.3.4
* Fri May 17 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.3.3; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.3.3
* Tue Apr 30 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.3.2; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.3.2
* Sat Apr 20 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.3.1; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.3.1
* Fri Apr 19 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.3.0; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.3.0
