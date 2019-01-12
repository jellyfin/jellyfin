#!/usr/bin/env bash

git submodule update --init --recursive

pushd ../Jellyfin.Versioning
./update-version
popd

#TODO enabled proper flag parsing for enabling and disabling building, signing, packaging and publishing

# Execute all build.sh, package.sh, sign.sh and publish.sh scripts in every folder. In that order. Script should check for artifacts themselves.
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
        if [ -f publish.sh ]; then
            ./publish.sh
        fi
        popd
    else
        echo "Skipping $platform."
    fi
done
