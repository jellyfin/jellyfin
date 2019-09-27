# Jellyfin Packaging

This directory contains the packaging configuration of Jellyfin for multiple platforms. The specification is below; all package platforms must follow the specification to be compatable with the central `build` script.

## Package List

### Operating System Packages

* `debian-package-x64`: Package for Debian and Ubuntu amd64 systems.
* `fedora-package-x64`: Package for Fedora, CentOS, and Red Hat Enterprise Linux amd64 systems.

### Portable Builds (archives)

* `linux-x64`: Portable binary archive for generic Linux amd64 systems.
* `macos`: Portable binary archive for MacOS amd64 systems.
* `win-x64`: Portable binary archive for Windows amd64 systems.
* `win-x86`: Portable binary archive for Windows i386 systems.

### Other Builds

These builds are not necessarily run from the `build` script, but are present for other platforms.

* `portable`: Compiled `.dll` for use with .NET Core runtime on any system.
* `docker`: Docker manifests for auto-publishing.
* `unraid`: unRaid Docker template; not built by `build` but imported into unRaid directly.
* `windows`: Support files and scripts for Windows CI build.

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

* The `clean` action can be passed a variable as argument 1, named `keep_artifacts`, containing either the value `y` or `n`. It is indended to handle situations when the user runs `build --keep-artifacts` and should be handled intelligently. Usually, this is used to preserve Docker images while still removing temporary directories.

### Output Files

* Upon completion of the defined actions, at least one output file must be created in the `<platform>/pkg-dist` directory.

* Output files will be moved to the directory `jellyfin-build/<platform>` one directory above the repository root upon completion.
