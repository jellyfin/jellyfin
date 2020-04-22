#!/usr/bin/env bash

# update-informational-version - increase the shared informational version

set -o errexit
set -o pipefail

usage() {
    echo -e "update-informational-version - increase the shared informational version"
    echo -e ""
    echo -e "Usage:"
    echo -e " $ update-informational-version <new_version>"
}

if [[ -z $1 ]]; then
    usage
    exit 1
fi

shared_version_file="./SharedVersion.cs"

new_version="$1"

echo "Updating to ${new_version}"

# Set the informational version to the specified new_version
new_version_sed="$( sed -e 's/[\/&]/\\&/g' <<<"${new_version}" )"
sed -i "s/AssemblyInformationalVersion(\"[^\"]*\")/AssemblyInformationalVersion(\"${new_version_sed}\")/g" ${shared_version_file}
