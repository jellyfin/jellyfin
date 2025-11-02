# ClusterJellyfin Quick Start Guide

## Installation

### 1. Add Required Helm Repositories

```bash
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update
```

### 2. Install ClusterJellyfin

**Basic installation with default settings:**
```bash
helm install jellyfin ./clusterjellyfin
```

**Installation with custom namespace:**
```bash
kubectl create namespace jellyfin
helm install jellyfin ./clusterjellyfin -n jellyfin
```

### 3. Access Jellyfin

After installation, get access information:
```bash
# Get the pod name
export POD_NAME=$(kubectl get pods -l "app.kubernetes.io/name=clusterjellyfin" -o jsonpath="{.items[0].metadata.name}")

# Port forward to access locally
kubectl port-forward $POD_NAME 8096:8096

# Open browser to http://localhost:8096
```

## Common Configurations

### Use External PostgreSQL Database

Create `external-db-values.yaml`:
```yaml
postgresql:
  enabled: false

externalPostgresql:
  enabled: true
  host: your-postgres-host.example.com
  port: 5432
  database: jellyfin
  username: jellyfin
  password: "your-secure-password"
```

Install:
```bash
helm install jellyfin ./clusterjellyfin -f external-db-values.yaml
```

### Enable Ingress

Create `ingress-values.yaml`:
```yaml
ingress:
  enabled: true
  className: nginx
  hosts:
    - host: jellyfin.yourdomain.com
      paths:
        - path: /
          pathType: Prefix
```

Install:
```bash
helm install jellyfin ./clusterjellyfin -f ingress-values.yaml
```

### Custom Storage Sizes

Create `storage-values.yaml`:
```yaml
persistence:
  config:
    size: 20Gi
  cache:
    size: 50Gi
  media:
    size: 500Gi
```

Install:
```bash
helm install jellyfin ./clusterjellyfin -f storage-values.yaml
```

## Upgrade

```bash
helm upgrade jellyfin ./clusterjellyfin
```

## Uninstall

```bash
helm uninstall jellyfin
```

## Troubleshooting

### Check Pod Status
```bash
kubectl get pods
kubectl describe pod <pod-name>
kubectl logs <pod-name>
```

### Check PostgreSQL Connection
```bash
kubectl exec -it <jellyfin-pod> -- env | grep POSTGRES
```

### Access Pod Shell
```bash
kubectl exec -it <jellyfin-pod> -- /bin/bash
```

## Next Steps

- Configure media libraries through the Jellyfin web interface
- Set up transcoding options
- Configure user accounts and permissions
- Enable hardware acceleration (see README.md)

For complete documentation, see [README.md](README.md)
