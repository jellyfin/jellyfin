#!/usr/bin/env bash

set -e

# Execute every clean.sh scripts in every folder.
echo "Running for platforms '$@'."
for directory in */ ; do
    platform=`basename "${directory}"`
    if [[ $@ == *"$platform"* || $@ = *"all"* ]]; then
        echo "Processing ${platform}"
        pushd "$platform"
        if [ -f clean.sh ]; then
            echo ./clean.sh 
        fi
        popd
    else
        echo "Skipping $platform."
    fi
done

rm -rf ./collect-dist
