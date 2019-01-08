%global         debug_package %{nil}
# jellyfin tag to package
%global         gittag v10.0.0
# Taglib-sharp commit of the submodule since github archive doesn't include submodules
%global         taglib_commit ee5ab21742b71fd1b87ee24895582327e9e04776
%global         taglib_shortcommit %(c=%{taglib_commit}; echo ${c:0:7})

Name:           jellyfin
Version:        10.0.0
Release:        1%{?dist}
Summary:        The Free Software Media Browser.
License:        GPLv2
URL:            https://jellyfin.media
Source0:        https://github.com/%{name}/%{name}/archive/%{gittag}.tar.gz
Source1:        jellyfin.service
Source2:        jellyfin.env
Source3:        jellyfin.sudoers
Source4:        restart.sh
Source5:        https://github.com/mono/taglib-sharp/archive/%{taglib_commit}/taglib-sharp-%{taglib_shortcommit}.tar.gz
Source6:        update-db.sh
Source7:        jellyfin-firewalld.xml

%{?systemd_requires}
BuildRequires:  systemd
Requires(pre):  shadow-utils
BuildRequires:  libcurl-devel, fontconfig-devel, freetype-devel, openssl-devel, glibc-devel, libicu-devel
Requires:       libcurl, fontconfig, freetype, openssl, glibc libicu
# Requirements not packaged in main repos
# COPR @dotnet-sig/dotnet
BuildRequires:  dotnet-sdk-2.2
# RPMfusion free
Requires:       ffmpeg

# For the update-db-paths.sh script to fix emby paths to jellyfin
%{?fedora:Recommends: sqlite}

# Fedora has openssl1.1 which is incompatible with dotnet 
%{?fedora:Requires: compat-openssl10}
# Disable Automatic Dependency Processing for Centos
%{?el7:AutoReqProv: no}

%description
Jellyfin is a free software media system that puts you in control of managing and streaming your media.


%prep
%autosetup -n %{name}-%{version}
pushd ThirdParty
    tar xf %{S:5}
    rm -rf taglib-sharp
    mv taglib-sharp-%{taglib_commit} taglib-sharp
popd

%build
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
dotnet build --runtime linux-x64

%install
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
dotnet publish --configuration Release --output='%{buildroot}%{_libdir}/jellyfin' --self-contained --runtime linux-x64
%{__install} -D -m 0644 LICENSE %{buildroot}%{_datadir}/licenses/%{name}/LICENSE
%{__install} -D -m 0644 debian/conf/jellyfin.service.conf %{buildroot}%{_sysconfdir}/systemd/system/%{name}.service.d/override.conf
%{__install} -D -m 0644 debian/conf/logging.json %{buildroot}%{_sysconfdir}/%{name}/logging.json
%{__mkdir} -p %{buildroot}%{_bindir}
tee %{buildroot}%{_bindir}/jellyfin << EOF
#!/bin/sh
exec %{_libdir}/%{name}/%{name} \${@}
EOF
%{__mkdir} -p %{buildroot}%{_sharedstatedir}/jellyfin
%{__mkdir} -p %{buildroot}%{_sysconfdir}/%{name}
%{__mkdir} -p %{buildroot}%{_var}/log/jellyfin

%{__install} -D -m 0644 %{SOURCE1} %{buildroot}%{_unitdir}/%{name}.service
%{__install} -D -m 0644 %{SOURCE2} %{buildroot}%{_sysconfdir}/sysconfig/%{name}
%{__install} -D -m 0600 %{SOURCE3} %{buildroot}%{_sysconfdir}/sudoers.d/%{name}-sudoers
%{__install} -D -m 0750 %{SOURCE4} %{buildroot}%{_libexecdir}/%{name}/restart.sh
%{__install} -D -m 0755 %{SOURCE6} %{buildroot}%{_datadir}/%{name}/update-db-paths.sh
%{__install} -D -m 0755 %{SOURCE7} %{buildroot}%{_prefix}/lib/firewalld/service/%{name}.xml

%files
%{_libdir}/%{name}/dashboard-ui/*
%attr(755,root,root) %{_bindir}/%{name}
%attr(644,root,root) %{_libdir}/%{name}/*.json
%attr(644,root,root) %{_libdir}/%{name}/*.pdb
%attr(755,root,root) %{_libdir}/%{name}/*.dll
%attr(755,root,root) %{_libdir}/%{name}/*.so
%attr(755,root,root) %{_libdir}/%{name}/*.a
%attr(755,root,root) %{_libdir}/%{name}/createdump
%attr(755,root,root) %{_libdir}/%{name}/jellyfin
%attr(644,root,root) %{_libdir}/%{name}/sosdocsunix.txt
%attr(644,root,root) %{_unitdir}/%{name}.service
%attr(755,root,root) %{_datadir}/%{name}/update-db-paths.sh
%attr(755,root,root) %{_libexecdir}/%{name}/restart.sh
%attr(644,root,root) %{_prefix}/lib/firewalld/service/%{name}.xml
%attr(755,jellyfin,jellyfin) %dir %{_sysconfdir}/%{name}
%config %{_sysconfdir}/sysconfig/%{name}
%config(noreplace) %attr(600,root,root) %{_sysconfdir}/sudoers.d/%{name}-sudoers
%config(noreplace) %{_sysconfdir}/systemd/system/%{name}.service.d/override.conf
%config(noreplace) %attr(644,jellyfin,jellyfin) %{_sysconfdir}/%{name}/logging.json
%attr(-,jellyfin,jellyfin) %dir %{_sharedstatedir}/jellyfin
%attr(-,jellyfin,jellyfin) %dir %{_var}/log/jellyfin
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
# Move existing configuration to /etc/jellyfin and symlink config to /etc/jellyfin
if [ $1 -gt 1 ] ; then
    if [ ! -L %{_sharedstatedir}/%{name}/config ]; then
        service_state=$(systemctl is-active jellyfin.service)
        if [ "${service_state}" = "active" ]; then
            systemctl stop jellyfin.service
        fi
        mv %{_sharedstatedir}/%{name}/config/* %{_sysconfdir}/%{name}/
        rmdir %{_sharedstatedir}/%{name}/config
        ln -sf %{_sysconfdir}/%{name}  %{_sharedstatedir}/%{name}/config
        if [ "${service_state}" = "active" ]; then
            systemctl start jellyfin.service
        fi
    fi
fi
%systemd_post jellyfin.service

%preun
%systemd_preun jellyfin.service

%postun
%systemd_postun_with_restart jellyfin.service

%changelog
* Mon Jan 07 2019 Thomas Büttner <thomas@vergesslicher.tech> - 10.0.0-1
- Bump version to 10.0.0
- Add logging and config directories
- Add %post script to move exitsting config to /etc/jellyfin and symlink it.

* Sat Jan 05 2019 Thomas Büttner <thomas@vergesslicher.tech> - 3.5.2-5
- Add firewalld service.xml

* Sat Jan 05 2019 Thomas Büttner <thomas@vergesslicher.tech> - 3.5.2-4
- Re-added sudoers policy

* Sat Jan 05 2019 Thomas Büttner <thomas@vergesslicher.tech> - 3.5.2-3
- Added script for database migration

* Fri Jan 04 2019 Thomas Büttner <thomas@vergesslicher.tech> - 3.5.2-2
- Moved sudoers policy and added a note for In-App service control
- Set Restart=on-failure in jellyfin.service

* Thu Jan 03 2019 Thomas Büttner <thomas@vergesslicher.tech> - 3.5.2-1
- Initial RPM package
