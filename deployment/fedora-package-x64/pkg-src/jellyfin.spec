%global         debug_package %{nil}
# jellyfin tag to package
%global         gittag v10.1.0
# Taglib-sharp commit of the submodule since github archive doesn't include submodules
%global         taglib_commit ee5ab21742b71fd1b87ee24895582327e9e04776
%global         taglib_shortcommit %(c=%{taglib_commit}; echo ${c:0:7})

AutoReq:        no
Name:           jellyfin
Version:        10.1.0
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

%build

%install
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
dotnet publish --configuration Release --output='%{buildroot}%{_libdir}/jellyfin' --self-contained --runtime fedora-x64 Jellyfin.Server
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

%{__install} -D -m 0644 %{SOURCE1} %{buildroot}%{_unitdir}/%{name}.service
%{__install} -D -m 0644 %{SOURCE2} %{buildroot}%{_sysconfdir}/sysconfig/%{name}
%{__install} -D -m 0600 %{SOURCE3} %{buildroot}%{_sysconfdir}/sudoers.d/%{name}-sudoers
%{__install} -D -m 0755 %{SOURCE4} %{buildroot}%{_libexecdir}/%{name}/restart.sh
%{__install} -D -m 0644 %{SOURCE6} %{buildroot}%{_prefix}/lib/firewalld/service/%{name}.xml

%files
%{_libdir}/%{name}/jellyfin-web/*
%attr(755,root,root) %{_bindir}/%{name}
%{_libdir}/%{name}/*.json
%{_libdir}/%{name}/*.pdb
%{_libdir}/%{name}/*.dll
%{_libdir}/%{name}/*.so
%{_libdir}/%{name}/*.a
%{_libdir}/%{name}/createdump
# Needs 755 else only root can run it since binary build by dotnet is 722
%attr(755,root,root) %{_libdir}/%{name}/jellyfin
%{_libdir}/%{name}/sosdocsunix.txt
%{_unitdir}/%{name}.service
%{_libexecdir}/%{name}/restart.sh
%{_prefix}/lib/firewalld/service/%{name}.xml
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
* Sun Jan 20 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- jellyfin:
- PR335 Build scripts and build system consolidation.
- PR424 add jellyfin-web as submodule
- PR455 Cleanup some small things
- PR458 Clean up several minor issues and add TODOs
- PR506 Removing tabs and trailing whitespace
- PR508 Update internal versioning and user agents.
- PR516 Remove useless properties from IEnvironmentInfo
- PR520 Fix potential bug where aspect ratio would be incorrectly calculated
- PR534 Add linux-arm and linux-arm64 native NuGet dependency.
- PR540 Update Emby API keys to our own
- PR541 Change ItemId to Guid in ProviderManager
- PR556 Fix "Password Reset by PIN" page
- PR562 Fix error with uppercase photo extension and fix typo in a log line
- PR563 Update dev from master
- PR566 Avoid printing stacktrace when bind to port 1900 fails
- PR567 Shutdown gracefully when recieving a termination signal
- PR571 Add more NuGet metadata properties
- PR575 Reformat all C# server code to conform with code standards
- PR576 Add code analysers for debug builds
- PR580 Fix Docker build
- PR582 Replace custom image parser with Skia
- PR587 Add nuget info to Emby.Naming
- PR589 Ensure config and log folders exist
- PR596 Fix indentation for xml files
- PR598 Remove MediaBrowser.Text for license violations and hackiness
- PR606 Slim down docker image
- PR613 Update MediaEncoding
- PR616 Add Swagger documentation
- PR619 Really slim down Docker container
- PR621 Minor improvements to library scan code
- PR622 Add unified build script and bump_version script
- PR623 Replaced injections of ILogger with ILoggerFactory
- PR625 Update taglib-sharp
- PR626 Fix extra type name in parameter, add out keyword
- PR627 Use string for ApplicationVersion
- PR628 Update Product Name (User-Agent)
- PR629 Fix subtitle converter misinterpreting 0 valued endTimeTicks
- PR631 Cleanup ImageProcessor and SkiaEncoder
- PR634 Replace our TVDB key with @drakus72's which is V1
- PR636 Allow subtitle extraction and conversion in direct streaming
- PR637 Remove unused font
- PR638 Removed XmlTv testfiles and nuget install
- PR639 Fix segment_time_delta for ffmpeg 4.1
- PR646: Fix infinite loop bug on subtitle.m3u8 request
- PR655: Support trying local branches in submodule
- jellyfin-web:
- PR1: Change webcomponents to non-minified version
- PR4: Fix user profile regression
- PR6: Make icon into proper ico and large PNG
- PR7: Fix firefox failing to set password for users with no password set
- PR8: Remove premiere stuff and fix crashes caused by earlier removals
- PR12: Fix return from PIN reset to index.html
- PR13: Send android clients to select server before login
- PR14: Reimplement page to add server
- PR16: Fix spinning circle at the end of config wizard
- PR17: Fix directorybrower not resetting scroll
- PR19: Set union merge for CONTRIBUTORS.md
- PR20: Show album thumbnail and artist image in page itemdetail
- PR26: Make the card titles clickable
- PR27: Stop pagination and adding a library from being able to trigger multiple times
- PR28: Add transparent nav bar to BlueRadiance theme CSS
- PR29: Clean up imageuploader
- PR30: Remove iap and simplify registrationservices
- PR36: Open videos in fullscreen on android devices
- PR37: Remove broken features from web interface
- PR38: Fix inconsistent UI coloring around settings drawer
- PR39: Remove back button from dashboard and metadata manager
- PR42: Fix Home backdrop not loading
- PR43: Filter videos by audio stream language
- PR44: Remove filter from library collection type options
- PR45: Fix data-backbutton logic
- PR46: Minor changes to navbar elements
- PR48: Remove Sync code
* Fri Jan 11 2019 Thomas Büttner <thomas@vergesslicher.tech> - 10.0.2-1
- TODO Changelog for 10.0.2
