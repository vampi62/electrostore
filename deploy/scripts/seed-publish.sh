#!/usr/bin/env bash
# Uploads a dataset directory (mariadb.sql.gz and/or files.tar.gz) to the in-cluster seed store.
#
#   deploy/scripts/seed-publish.sh <dataset> <directory> [--namespace electrostore-seed]
#
# The preview instances fetch it from http://seed-store.electrostore-seed.svc.cluster.local/<dataset>/
# (chart value seed.dataset). Re-publishing replaces the files; instances already restored are not touched
# (the seed Job only restores into an empty database / bucket).
set -euo pipefail

NAMESPACE="electrostore-seed"
POSITIONAL=()
while [ $# -gt 0 ]; do
  case "$1" in
    --namespace|-n) NAMESPACE="$2"; shift 2 ;;
    -h|--help) sed -n '2,9p' "$0"; exit 0 ;;
    *) POSITIONAL+=("$1"); shift ;;
  esac
done
[ "${#POSITIONAL[@]}" -eq 2 ] || { echo "Usage: $0 <dataset> <directory> [--namespace NS]" >&2; exit 2; }
DATASET="${POSITIONAL[0]}"
DIR="${POSITIONAL[1]}"

[[ "$DATASET" =~ ^[A-Za-z0-9._-]+$ ]] || { echo "Invalid dataset name: $DATASET" >&2; exit 2; }
[ -d "$DIR" ] || { echo "Not a directory: $DIR" >&2; exit 2; }

ROOT="/usr/share/nginx/html"
kubectl -n "$NAMESPACE" exec deploy/seed-store -- mkdir -p "$ROOT/$DATASET"

found=0
for f in mariadb.sql.gz files.tar.gz; do
  [ -f "$DIR/$f" ] || continue
  found=1
  echo "==> Uploading $f ($(du -h "$DIR/$f" | cut -f1))"
  # Upload to a temporary name then rename, so a half-uploaded file is never served.
  kubectl -n "$NAMESPACE" exec -i deploy/seed-store -- sh -c 'cat > "$1.tmp" && mv "$1.tmp" "$1"' sh "$ROOT/$DATASET/$f" < "$DIR/$f"
done
[ "$found" = "1" ] || { echo "No mariadb.sql.gz or files.tar.gz in $DIR" >&2; exit 1; }

echo "==> Published. Content of the seed store:"
kubectl -n "$NAMESPACE" exec deploy/seed-store -- ls -lh "$ROOT/$DATASET"
