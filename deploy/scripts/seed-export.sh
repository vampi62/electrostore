#!/usr/bin/env bash
# Creates a preview dataset (MariaDB dump + S3 files archive) from a running ElectroStore instance.
#
#   deploy/scripts/seed-export.sh --source k8s    --namespace electrostore-pr-12 [--dataset default] [--out ./dataset]
#   deploy/scripts/seed-export.sh --source docker [--env-file ./.env]           [--dataset default] [--out ./dataset]
#
# Output (in --out, default ./dataset/<dataset>):
#   mariadb.sql.gz   dump of the application database (no CREATE DATABASE / USE statements)
#   files.tar.gz     content of the S3 bucket
# Then publish it with deploy/scripts/seed-publish.sh.
#
# Requirements: bash, gzip, tar, rclone, plus kubectl (k8s source) or docker (docker source).
#
# IMPORTANT
# - The dataset is served WITHOUT authentication inside the cluster and is NOT kept in git: use test data only.
# - The database stores MQTT passwords encrypted with the instance AES key. Preview instances use the fixed key of
#   deploy/helm/electrostore/values-preview.yaml (secrets.aesKey): export from an instance configured with that same
#   key (Encryption:HexKey), otherwise the encrypted values can't be decrypted in the previews.
set -euo pipefail

SOURCE=""
NAMESPACE=""
DATASET="default"
OUT=""
ENV_FILE=""

while [ $# -gt 0 ]; do
  case "$1" in
    --source) SOURCE="$2"; shift 2 ;;
    --namespace|-n) NAMESPACE="$2"; shift 2 ;;
    --dataset) DATASET="$2"; shift 2 ;;
    --out) OUT="$2"; shift 2 ;;
    --env-file) ENV_FILE="$2"; shift 2 ;;
    -h|--help) sed -n '2,22p' "$0"; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done

[[ "$DATASET" =~ ^[A-Za-z0-9._-]+$ ]] || { echo "Invalid dataset name: $DATASET" >&2; exit 2; }
[ -n "$SOURCE" ] || { echo "--source docker|k8s is required" >&2; exit 2; }
command -v rclone >/dev/null || { echo "rclone is required" >&2; exit 1; }
OUT="${OUT:-./dataset/$DATASET}"
mkdir -p "$OUT"
WORK="$(mktemp -d)"
PF_PID=""
cleanup() { [ -n "$PF_PID" ] && kill "$PF_PID" 2>/dev/null || true; rm -rf "$WORK"; }
trap cleanup EXIT

case "$SOURCE" in
  k8s)
    [ -n "$NAMESPACE" ] || { echo "--namespace is required with --source k8s" >&2; exit 2; }
    K="kubectl -n $NAMESPACE"
    echo "==> Dumping the database from $NAMESPACE"
    $K exec deploy/mariadb -- sh -c \
      'MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mariadb-dump -uroot --single-transaction --no-tablespaces --routines --triggers "$MYSQL_DATABASE"' \
      | gzip > "$OUT/mariadb.sql.gz"
    S3_ACCESS_KEY="$($K get secret credentials -o jsonpath='{.data.s3-access-key}' | base64 -d)"
    S3_SECRET_KEY="$($K get secret credentials -o jsonpath='{.data.s3-secret-key}' | base64 -d)"
    S3_BUCKET="${S3_BUCKET:-electrostore-api}"
    S3_REGION="${S3_REGION:-garage}"
    S3_ENDPOINT="http://127.0.0.1:13900"
    $K port-forward svc/garage 13900:3900 >/dev/null &
    PF_PID=$!
    sleep 3
    ;;
  docker)
    [ -z "$ENV_FILE" ] || { set -a; . "$ENV_FILE"; set +a; }
    : "${MARIADB_ROOT_PASSWORD:?MARIADB_ROOT_PASSWORD is required (see --env-file)}"
    : "${S3_ACCESS_KEY:?S3_ACCESS_KEY is required (see --env-file)}"
    : "${S3_SECRET_KEY:?S3_SECRET_KEY is required (see --env-file)}"
    DB="${MARIADB_DATABASE:-electrostore_db}"
    S3_BUCKET="${S3_BUCKET:-electrostore-api}"
    S3_REGION="${S3_REGION:-garage}"
    S3_ENDPOINT="${S3_ENDPOINT:-http://127.0.0.1:3900}"
    echo "==> Dumping the database from electrostore-mariadb"
    docker exec -e MYSQL_PWD="$MARIADB_ROOT_PASSWORD" electrostore-mariadb \
      mariadb-dump -uroot --single-transaction --no-tablespaces --routines --triggers "$DB" \
      | gzip > "$OUT/mariadb.sql.gz"
    ;;
  *) echo "--source must be docker or k8s" >&2; exit 2 ;;
esac

echo "==> Downloading the S3 bucket '$S3_BUCKET' from $S3_ENDPOINT"
export RCLONE_CONFIG_SRC_TYPE=s3
export RCLONE_CONFIG_SRC_PROVIDER=Other
export RCLONE_CONFIG_SRC_ENDPOINT="$S3_ENDPOINT"
export RCLONE_CONFIG_SRC_REGION="$S3_REGION"
export RCLONE_CONFIG_SRC_ACCESS_KEY_ID="$S3_ACCESS_KEY"
export RCLONE_CONFIG_SRC_SECRET_ACCESS_KEY="$S3_SECRET_KEY"
export RCLONE_CONFIG_SRC_FORCE_PATH_STYLE=true
mkdir -p "$WORK/files"
rclone sync "src:$S3_BUCKET" "$WORK/files" --transfers 4
rm -f "$WORK/files/.electrostore-seeded"
tar -czf "$OUT/files.tar.gz" -C "$WORK/files" .

echo "==> Dataset '$DATASET' written to $OUT:"
ls -lh "$OUT"
echo "Next: deploy/scripts/seed-publish.sh $DATASET $OUT"
