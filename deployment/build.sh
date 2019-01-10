#!/usr/bin/env bash

# Execute all build.sh and package.sh and sign.sh scripts in every folder. In that order. Script should check for artifacts themselves.
echo "Running for platforms '$@'."
for directory in */ ; do
    platform=`basename "${directory}"`
    if [[ $@ == *"$platform"* || $@ = *"all"* ]]; then
        echo "Processing ${platform}"
        pushd "$platform"
        if [ -f build.sh ]; then
            echo ./build.sh 
        fi  
        if [ -f package.sh ]; then
            echo ./package.sh
        fi
        if [ -f sign.sh ]; then
            echo ./sign.sh
        fi
        popd
    else
        echo "Skipping $platform."
    fi
done
