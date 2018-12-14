#!/bin/bash

# Build a Jellyfin .deb file with Docker on Linux
# Places the output .deb file in the parent directory

set -o xtrace
set -o errexit
set -o pipefail
set -o nounset

date="$( date +%s )"
curdir="$( pwd )"
tmpdir="$( mktemp -d )"
curuser="$( whoami )"

docker build ${curdir} --tag jellyfin-debuild-${date} --file ${curdir}/Dockerfile.debian_package
docker run --volume ${tmpdir}:/temp --interactive --tty jellyfin-debuild-${date} cp --recursive /dist /temp/
docker image rm jellyfin-debuild-${date} --force
sudo chown --recursive ${curuser} ${tmpdir}
mv ${tmpdir}/dist/*.deb ${curdir}/../
rm --recursive --force ${tmpdir}
