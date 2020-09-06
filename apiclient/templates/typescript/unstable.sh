#!/bin/bash

CLIENT=$1
openapi-generator generate \
    --input-spec $(System.ArtifactsDirectory)/openapi/openapi.json \
    --generator-name typescript-${CLIENT} \
    --output ./apiclient/generated/typescript/${CLIENT}  \
    --template-dir ./apiclient/templates/typescript \
    --ignore-file-override ./apiclient/.openapi-generator-ignore \
    --additional-properties=useSingleRequestParameter="true",npmName="${CLIENT}",snapshotVersion="-SNAPSHOT.$(Build.BuildNumber)",npmRepository="https://dev.azure.com/jellyfin-project/jellyfin/_packaging"
