# Kubernetes Deployment

Manifests for running See.Idp and its dependencies on a home Kubernetes cluster.

## Prerequisites

The following must be installed and configured in the cluster before applying these
manifests.

| Component | Purpose | Install |
|---|---|---|
| [nginx ingress controller](https://kubernetes.github.io/ingress-nginx/) | HTTP(S) ingress | `helm install ingress-nginx ingress-nginx/ingress-nginx` |
| [cert-manager](https://cert-manager.io/) | Automatic TLS via Let's Encrypt | `helm install cert-manager jetstack/cert-manager --set crds.enabled=true` |
| [CloudNativePG operator](https://cloudnative-pg.io/) | PostgreSQL cluster management | `helm install cnpg cloudnative-pg/cloudnative-pg` |

### cert-manager ClusterIssuer

Apply this once after cert-manager is running. Replace the email address.

```yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: you@example.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
      - http01:
          ingress:
            ingressClassName: nginx
```

---

## Deployment steps

### 1 — Update placeholders

Before applying anything, replace the placeholders in these files:

| File | Placeholder | Replace with |
|---|---|---|
| `idp-deployment.yaml` | `YOUR_REGISTRY/see-idp-web:latest` | Your container registry and image tag |
| `idp-ingress.yaml` | `idp.example.com` (×2) | Your public domain name |

### 2 — Create the namespace

```bash
kubectl apply -f k8s/namespace.yaml
```

### 3 — Deploy PostgreSQL

```bash
kubectl apply -f k8s/postgres-cluster.yaml
```

Wait for the cluster to be ready:

```bash
kubectl wait --for=condition=Ready cluster/seeidp-postgres -n seeidp --timeout=120s
```

CloudNativePG creates a Secret named `seeidp-postgres-app` in the `seeidp` namespace
containing `username`, `password`, `host`, `port`, `dbname`, and `uri`. Retrieve the
connection details:

```bash
kubectl get secret seeidp-postgres-app -n seeidp -o jsonpath='{.data.username}' | base64 -d
kubectl get secret seeidp-postgres-app -n seeidp -o jsonpath='{.data.password}' | base64 -d
```

### 4 — Generate OpenIddict signing and encryption keys

Run this snippet twice — once for the signing key, once for the encryption key:

```csharp
using System.Security.Cryptography;
using var rsa = RSA.Create(2048);
Console.WriteLine(rsa.ToXmlString(includePrivateParameters: true));
```

Or with a one-liner in a .NET script / `dotnet-script`.

### 5 — Create the Secrets

**Never commit real secret values to source control.**

```bash
kubectl create secret generic seeidp-secrets \
  -n seeidp \
  --from-literal=ConnectionStrings__DefaultConnection="Host=seeidp-postgres-rw;Port=5432;Database=seeidp;Username=seeidp;Password=REPLACE" \
  --from-literal=OpenIddict__SigningKey="REPLACE_WITH_RSA_XML" \
  --from-literal=OpenIddict__EncryptionKey="REPLACE_WITH_RSA_XML" \
  --from-literal=Email__ApiKey="REPLACE_WITH_RESEND_API_KEY" \
  --from-literal=Email__FromAddress="noreply@yourdomain.com"
```

The `Host` in the connection string is the CloudNativePG read-write service, which is
always `{cluster-name}-rw` within the same namespace (`seeidp-postgres-rw` here).

### 6 — Deploy Redis

```bash
kubectl apply -f k8s/redis-pvc.yaml
kubectl apply -f k8s/redis-deployment.yaml
kubectl apply -f k8s/redis-service.yaml
```

### 7 — Build and push the IDP image

```bash
docker build -t YOUR_REGISTRY/see-idp-web:1.0.0 -f src/See.Idp.Web/Dockerfile ./src
docker push YOUR_REGISTRY/see-idp-web:1.0.0
```

Update the `image:` field in `idp-deployment.yaml` to match the pushed tag.

### 8 — Deploy the IDP

For the **first deployment only**, temporarily enable seeding to create the initial admin
user and clients. Edit `configmap.yaml` and set `Initialization__Enabled: "true"`, then
revert it to `"false"` after the first pod starts successfully.

The `Initialization` users and clients are read from `appsettings.json` /
`appsettings.Production.json`. Override them via additional env vars or a second ConfigMap
as needed.

```bash
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/idp-deployment.yaml
kubectl apply -f k8s/idp-service.yaml
kubectl apply -f k8s/idp-ingress.yaml
kubectl apply -f k8s/idp-hpa.yaml
```

### 9 — Verify

```bash
# Watch pods come up
kubectl get pods -n seeidp -w

# Check IDP health
kubectl port-forward svc/seeidp 8080:80 -n seeidp
curl http://localhost:8080/health
```

---

## Updating the IDP

```bash
docker build -t YOUR_REGISTRY/see-idp-web:NEW_TAG -f src/See.Idp.Web/Dockerfile ./src
docker push YOUR_REGISTRY/see-idp-web:NEW_TAG
kubectl set image deployment/seeidp seeidp=YOUR_REGISTRY/see-idp-web:NEW_TAG -n seeidp
kubectl rollout status deployment/seeidp -n seeidp
```

---

## Manifest inventory

| File | Kind | Purpose |
|---|---|---|
| `namespace.yaml` | Namespace | `seeidp` namespace |
| `configmap.yaml` | ConfigMap | Non-sensitive runtime config |
| `postgres-cluster.yaml` | Cluster (CNPG) | PostgreSQL cluster, 1 instance, 5 Gi |
| `redis-pvc.yaml` | PersistentVolumeClaim | Redis data volume, 1 Gi |
| `redis-deployment.yaml` | Deployment | Redis 7 (alpine) with AOF persistence |
| `redis-service.yaml` | Service | ClusterIP for Redis |
| `idp-deployment.yaml` | Deployment | See.Idp web app, 1 replica, `/health` probes |
| `idp-service.yaml` | Service | ClusterIP on port 80 → container 8080 |
| `idp-ingress.yaml` | Ingress | nginx + cert-manager TLS, letsencrypt-prod |
| `idp-hpa.yaml` | HorizontalPodAutoscaler | Scale 1–3 replicas on CPU 70% / memory 80% |

---

## Notes

- **Secrets** are never committed. Use `kubectl create secret` as shown above, or a
  secrets manager (Sealed Secrets, External Secrets Operator, etc.).
- **Forwarded headers** — the IDP trusts `X-Forwarded-For` and `X-Forwarded-Proto` from
  any in-cluster proxy. The nginx ingress sets these automatically; no additional
  configuration is needed.
- **Data Protection keys** persist to Redis, so rolling pod restarts do not invalidate
  existing cookies or tokens.
- **OpenIddict keys** are loaded from environment variables at startup; no key rotation
  automation is included — rotate by updating the Secret and restarting the Deployment.
