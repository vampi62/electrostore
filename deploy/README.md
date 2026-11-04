# Deployment on Kubernetes (k3s + Helm + Argo CD + Traefik)

This folder deploys **one full ElectroStore instance per pull request** on a k3s server, so that changes
(and the integration tests) can be exercised in parallel against a real stack. The production-style
deployment of ElectroStore remains the [docker-compose generator](../docs/01_installation.md).

```
deploy/
├── helm/electrostore/     chart: API, FRONT, NOTIF, CRON, WORKER + MariaDB, Kafka (KRaft), Mosquitto, Garage (S3)
├── argocd/                AppProject, ApplicationSet (one Application per "preview" pull request), seed-store Application
├── seed-store/            tiny in-cluster HTTP server holding the preview datasets (outside git)
├── bootstrap/             values/manifests to set up Argo CD and the k3s Traefik (adapt to your domain)
└── scripts/               seed-export.sh (create a dataset) / seed-publish.sh (upload it)
```

## How it works

```
 pull request
     │ push
     ▼
 .github/workflows/pull-request.yml
   quality + tests ──► build images pr-<n>-<sha7> (ghcr.io, public) ──► label the PR "preview"
                                                                          │
 Argo CD ApplicationSet (pull-request generator, label "preview") ◄───────┘
     │ creates Application electrostore-pr-<n>, chart taken from the PR commit
     ▼
 namespace electrostore-pr-<n>
   wave 0  MariaDB, Kafka, Mosquitto, Garage (+ PVCs)
   wave 1  Jobs: garage-init (layout, key, bucket) · kafka-topics
   wave 2  Job : seed (restore the dataset: MariaDB dump + files into Garage)
   wave 3  API, FRONT, NOTIF, CRON, WORKER + Ingresses (Traefik)
     │
     ▼
 https://pr-<n>.<baseDomain>  (front)      https://api-pr-<n>.<baseDomain>  (API)
     │
 .github/workflows/integration.yml: waits for /health to report revision <sha7>, then Playwright + k6
     │
 pull request closed ──► Application deleted (namespace + PVCs gone) · pr-cleanup.yml deletes the images
```

Design notes:

- **Files only go through S3.** The API runs with `S3.Enable=true` against the instance's own Garage; there is no
  shared volume for files. Garage and MariaDB use a PVC (k3s `local-path`) that disappears with the namespace.
- **Credentials are derived** from `secrets.salt` + the instance name (deterministic, no Argo CD drift). They only protect
  throwaway preview data. The AES key is a fixed test key (`values-preview.yaml`), because the seeded database stores MQTT
  passwords encrypted with it.
- **WORKER and `docker.sock`**: on Kubernetes the WORKER manages broker users with the Kubernetes API (`pods/exec` on the
  Mosquitto pod, ServiceAccount + Role in the chart) instead of the Docker API. See `MQTT:UserManagement` in
  [architecture.md](../docs/reference/architecture.md#mqtt).
- The API reports the deployed build on `GET /health` (`revision`, from `AppRevision`); CI uses it to know that the new
  commit is live before testing.

## 1. Prerequisites (once)

| What | Notes |
|---|---|
| k3s server | with the bundled Traefik and `local-path` storage (defaults). amd64 or arm64: the images are multi-arch. |
| DNS | wildcard record `*.<baseDomain>` → the server, e.g. `*.devstore.example.com` (front `pr-12.devstore…`, API `api-pr-12.devstore…`, Argo CD `argocd.devstore…`). |
| TLS | see `bootstrap/traefik-helmchartconfig.yaml` (Let's Encrypt resolver `letsencrypt`). Prefer a single wildcard certificate (DNS-01) over HTTP-01 for many parallel previews. With cert-manager instead, set `ingress.certManagerIssuer`. |
| GitHub | a token for Argo CD (below), the repository variables/secrets of [section 4](#4-github-repository-settings). |

## 2. Bootstrap the cluster

```bash
# k3s (Traefik and local-path are included)
curl -sfL https://get.k3s.io | sh -
export KUBECONFIG=/etc/rancher/k3s/k3s.yaml

# Traefik: certificate resolver + HTTP->HTTPS redirect (edit the e-mail first)
kubectl apply -f deploy/bootstrap/traefik-helmchartconfig.yaml

# Argo CD (edit global.domain first)
helm repo add argo https://argoproj.github.io/argo-helm
helm upgrade --install argocd argo/argo-cd -n argocd --create-namespace -f deploy/bootstrap/argocd-values.yaml

# Token used by the ApplicationSet to list pull requests (fine-grained PAT: Pull requests: read-only on this repo)
kubectl -n argocd create secret generic github-token --from-literal=token=<github_pat>
```

## 3. Install the previews

Edit `deploy/argocd/applicationset-previews.yaml` (`owner`/`repo` if you forked, `baseDomain`), then:

```bash
kubectl apply -f deploy/argocd/appproject.yaml
kubectl apply -f deploy/argocd/seed-store-app.yaml      # the dataset store (see below)
kubectl apply -f deploy/argocd/applicationset-previews.yaml
```

Open a pull request: once CI has built the images it labels the PR `preview`, and Argo CD creates
`electrostore-pr-<n>` within `requeueAfterSeconds` (2 min; add a GitHub webhook to the ApplicationSet controller for instant updates).

## 4. GitHub repository settings

| Kind | Name | Purpose |
|---|---|---|
| Variable | `PREVIEW_DOMAIN` | base domain (e.g. `devstore.example.com`). **Without it the integration job is skipped** (images are still built). |
| Variable | `PR_BUILD_ARCH` | optional, e.g. `linux/arm64/v8`: build only the architecture of the cluster (faster). Default: both. |
| Variable | `PREVIEW_ADMIN_EMAIL` | optional, admin account of the seeded dataset (default `admin@localhost.local`). |
| Secret | `PREVIEW_ADMIN_PASSWORD` | optional, matching password (default `Admin@1234`, the account the API creates on an empty database). |
| Secret | `PACKAGES_DELETE_TOKEN` | optional PAT (`delete:packages`) for `pr-cleanup.yml`, only if `GITHUB_TOKEN` is refused when deleting images. |

## 5. The dataset (MariaDB dump + S3 files)

Not stored in git. It lives in the **seed store** (`deploy/seed-store`): an nginx serving a PVC, reachable **only inside the
cluster** at `http://seed-store.electrostore-seed.svc.cluster.local/<dataset>/` with **no authentication** - use test data only.

```
<dataset>/mariadb.sql.gz   dump of the application database
<dataset>/files.tar.gz     content of the S3 bucket
```

```bash
# 1. export from a running instance (k8s namespace or the docker-compose stack of the generator)
bash deploy/scripts/seed-export.sh --source k8s --namespace electrostore-pr-12 --dataset default
bash deploy/scripts/seed-export.sh --source docker --env-file ./.env --dataset default

# 2. publish it to the seed store
bash deploy/scripts/seed-publish.sh default ./dataset/default
```

On each new instance the `seed` Job downloads the dataset and restores it **only into an empty database / bucket**
(so re-syncs never overwrite data). Without a dataset the instance starts empty and the API creates its default admin.
The chart value `seed.dataset` selects the dataset (default `default`); `seed.required=true` makes a missing dataset fail the deployment.

The exported database must come from an instance that uses the test AES key of `values-preview.yaml` (`Encryption:HexKey`),
and the schema must be older than or equal to the one of the pull request (the API applies pending EF migrations at startup).

## Validate the chart locally

```bash
helm lint deploy/helm/electrostore -f deploy/helm/electrostore/values-preview.yaml
helm template electrostore deploy/helm/electrostore -f deploy/helm/electrostore/values-preview.yaml \
  --namespace electrostore-pr-1 --set instance.name=pr-1 --set baseDomain=devstore.example.com --set image.tag=pr-1-abc1234 \
  | kubectl apply --dry-run=client -f -
```

## Operating notes

- `argocd app list` / the Argo CD UI show one Application per open `preview` pull request; delete the label to remove an instance.
- Logs: `kubectl -n electrostore-pr-<n> logs deploy/api` (also `worker`, `kafka`, jobs: `kubectl logs job/seed`).
- The chart is meant to be deployed by Argo CD (sync waves + hook Jobs). A plain `helm install` also works but the init Jobs are not ordered
  (the .NET services retry their dependencies at startup).
- Capacity: one instance requests about 1.3 GiB of memory (Kafka and the API dominate); size the server for the number of parallel PRs you expect.
