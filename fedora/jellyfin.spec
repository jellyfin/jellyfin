%global         debug_package %{nil}
# Set the dotnet runtime
%if 0%{?fedora}
%global         dotnet_runtime  fedora-x64
%else
%global         dotnet_runtime  centos-x64
%endif

Name:           jellyfin
Version:        10.6.0
Release:        1%{?dist}
Summary:        The Free Software Media System
License:        GPLv3
URL:            https://jellyfin.org
# Jellyfin Server tarball created by `make -f .copr/Makefile srpm`, real URL ends with `v%{version}.tar.gz`
Source0:        jellyfin-server-%{version}.tar.gz
Source11:       jellyfin.service
Source12:       jellyfin.env
Source13:       jellyfin.sudoers
Source14:       restart.sh
Source15:       jellyfin.override.conf
Source16:       jellyfin-firewalld.xml

%{?systemd_requires}
BuildRequires:  systemd
BuildRequires:  libcurl-devel, fontconfig-devel, freetype-devel, openssl-devel, glibc-devel, libicu-devel
# Requirements not packaged in main repos
# COPR @dotnet-sig/dotnet or
# https://packages.microsoft.com/rhel/7/prod/
BuildRequires:  dotnet-runtime-3.1, dotnet-sdk-3.1
Requires: %{name}-server = %{version}-%{release}, %{name}-web >= 10.6, %{name}-web < 10.7
# Disable Automatic Dependency Processing
AutoReqProv:    no

%description
Jellyfin is a free software media system that puts you in control of managing and streaming your media.

%package server
# RPMfusion free
Summary:        The Free Software Media System Server backend
Requires(pre):  shadow-utils
Requires:       ffmpeg
Requires:       libcurl, fontconfig, freetype, openssl, glibc libicu

%description server
The Jellyfin media server backend.

%prep
%autosetup -n jellyfin-server-%{version} -b 0

%build

%install
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
dotnet publish --configuration Release --output='%{buildroot}%{_libdir}/jellyfin' --self-contained --runtime %{dotnet_runtime} \
    "-p:GenerateDocumentationFile=false;DebugSymbols=false;DebugType=none" Jellyfin.Server
%{__install} -D -m 0644 LICENSE %{buildroot}%{_datadir}/licenses/jellyfin/LICENSE
%{__install} -D -m 0644 %{SOURCE15} %{buildroot}%{_sysconfdir}/systemd/system/jellyfin.service.d/override.conf
%{__install} -D -m 0644 Jellyfin.Server/Resources/Configuration/logging.json %{buildroot}%{_sysconfdir}/jellyfin/logging.json
%{__mkdir} -p %{buildroot}%{_bindir}
tee %{buildroot}%{_bindir}/jellyfin << EOF
#!/bin/sh
exec %{_libdir}/jellyfin/jellyfin \${@}
EOF
%{__mkdir} -p %{buildroot}%{_sharedstatedir}/jellyfin
%{__mkdir} -p %{buildroot}%{_sysconfdir}/jellyfin
%{__mkdir} -p %{buildroot}%{_var}/log/jellyfin
%{__mkdir} -p %{buildroot}%{_var}/cache/jellyfin

%{__install} -D -m 0644 %{SOURCE11} %{buildroot}%{_unitdir}/jellyfin.service
%{__install} -D -m 0644 %{SOURCE12} %{buildroot}%{_sysconfdir}/sysconfig/jellyfin
%{__install} -D -m 0600 %{SOURCE13} %{buildroot}%{_sysconfdir}/sudoers.d/jellyfin-sudoers
%{__install} -D -m 0755 %{SOURCE14} %{buildroot}%{_libexecdir}/jellyfin/restart.sh
%{__install} -D -m 0644 %{SOURCE16} %{buildroot}%{_prefix}/lib/firewalld/services/jellyfin.xml

%files server
%attr(755,root,root) %{_bindir}/jellyfin
%{_libdir}/jellyfin/*.json
%{_libdir}/jellyfin/*.dll
%{_libdir}/jellyfin/*.so
%{_libdir}/jellyfin/*.a
%{_libdir}/jellyfin/createdump
# Needs 755 else only root can run it since binary build by dotnet is 722
%attr(755,root,root) %{_libdir}/jellyfin/jellyfin
%{_libdir}/jellyfin/SOS_README.md
%{_unitdir}/jellyfin.service
%{_libexecdir}/jellyfin/restart.sh
%{_prefix}/lib/firewalld/services/jellyfin.xml
%attr(755,jellyfin,jellyfin) %dir %{_sysconfdir}/jellyfin
%config %{_sysconfdir}/sysconfig/jellyfin
%config(noreplace) %attr(600,root,root) %{_sysconfdir}/sudoers.d/jellyfin-sudoers
%config(noreplace) %{_sysconfdir}/systemd/system/jellyfin.service.d/override.conf
%config(noreplace) %attr(644,jellyfin,jellyfin) %{_sysconfdir}/jellyfin/logging.json
%attr(750,jellyfin,jellyfin) %dir %{_sharedstatedir}/jellyfin
%attr(-,jellyfin,jellyfin) %dir %{_var}/log/jellyfin
%attr(750,jellyfin,jellyfin) %dir %{_var}/cache/jellyfin
%{_datadir}/licenses/jellyfin/LICENSE

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
