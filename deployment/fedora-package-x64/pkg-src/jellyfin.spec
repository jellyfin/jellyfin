%global         debug_package %{nil}
# Set the dotnet runtime
%if 0%{?fedora}
%global         dotnet_runtime  fedora-x64
%else
%global         dotnet_runtime  centos-x64
%endif

Name:           jellyfin
Version:        10.3.0
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
%{__install} -D -m 0644 %{SOURCE6} %{buildroot}%{_prefix}/lib/firewalld/service/%{name}.xml

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
%{_prefix}/lib/firewalld/service/%{name}.xml
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
* Fri Apr 19 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- New upstream version 10.3.0; release changelog at https://github.com/jellyfin/jellyfin/releases/tag/v10.3.0
* Thu Feb 28 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- jellyfin:
- PR968 Release 10.2.z copr autobuild
- PR964 Install the dotnet runtime package in Fedora build
- PR979 Build Package releases without debug turned on
- PR990 Fix slow local image validation
- PR991 Fix the ffmpeg compatibility
- PR992 Add Debian armhf (Raspberry Pi) build plus crossbuild
- PR998 Set EnableRaisingEvents to true for processes that require it
- PR1017 Set ffmpeg+ffprobe paths in Docker container
- jellyfin-web:
- PR152 Go back on Media stop
- PR156 Fix volume slider not working on nowplayingbar
* Wed Feb 20 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- jellyfin:
- PR920 Fix cachedir missing from Docker container
- PR924 Use the movie name instead of folder name
- PR933 Semi-revert to prefer old movie grouping behaviour
- PR948 Revert movie matching (supercedes PR933, PR924, PR739)
- PR960 Use jellyfin/ffmpeg image
- jellyfin-web:
- PR136 Re-add OpenSubtitles configuration page
- PR137 Replace HeaderEmbyServer with HeaderJellyfinServer on plugincatalog
- PR138 Remove left-over JS for Customize Home Screen
- PR141 Exit fullscreen automatically after video playback ends
* Fri Feb 15 2019 Jellyfin Packaging Team <packaging@jellyfin.org>
- jellyfin:
- PR452 Use EF Core for Activity database
- PR535 Clean up streambuilder
- PR655 Support trying local branches in submodule
- PR656 Do some logging in MediaInfoService
- PR657 Remove conditions that are always true/false
- PR661 Fix NullRef from progress report
- PR663 Use TagLibSharp Nuget package
- PR664 Revert "Fix segment_time_delta for ffmpeg 4.1"
- PR666 Add cross-platform build for arm64
- PR668 Return Audio objects from MusicAlbum.Tracks
- PR671 Set EnableRaisingEvents correctly
- PR672 Remove unconditional caching, modified since header and use ETags
- PR677 Fix arm32 Docker
- PR681 Fix Windows build script errors + pin ffmpeg to 4.0
- PR686 Disable some StyleCop warnings
- PR687 Fix some analyzer warnings
- PR689 Fix RPM package build for fedora
- PR702 Fix debug build on windows
- PR706 Make another docker layer reusable
- PR709 Fix always null expressions
- PR710 Fix a spelling mistake
- PR711 Remove remnants of system events
- PR713 Fix empty statement in DidlBuilder.cs
- PR716 Remove more compile time warnings
- PR721 Change image dimentions from double to int
- PR723 Minor improvements to db code
- PR724 Move Skia back into it's own project
- PR726 Clean up IFileSystem wrappers around stdlib.
- PR727 Change default aspect ratio to 2/3 from 0
- PR728 Use ffmpeg from jrottenberg/ffmpeg
- PR732 Reworked LocalizationManager to load data async
- PR733 Remove unused function
- PR734 Fix more analyzer warnings
- PR736 Start startup tasks async
- PR737 Add AssemblyInfo for Jellyfin.Drawing.Skia
- PR739 Change multi version logic for movies
- PR740 Remove code for pre-installed plugins & properly check if file exists
- PR756 Make cache dir configurable
- PR757 Fix default aspect ratio
- PR758 Add password field to initial setup
- PR764 Remove dead code, made some functions properly async
- PR769 Fix conditions where the ! was swallowed in #726
- PR774 reimplement support for plugin repository
- PR782 Remove commented file MediaBrowser.LocalMetadata.Savers.PersonXmlSaver
- PR783 Update builds to use #749 and #756
- PR788 Fix more warnings
- PR794 Remove MoreLINQ
- PR797 Fix all warnings
- PR798 Cleanup around the api endpoints
- PR800 Add CentOS and update rpm spec for the cachedir option
- PR802 Fix build error
- PR804 Handle new option parser properly
- PR805 Add weblate translation status to README
- PR807 Fix restart script in OS packages
- PR810 Fix loading of rating files
- PR812 Fix up the explicit docs links in the README
- PR819 Some small changes in Device.cs and DidlBuilder.cs
- PR822 Complete rename ImageSize -> ImageDimensions
- PR824 Improved Docker pkgbuild
- PR831 Move some arrays to generics
- PR833 Add await to GetCountries in LocalizationService
- PR834 Add donation badge and reorganize badges
- PR838 Quick style fix
- PR840 Fix more warnings
- PR841 Fix OC badge to all and add forum badge
- PR842 Use VAAPI-enabled ffmpeg
- PR852 Use SQLitePCL.pretty.netstandard on NuGet
- PR853 Fix poor handling of cache directories
- PR864 Add support for ZIP plugin archives
- PR868 Fix audio streaming via BaseProgressiveStreamingService
- PR869 Remove DLL support and require all packages/plugins to be zip archives
- PR872 Fix potential NullReferenceException
- PR899: DLNA: Fix race condition leading to missing device names
- PR890 Drop ETag and use Last-Modified header
- PR892: Add jellyfin-ffmpeg and versioning to package deps
- PR901: Properly dispose HttpWebResponse when the request failed to avoid 'too many open files'
- PR909: Fix docker arm builds
- PR910: Enhance Dockerfiles
- PR911: Checkout submodules in Docker Hub hook
- jellyfin-web:
- PR51 remove more code for sync and camera roll
- PR56 Use English for fallback translations and clean up language files
- PR58 Css slider fixes
- PR62 remove BOM markers
- PR65 Fix profile image not being shown on profile page
- PR73 Dev sync
- PR74 Add download menu option to media items
- PR75 User profile fixes
- PR76 Fix syntax error caused by deminification
- PR79 Remove unused Connect related from the frontend
- PR80 Remove games
- PR92 Added frontend support for a password field on setup
- PR94 Update british strings
- PR95 add display language option back
- PR112 Removed seasonal theme support
- PR116 Consolidate all strings into a single file per language
- PR117 Fix volume slider behavior
- PR118 Enable and fix PiP for Safari
- PR119 Make the toggle track visible on all themes
- PR121 Fix syntax error in site.js
- PR127 Change sharedcomponents module to core
- PR135 Make sure fallback culture is always available
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
- PR646: Fix infinite loop bug on subtitle.m3u8 request
- PR655: Support trying local branches in submodule
- PR661: Fix NullRef from progress report
- PR666: Add cross-platform build for arm64
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
- PR52: Fix progress color
- PR53: Fix user tabs color
- PR54: Add back button to server dashboard
* Fri Jan 11 2019 Thomas Büttner <thomas@vergesslicher.tech> - 10.0.2-1
- TODO Changelog for 10.0.2
