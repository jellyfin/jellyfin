%global         debug_package %{nil}
# Set the dotnet runtime
%if 0%{?fedora}
%global         dotnet_runtime  fedora-x64
%else
%global         dotnet_runtime  centos-x64
%endif

Name:           jellyfin
Version:        10.5.0
Release:        1%{?dist}
Summary:        The Free Software Media Browser
License:        GPLv2
URL:            https://jellyfin.media
Source0:        %{name}-%{version}.tar.gz
Source1:        jellyfin.service
Source2:        jellyfin.env
Source3:        jellyfin.sudoers
Source4:        restart.sh
Source5:        jellyfin.override.conf
Source6:        jellyfin-firewalld.xml

%{?systemd_requires}
BuildRequires:  systemd
Requires(pre):  shadow-utils
BuildRequires:  libcurl-devel, fontconfig-devel, freetype-devel, openssl-devel, glibc-devel, libicu-devel
Requires:       libcurl, fontconfig, freetype, openssl, glibc libicu
# Requirements not packaged in main repos
# COPR @dotnet-sig/dotnet
BuildRequires:  dotnet-runtime-2.2, dotnet-sdk-2.2
# RPMfusion free
Requires:       ffmpeg

# Fedora has openssl1.1 which is incompatible with dotnet 
%{?fedora:Requires: compat-openssl10}

# Disable Automatic Dependency Processing
AutoReqProv:    no

%description
Jellyfin is a free software media system that puts you in control of managing and streaming your media.


%prep
%autosetup -n %{name}-%{version}

%build

%install
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
dotnet publish --configuration Release --output='%{buildroot}%{_libdir}/jellyfin' --self-contained --runtime %{dotnet_runtime} \
    "-p:GenerateDocumentationFile=false;DebugSymbols=false;DebugType=none" Jellyfin.Server
%{__install} -D -m 0644 LICENSE %{buildroot}%{_datadir}/licenses/%{name}/LICENSE
%{__install} -D -m 0644 %{SOURCE5} %{buildroot}%{_sysconfdir}/systemd/system/%{name}.service.d/override.conf
%{__install} -D -m 0644 Jellyfin.Server/Resources/Configuration/logging.json %{buildroot}%{_sysconfdir}/%{name}/logging.json
%{__mkdir} -p %{buildroot}%{_bindir}
tee %{buildroot}%{_bindir}/jellyfin << EOF
#!/bin/sh
exec %{_libdir}/%{name}/%{name} \${@}
EOF
%{__mkdir} -p %{buildroot}%{_sharedstatedir}/jellyfin
%{__mkdir} -p %{buildroot}%{_sysconfdir}/%{name}
%{__mkdir} -p %{buildroot}%{_var}/log/jellyfin
%{__mkdir} -p %{buildroot}%{_var}/cache/jellyfin

%{__install} -D -m 0644 %{SOURCE1} %{buildroot}%{_unitdir}/%{name}.service
%{__install} -D -m 0644 %{SOURCE2} %{buildroot}%{_sysconfdir}/sysconfig/%{name}
%{__install} -D -m 0600 %{SOURCE3} %{buildroot}%{_sysconfdir}/sudoers.d/%{name}-sudoers
%{__install} -D -m 0755 %{SOURCE4} %{buildroot}%{_libexecdir}/%{name}/restart.sh
%{__install} -D -m 0644 %{SOURCE6} %{buildroot}%{_prefix}/lib/firewalld/services/%{name}.xml

%files
%{_libdir}/%{name}/jellyfin-web/*
%attr(755,root,root) %{_bindir}/%{name}
%{_libdir}/%{name}/*.json
%{_libdir}/%{name}/*.dll
%{_libdir}/%{name}/*.so
%{_libdir}/%{name}/*.a
%{_libdir}/%{name}/createdump
# Needs 755 else only root can run it since binary build by dotnet is 722
%attr(755,root,root) %{_libdir}/%{name}/jellyfin
%{_libdir}/%{name}/sosdocsunix.txt
%{_unitdir}/%{name}.service
%{_libexecdir}/%{name}/restart.sh
%{_prefix}/lib/firewalld/services/%{name}.xml
%attr(755,jellyfin,jellyfin) %dir %{_sysconfdir}/%{name}
%config %{_sysconfdir}/sysconfig/%{name}
%config(noreplace) %attr(600,root,root) %{_sysconfdir}/sudoers.d/%{name}-sudoers
%config(noreplace) %{_sysconfdir}/systemd/system/%{name}.service.d/override.conf
%config(noreplace) %attr(644,jellyfin,jellyfin) %{_sysconfdir}/%{name}/logging.json
%attr(750,jellyfin,jellyfin) %dir %{_sharedstatedir}/jellyfin
%attr(-,jellyfin,jellyfin) %dir %{_var}/log/jellyfin
%attr(750,jellyfin,jellyfin) %dir %{_var}/cache/jellyfin
%if 0%{?fedora}
%license LICENSE
%else
%{_datadir}/licenses/%{name}/LICENSE
%endif

%pre
getent group jellyfin >/dev/null || groupadd -r jellyfin
getent passwd jellyfin >/dev/null || \
    useradd -r -g jellyfin -d %{_sharedstatedir}/jellyfin -s /sbin/nologin \
    -c "Jellyfin default user" jellyfin
exit 0

%post
# Move existing configuration cache and logs to their new locations and symlink them.
if [ $1 -gt 1 ] ; then
    service_state=$(systemctl is-active jellyfin.service)
    if [ "${service_state}" = "active" ]; then
        systemctl stop jellyfin.service
    fi
    if [ ! -L %{_sharedstatedir}/%{name}/config ]; then
        mv %{_sharedstatedir}/%{name}/config/* %{_sysconfdir}/%{name}/
        rmdir %{_sharedstatedir}/%{name}/config
        ln -sf %{_sysconfdir}/%{name}  %{_sharedstatedir}/%{name}/config
    fi
    if [ ! -L %{_sharedstatedir}/%{name}/logs ]; then
        mv %{_sharedstatedir}/%{name}/logs/* %{_var}/log/jellyfin
        rmdir %{_sharedstatedir}/%{name}/logs
        ln -sf %{_var}/log/jellyfin  %{_sharedstatedir}/%{name}/logs
    fi
    if [ ! -L %{_sharedstatedir}/%{name}/cache ]; then
        mv %{_sharedstatedir}/%{name}/cache/* %{_var}/cache/jellyfin
        rmdir %{_sharedstatedir}/%{name}/cache
        ln -sf %{_var}/cache/jellyfin  %{_sharedstatedir}/%{name}/cache
    fi
    if [ "${service_state}" = "active" ]; then
        systemctl start jellyfin.service
    fi
fi
%systemd_post jellyfin.service

%preun
%systemd_preun jellyfin.service

%postun
%systemd_postun_with_restart jellyfin.service

%changelog
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
