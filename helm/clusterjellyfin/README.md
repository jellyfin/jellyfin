# ClusterJellyfin Helm Chart

A Helm chart for deploying Jellyfin media server with PostgreSQL backend on Kubernetes.

## Features

- 🐘 **PostgreSQL Database**: Full PostgreSQL support with bundled or external database options
- 📦 **Persistent Storage**: Configurable PVCs for config, cache, and media
- 🚀 **Ingress Support**: Optional ingress with TLS for external access
- 🎮 **Hardware Acceleration**: Support for Intel QSV, NVIDIA, and AMD GPU transcoding
- 🔒 **Security**: Pod Security Standards compliant with configurable security contexts
- 📊 **Health Checks**: Comprehensive liveness, readiness, and startup probes
- 🌐 **Flexible Networking**: ClusterIP, NodePort, or LoadBalancer service types
- 📁 **NFS Support**: Optional NFS volume for shared media storage

## Prerequisites

- Kubernetes 1.19+
- Helm 3.0+
- PV provisioner support in the underlying infrastructure (if using persistent volumes)
- PostgreSQL 14+ (if using external database)

## Installing the Chart

### Add Bitnami Repository (for PostgreSQL dependency)

```bash
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update
```

### Install with Default Configuration

```bash
helm install my-jellyfin ./clusterjellyfin
```

### Install with Custom Values

```bash
helm install my-jellyfin ./clusterjellyfin -f custom-values.yaml
```

### Install in a Specific Namespace

```bash
kubectl create namespace jellyfin
helm install my-jellyfin ./clusterjellyfin -n jellyfin
```

## Uninstalling the Chart

```bash
helm uninstall my-jellyfin
```

This removes all the Kubernetes components associated with the chart and deletes the release.

## Configuration

The following table lists the configurable parameters and their default values.

### Image Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `image.repository` | Jellyfin image repository | `ghcr.io/celesrenata/jellyfin` |
| `image.tag` | Jellyfin image tag | `postgres` |
| `image.pullPolicy` | Image pull policy | `IfNotPresent` |
| `image.pullSecrets` | Image pull secrets | `[]` |

### Service Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `service.type` | Kubernetes service type | `ClusterIP` |
| `service.http.port` | HTTP port | `8096` |
| `service.http.nodePort` | HTTP NodePort (if type is NodePort) | `null` |
| `service.https.enabled` | Enable HTTPS port | `false` |
| `service.https.port` | HTTPS port | `8920` |

### Ingress Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `ingress.enabled` | Enable ingress | `false` |
| `ingress.className` | Ingress class name | `""` |
| `ingress.annotations` | Ingress annotations | `{}` |
| `ingress.hosts` | Ingress hosts configuration | See values.yaml |
| `ingress.tls` | Ingress TLS configuration | `[]` |

### PostgreSQL Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `postgresql.enabled` | Enable bundled PostgreSQL | `true` |
| `postgresql.auth.username` | PostgreSQL username | `jellyfin` |
| `postgresql.auth.password` | PostgreSQL password | `jellyfin` |
| `postgresql.auth.database` | PostgreSQL database name | `jellyfin` |
| `postgresql.primary.persistence.size` | PostgreSQL storage size | `8Gi` |

### External PostgreSQL Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `externalPostgresql.enabled` | Use external PostgreSQL | `false` |
| `externalPostgresql.host` | External PostgreSQL host | `""` |
| `externalPostgresql.port` | External PostgreSQL port | `5432` |
| `externalPostgresql.database` | External PostgreSQL database | `jellyfin` |
| `externalPostgresql.username` | External PostgreSQL username | `jellyfin` |
| `externalPostgresql.password` | External PostgreSQL password | `""` |

### Persistence Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `persistence.config.enabled` | Enable config persistence | `true` |
| `persistence.config.size` | Config volume size | `10Gi` |
| `persistence.config.storageClass` | Config storage class | `""` |
| `persistence.cache.enabled` | Enable cache persistence | `true` |
| `persistence.cache.size` | Cache volume size | `20Gi` |
| `persistence.media.enabled` | Enable media persistence | `true` |
| `persistence.media.size` | Media volume size | `100Gi` |
| `persistence.media.nfs.enabled` | Use NFS for media | `false` |

### Hardware Acceleration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `hwaccel.enabled` | Enable hardware acceleration | `false` |
| `hwaccel.type` | Acceleration type (intel, nvidia, amd) | `""` |
| `hwaccel.devices.dri.enabled` | Enable Intel/AMD GPU | `false` |
| `hwaccel.devices.nvidia.enabled` | Enable NVIDIA GPU | `false` |

### Resources

| Parameter | Description | Default |
|-----------|-------------|---------|
| `resources.limits.cpu` | CPU limit | `4000m` |
| `resources.limits.memory` | Memory limit | `4Gi` |
| `resources.requests.cpu` | CPU request | `1000m` |
| `resources.requests.memory` | Memory request | `1Gi` |

## Examples

### Using External PostgreSQL

```yaml
# external-postgres-values.yaml
postgresql:
  enabled: false

externalPostgresql:
  enabled: true
  host: postgres.example.com
  port: 5432
  database: jellyfin
  username: jellyfin
  password: "super-secret-password"
```

```bash
helm install my-jellyfin ./clusterjellyfin -f external-postgres-values.yaml
```

### Using NFS for Media Storage

```yaml
# nfs-values.yaml
persistence:
  media:
    enabled: true
    nfs:
      enabled: true
      server: nfs-server.example.com
      path: /exports/media
```

```bash
helm install my-jellyfin ./clusterjellyfin -f nfs-values.yaml
```

### Enable Ingress with TLS

```yaml
# ingress-values.yaml
ingress:
  enabled: true
  className: nginx
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/proxy-body-size: "0"
  hosts:
    - host: jellyfin.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: jellyfin-tls
      hosts:
        - jellyfin.example.com
```

```bash
helm install my-jellyfin ./clusterjellyfin -f ingress-values.yaml
```

### Enable Intel QSV Hardware Acceleration

```yaml
# hwaccel-intel-values.yaml
hwaccel:
  enabled: true
  type: intel
  devices:
    dri:
      enabled: true
      hostPath: /dev/dri
  nodeSelector:
    feature.node.kubernetes.io/gpu: "true"

securityContext:
  privileged: true
```

```bash
helm install my-jellyfin ./clusterjellyfin -f hwaccel-intel-values.yaml
```

### Enable NVIDIA GPU Hardware Acceleration

```yaml
# hwaccel-nvidia-values.yaml
hwaccel:
  enabled: true
  type: nvidia
  devices:
    nvidia:
      enabled: true
      devices:
        - /dev/nvidia0
        - /dev/nvidiactl
        - /dev/nvidia-uvm
  nodeSelector:
    nvidia.com/gpu: "true"
  tolerations:
    - key: nvidia.com/gpu
      operator: Exists
      effect: NoSchedule

securityContext:
  privileged: true
```

```bash
helm install my-jellyfin ./clusterjellyfin -f hwaccel-nvidia-values.yaml
```

## Upgrading

### To upgrade an existing release:

```bash
helm upgrade my-jellyfin ./clusterjellyfin
```

### With custom values:

```bash
helm upgrade my-jellyfin ./clusterjellyfin -f custom-values.yaml
```

## Backup and Restore

### Backup PostgreSQL Database

If using bundled PostgreSQL:

```bash
kubectl exec -it <postgresql-pod> -- pg_dump -U jellyfin jellyfin > jellyfin-backup.sql
```

### Backup Configuration and Media

```bash
# Backup config
kubectl cp <jellyfin-pod>:/config ./config-backup

# If not using NFS, backup media
kubectl cp <jellyfin-pod>:/media ./media-backup
```

## Troubleshooting

### Check Pod Status

```bash
kubectl get pods
kubectl describe pod <jellyfin-pod>
kubectl logs <jellyfin-pod>
```

### Check PostgreSQL Connection

```bash
kubectl exec -it <jellyfin-pod> -- env | grep POSTGRES
```

### Check Persistent Volume Claims

```bash
kubectl get pvc
kubectl describe pvc <pvc-name>
```

### Access Jellyfin Shell

```bash
kubectl exec -it <jellyfin-pod> -- /bin/bash
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This Helm chart is provided as-is. Jellyfin itself is licensed under the GNU General Public License v2.0.

## Links

- [Jellyfin Documentation](https://jellyfin.org/docs/)
- [Jellyfin GitHub](https://github.com/jellyfin/jellyfin)
- [PostgreSQL Provider Source](https://github.com/celesrenata/jellyfin)
