#!/usr/bin/env bash

git submodule update --init --recursive

pushd ../Jellyfin.Versioning
#TODO Uncomment the next line with PR is merged.
#./update-version
popd

# Execute all build.sh and package.sh and sign.sh scripts in every folder. In that order. Script should check for artifacts themselves.
echo "Running for platforms '$@'."
for directory in */ ; do
    platform=`basename "${directory}"`
    if [[ $@ == *"$platform"* || $@ = *"all"* ]]; then
        echo "Processing ${platform}"
        pushd "$platform"
        if [ -f build.sh ]; then
            ./build.sh 
        fi  
        if [ -f package.sh ]; then
            ./package.sh
        fi
        if [ -f sign.sh ]; then
            ./sign.sh
        fi
        popd
    else
        echo "Skipping $platform."
    fi
done
