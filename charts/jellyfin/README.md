# jellyfin

![Version: 10.9.0](https://img.shields.io/badge/Version-10.9.0-informational?style=flat-square) ![Type: application](https://img.shields.io/badge/Type-application-informational?style=flat-square) ![AppVersion: 10.9.0](https://img.shields.io/badge/AppVersion-10.9.0-informational?style=flat-square)

Jellyfin Media Server

**Homepage:** <https://jellyfin.org/>

## Values

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| affinity | object | `{}` |  |
| enableDLNA | bool | `false` |  |
| extraVolumeMounts | list | `[]` |  |
| extraVolumes | list | `[]` |  |
| fullnameOverride | string | `""` |  |
| image.pullPolicy | string | `"IfNotPresent"` |  |
| image.repository | string | `"docker.io/jellyfin/jellyfin"` |  |
| image.tag | string | `"10.9.0"` |  |
| ingress.annotations | object | `{}` |  |
| ingress.enabled | bool | `false` |  |
| ingress.hosts[0] | string | `"chart-example.local"` |  |
| ingress.path | string | `"/"` |  |
| ingress.tls | list | `[]` |  |
| nameOverride | string | `""` |  |
| nodeSelector | object | `{}` |  |
| persistence.config.accessMode | string | `"ReadWriteOnce"` |  |
| persistence.config.enabled | bool | `false` |  |
| persistence.config.size | string | `"1Gi"` |  |
| persistence.extraExistingClaimMounts | list | `[]` |  |
| persistence.media.accessMode | string | `"ReadWriteOnce"` |  |
| persistence.media.enabled | bool | `false` |  |
| persistence.media.size | string | `"10Gi"` |  |
| replicaCount | int | `1` |  |
| resources | object | `{}` |  |
| service.annotations | object | `{}` |  |
| service.labels | object | `{}` |  |
| service.loadBalancerIP | string | `nil` |  |
| service.port | int | `8096` |  |
| service.type | string | `"LoadBalancer"` |  |
| tolerations | list | `[]` |  |

