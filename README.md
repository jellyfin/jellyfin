<h1 align="center">Jellyfin</h1>
<h3 align="center">The Free Software Media System</h3>

---

<p align="center">
<img alt="Logo Banner" src="https://raw.githubusercontent.com/jellyfin/jellyfin-ux/master/branding/SVG/banner-logo-solid.svg?sanitize=true"/>
<br/>
<br/>
<a href="https://github.com/jellyfin/jellyfin">
<img alt="GPL 2.0 License" src="https://img.shields.io/github/license/jellyfin/jellyfin.svg"/>
</a>
<a href="https://github.com/jellyfin/jellyfin/releases">
<img alt="Current Release" src="https://img.shields.io/github/release/jellyfin/jellyfin.svg"/>
</a>
<a href="https://translate.jellyfin.org/projects/jellyfin/jellyfin-core/?utm_source=widget">
<img alt="Translation Status" src="https://translate.jellyfin.org/widgets/jellyfin/-/jellyfin-core/svg-badge.svg"/>
</a>
<a href="https://hub.docker.com/r/jellyfin/jellyfin">
<img alt="Docker Pull Count" src="https://img.shields.io/docker/pulls/jellyfin/jellyfin.svg"/>
</a>
<br/>
<a href="https://opencollective.com/jellyfin">
<img alt="Donate" src="https://img.shields.io/opencollective/all/jellyfin.svg?label=backers"/>
</a>
<a href="https://features.jellyfin.org">
<img alt="Submit Feature Requests" src="https://img.shields.io/badge/fider-vote%20on%20features-success.svg"/>
</a>
<a href="https://matrix.to/#/#jellyfinorg:matrix.org">
<img alt="Chat on Matrix" src="https://img.shields.io/matrix/jellyfinorg:matrix.org.svg?logo=matrix"/>
</a>
<a href="https://github.com/jellyfin/jellyfin/releases.atom">
<img alt="Release RSS Feed" src="https://img.shields.io/badge/rss-releases-ffa500?logo=rss" />
</a>
<a href="https://github.com/jellyfin/jellyfin/commits/master.atom">
<img alt="Master Commits RSS Feed" src="https://img.shields.io/badge/rss-commits-ffa500?logo=rss" />
</a>
</p>

---

Jellyfin is a Free Software Media System that puts you in control of managing and streaming your media. It is an alternative to the proprietary Emby and Plex, to provide media from a dedicated server to end-user devices via multiple apps. Jellyfin is descended from Emby's 3.5.2 release and ported to the .NET Core framework to enable full cross-platform support. There are no strings attached, no premium licenses or features, and no hidden agendas: just a team who want to build something better and work together to achieve it. We welcome anyone who is interested in joining us in our quest!

For further details, please see [our documentation page](https://jellyfin.org/docs/). To receive the latest updates, get help with Jellyfin, and join the community, please visit [one of our communication channels](https://jellyfin.org/docs/general/getting-help). For more information about the project, please see our [about page](https://jellyfin.org/docs/general/about).

<strong>Want to get started?</strong><br/>
Check out our <a href="https://jellyfin.org/downloads">downloads page</a> or our <a href="https://jellyfin.org/docs/general/installation/">installation guide</a>, then see our <a href="https://jellyfin.org/docs/general/quick-start">quick start guide</a>. You can also <a href="https://jellyfin.org/docs/general/installation/source">build from source</a>.<br/>

<strong>Something not working right?</strong><br/>
Open an <a href="https://jellyfin.org/docs/general/contributing/issues">Issue</a> on GitHub.<br/>

<strong>Want to contribute?</strong><br/>
Check out our <a href="https://jellyfin.org/contribute">contributing choose-your-own-adventure</a> to see where you can help, then see our <a href="https://jellyfin.org/docs/general/contributing/">contributing guide</a> and our <a href="https://jellyfin.org/docs/general/community-standards">community standards</a>.<br/>

<strong>New idea or improvement?</strong><br/>
Check out our <a href="https://features.jellyfin.org/?view=most-wanted">feature request hub</a>.<br/>

<strong>Don't see Jellyfin in your language?</strong><br/>
Check out our <a href="https://translate.jellyfin.org">Weblate instance</a> to help translate Jellyfin and its subprojects.<br/>

<a href="https://translate.jellyfin.org/engage/jellyfin/?utm_source=widget">
<img src="https://translate.jellyfin.org/widgets/jellyfin/-/jellyfin-web/multi-auto.svg" alt="Detailed Translation Status"/>
</a>

---

## Jellyfin Server

This repository contains the code for Jellyfin's backend server. Note that this is only one of many projects under the Jellyfin GitHub [organization](https://github.com/jellyfin/) on GitHub. If you want to contribute, you can start by checking out our [documentation](https://jellyfin.org/docs/general/contributing/index.html) to see what to work on.

## Server Development

These instructions will help you get set up with a local development environment in order to contribute to this repository. Before you start, please be sure to completely read our [guidelines on development contributions](https://jellyfin.org/docs/general/contributing/development.html). Note that this project is supported on all major operating systems except FreeBSD, which is still incompatible.

### Prerequisites

Before the project can be built, you must first install the [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet) on your system.

Instructions to run this project from the command line are included here, but you will also need to install an IDE if you want to debug the server while it is running. Any IDE that supports .NET 6 development will work, but two options are recent versions of [Visual Studio](https://visualstudio.microsoft.com/downloads/) (at least 2022) and [Visual Studio Code](https://code.visualstudio.com/Download).

[ffmpeg](https://github.com/jellyfin/jellyfin-ffmpeg) will also need to be installed.

### Cloning the Repository

After dependencies have been installed you will need to clone a local copy of this repository. If you just want to run the server from source you can clone this repository directly, but if you are intending to contribute code changes to the project, you should [set up your own fork](https://jellyfin.org/docs/general/contributing/development.html#set-up-your-copy-of-the-repo) of the repository. The following example shows how you can clone the repository directly over HTTPS.

```bash
git clone https://github.com/jellyfin/jellyfin.git
```

### Installing the Web Client

The server is configured to host the static files required for the [web client](https://github.com/jellyfin/jellyfin-web) in addition to serving the backend by default. Before you can run the server, you will need to get a copy of the web client since they are not included in this repository directly.

Note that it is also possible to [host the web client separately](#hosting-the-web-client-separately) from the web server with some additional configuration, in which case you can skip this step.

There are three options to get the files for the web client.

1. Download one of the finished builds from the [Azure DevOps pipeline](https://dev.azure.com/jellyfin-project/jellyfin/_build?definitionId=27). You can download the build for a specific release by looking at the [branches tab](https://dev.azure.com/jellyfin-project/jellyfin/_build?definitionId=27&_a=summary&repositoryFilter=6&view=branches) of the pipelines page.
2. Build them from source following the instructions on the [jellyfin-web repository](https://github.com/jellyfin/jellyfin-web)
3. Get the pre-built files from an existing installation of the server. For example, with a Windows server installation the client files are located at `C:\Program Files\Jellyfin\Server\jellyfin-web`

### Running The Server

The following instructions will help you get the project up and running via the command line, or your preferred IDE.

#### Running With Visual Studio

To run the project with Visual Studio you can open the Solution (`.sln`) file and then press `F5` to run the server.

#### Running With Visual Studio Code

To run the project with Visual Studio Code you will first need to open the repository directory with Visual Studio Code using the `Open Folder...` option.

Second, you need to [install the recommended extensions for the workspace](https://code.visualstudio.com/docs/editor/extension-gallery#_recommended-extensions). Note that extension recommendations are classified as either "Workspace Recommendations" or "Other Recommendations", but only the "Workspace Recommendations" are required.

After the required extensions are installed, you can run the server by pressing `F5`.

#### Running From the Command Line

To run the server from the command line you can use the `dotnet run` command. The example below shows how to do this if you have cloned the repository into a directory named `jellyfin` (the default directory name) and should work on all operating systems.

```bash
cd jellyfin                          # Move into the repository directory
dotnet run --project Jellyfin.Server --webdir /absolute/path/to/jellyfin-web/dist # Run the server startup project
```

A second option is to build the project and then run the resulting executable file directly. When running the executable directly you can easily add command line options. Add the `--help` flag to list details on all the supported command line options.

1. Build the project

```bash
dotnet build                       # Build the project
cd Jellyfin.Server/bin/Debug/net9.0 # Change into the build output directory
```

2. Execute the build output. On Linux, Mac, etc. use `./jellyfin` and on Windows use `jellyfin.exe`.

#### Accessing the Hosted Web Client

If the Server is configured to host the Web Client, and the Server is running, the Web Client can be accessed at `http://localhost:8096` by default.

API documentation can be viewed at `http://localhost:8096/api-docs/swagger/index.html`


### Running from GitHub Codespaces

As Jellyfin will run on a container on a GitHub hosted server, JF needs to handle some things differently.

**NOTE:** Depending on the selected configuration (if you just click 'create codespace' it will create a default configuration one) it might take 20-30 seconds to load all extensions and prepare the environment while VS Code is already open. Just give it some time and wait until you see `Downloading .NET version(s) 7.0.15~x64 ...... Done!` in the output tab.

**NOTE:** If you want to access the JF instance from outside, like with a WebClient on another PC, remember to set the "ports" in the lower VS Code window to public.

**NOTE:** When first opening the server instance with any WebUI, you will be sent to the login instead of the setup page. Refresh the login page once and you should be redirected to the Setup.

There are two configurations for you to choose from.
#### Default - Development Jellyfin Server
This creates a container that has everything to run and debug the Jellyfin Media server but does not setup anything else. Each time you create a new container you have to run through the whole setup again. There is also no ffmpeg, webclient or media preloaded. Use the `.NET Launch (nowebclient)` launch config to start the server.

> Keep in mind that as this has no web client you have to connect to it via an external client. This can be just another codespace container running the WebUI. vuejs does not work from the get-go as it does not support the setup steps.

#### Development Jellyfin Server ffmpeg
this extends the default server with a default installation of ffmpeg6 though the means described here: https://jellyfin.org/docs/general/installation/linux#repository-manual
If you want to install a specific ffmpeg version, follow the comments embedded in the `.devcontainer/Dev - Server Ffmpeg/install.ffmpeg.sh` file.

Use the `ghcs .NET Launch (nowebclient, ffmpeg)` launch config to run with the jellyfin-ffmpeg enabled.


### Running The Tests

This repository also includes unit tests that are used to validate functionality as part of a CI pipeline on Azure. There are several ways to run these tests.

1. Run tests from the command line using `dotnet test`
2. Run tests in Visual Studio using the [Test Explorer](https://docs.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer)
3. Run individual tests in Visual Studio Code using the associated [CodeLens annotation](https://github.com/OmniSharp/omnisharp-vscode/wiki/How-to-run-and-debug-unit-tests)

### Advanced Configuration

The following sections describe some more advanced scenarios for running the server from source that build upon the standard instructions above.

#### Hosting The Web Client Separately

It is not necessary to host the frontend web client as part of the backend server. Hosting these two components separately may be useful for frontend developers who would prefer to host the client in a separate webpack development server for a tighter development loop. See the [jellyfin-web](https://github.com/jellyfin/jellyfin-web#getting-started) repo for instructions on how to do this.

To instruct the server not to host the web content, there is a `nowebclient` configuration flag that must be set. This can be specified using the command line
switch `--nowebclient` or the environment variable `JELLYFIN_NOWEBCONTENT=true`.

Since this is a common scenario, there is also a separate launch profile defined for Visual Studio called `Jellyfin.Server (nowebcontent)` that can be selected from the 'Start Debugging' dropdown in the main toolbar.

**NOTE:** The setup wizard cannot be run if the web client is hosted separately.

---
<p align="center">
This project is supported by:
<br/>
<br/>
<a href="https://www.digitalocean.com"><img src="https://opensource.nyc3.cdn.digitaloceanspaces.com/attribution/assets/SVG/DO_Logo_horizontal_blue.svg" height="50px" alt="DigitalOcean"></a>
    &nbsp;
<a href="https://www.jetbrains.com"><img src="https://gist.githubusercontent.com/anthonylavado/e8b2403deee9581e0b4cb8cd675af7db/raw/fa104b7d73f759d7262794b94569f1b89df41c0b/jetbrains.svg" height="50px" alt="JetBrains logo"></a>
</p>
