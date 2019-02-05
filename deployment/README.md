# Jellyfin Packaging

This directory contains the packaging configuration of Jellyfin for multiple platforms. The specification is below; all package platforms must follow the specification to be compatable with the central `build` script.

## Package List

### Operating System Packages

* `debian-package-x64`: Package for Debian and Ubuntu amd64 systems.
* `fedora-package-x64`: Package for Fedora, CentOS, and Red Hat Enterprise Linux amd64 systems.

### Portable Builds (archives)

* `debian-x64`: Portable binary archive for Debian amd64 systems.
* `ubuntu-x64`: Portable binary archive for Ubuntu amd64 systems.
* `linux-x64`: Portable binary archive for generic Linux amd64 systems.
* `osx-x64`: Portable binary archive for MacOS amd64 systems.
* `win-x64`: Portable binary archive for Windows amd64 systems.
* `win-x86`: Portable binary archive for Windows i386 systems.

### Other Builds

These builds are not necessarily run from the `build` script, but are present for other platforms.

* `framework`: Compiled `.dll` for use with .NET Core runtime on any system.
* `docker`: Docker manifests for auto-publishing.
* `unraid`: unRaid Docker template; not built by `build` but imported into unRaid directly.
* `win-generic`: Portable binary for generic Windows systems.

## Package Specification

### Dependencies

* If a platform requires additional build dependencies, the required binary names, i.e. to validate `which <binary>`, should be specified in a `dependencies.txt` file inside the platform directory.

* Each dependency should be present on its own line.

### Action Scripts

* Actions are defined in BASH scripts with the name `<action>.sh` within the platform directory.

* The list of valid actions are:

    1. `build`: Builds a set of binaries.
    2. `package`: Assembles the compiled binaries into a package.
    3. `sign`: Performs signing actions on a package.
    4. `publish`: Performs a publishing action for a package.
    5. `clean`: Cleans up any artifacts from the previous actions.

* All package actions are optional, however at least one should generate output files, and any that do should contain a `clean` action.

* Actions are executed in the order specified above, and later actions may depend on former actions.

* Actions except for `clean` should `set -o errexit` to terminate on failed actions.

* The `clean` action should always `exit 0` even if no work is done or it fails.

* The `clean` action can be passed a variable as argument 1, named `keep_artifacts`. It is indended to handle situations when the user runs `build --keep-artifacts` and should be handled intelligently. Usually, this is used to preserve Docker images while still removing temporary directories.

### Output Files

* Upon completion of the defined actions, at least one output file must be created in the `<platform>/pkg-dist` directory.

* Output files will be moved to the directory `jellyfin-build/<platform>` one directory above the repository root upon completion.

### Common Functions

* A number of common functions are defined in `deployment/common.build.sh` for use by platform scripts.

* Each action script should import the common functions to define a number of standard variables.

* The common variables are:

    * `ROOT`: The Jellyfin repostiory root, usually `../..`.
    * `CONFIG`: The .NET config, usually `Release`.
    * `DOTNETRUNTIME`: The .NET `--runtime` value, platform-dependent.
    * `OUTPUT_DIR`: The intermediate output dir, usually `./dist/jellyfin_${VERSION}`.
    * `BUILD_CONTEXT`: The Docker build context, usually `../..`.
    * `DOCKERFILE`: The Dockerfile, usually `Dockerfile` in the platform directory.
    * `IMAGE_TAG`: A tag for the built Docker image.
    * `PKG_DIR`: The final binary output directory for collection, invariably `pkg-dist`.
    * `ARCHIVE_CMD`: The compression/archive command for release archives, usually `tar -xvzf` or `zip`.

#### `get_version`

Reads the version information from `SharedVersion.cs`.

**Arguments:** `ROOT`

#### `build_jellyfin`

Build a standard self-contained binary in the current OS context.

**Arguments:** `ROOT` `CONFIG` `DOTNETRUNTIME` `OUTPUT_DIR`

#### `build_jellyfin_docker`

Build a standard self-contained binary in a Docker image.

**Arguments:** `BUILD_CONTEXT` `DOCKERFILE` `IMAGE_TAG`

#### `clean_jellyfin`

Clean up a build for housekeeping.

**Arguments:** `ROOT` `CONFIG` `OUTPUT_DIR` `PKG_DIR`

#### `package_portable`

Produce a compressed archive.

**Arguments:** `ROOT` `OUTPUT_DIR` `PKG_DIR` `ARCHIVE_CMD`

