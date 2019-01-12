#!/usr/bin/env bash

source ../common.build.sh

VERSION=`get_version ../..`

package_portable ../.. `pwd`/dist/jellyfin_${VERSION}

#TODO setup and maybe change above code to produce the Windows native zip format.
