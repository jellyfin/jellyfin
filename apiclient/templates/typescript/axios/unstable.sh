#!/bin/bash

java -jar openapi-generator-cli.jar generate \
    --input-spec $(System.ArtifactsDirectory)/openapispec/openapi.json \
    --generator-name typescript-axios \
    --output ./apiclient/generated/typescript/axios  \
    --template-dir ./apiclient/templates/typescript/axios \
    --ignore-file-override ./apiclient/.openapi-generator-ignore \
    --additional-properties=useSingleRequestParameter="true",withSeparateModelsAndApi="true",npmName="axios",snapshotVersion="-SNAPSHOT.$(Build.BuildNumber)",npmRepository="https://pkgs.dev.azure.com/jellyfin-project/jellyfin/_packaging/unstable/npm/registry/"
