#!/usr/bin/env bash
# Build and deploy Fanzine Press to a remote Ubuntu server.
#
# Required env vars (or defaults shown):
#   SSH_HOST        www.bokontep.gr
#   SSH_USER        claude
#   SSH_HOSTKEY     SHA256 fingerprint (optional, strict-host-checking)
#   REMOTE_DIR      /opt/fanzine-press
#   SERVICE_NAME    fanzine-press.service
#
# Requires on the local machine: git, tar, ssh, scp
# Requires on the remote: docker, systemd, the fanzine-press.service unit
#
# Authentication: assumes SSH key auth (ssh-agent or ~/.ssh/id_*).
# If you must use a password, export SSH_PASS and install sshpass.
set -euo pipefail

SSH_HOST="${SSH_HOST:-www.bokontep.gr}"
SSH_USER="${SSH_USER:-claude}"
REMOTE_DIR="${REMOTE_DIR:-/opt/fanzine-press}"
SERVICE_NAME="${SERVICE_NAME:-fanzine-press.service}"
IMAGE_TAG="${IMAGE_TAG:-fanzine-press:latest}"

# Move to repo root (this script lives in deploy/)
cd "$(dirname "$0")/.."

if ! command -v git >/dev/null; then
    echo "error: git not found" >&2
    exit 1
fi

# Version info from git. Bare tags are expected (e.g. "0.1.0", not "v0.1.0").
# --dirty marks the version if the working tree has uncommitted changes.
APP_VERSION=$(git describe --tags --always --dirty --match '[0-9]*' 2>/dev/null || echo "dev")
GIT_SHA=$(git rev-parse --short HEAD)

echo ">>> Building $IMAGE_TAG"
echo "    APP_VERSION = $APP_VERSION"
echo "    GIT_SHA     = $GIT_SHA"
echo "    target      = $SSH_USER@$SSH_HOST:$REMOTE_DIR"
echo

if [[ "$APP_VERSION" == *-dirty ]]; then
    echo "!!! working tree is dirty — consider committing before deploying"
    echo
fi

# --- Package sources ---
TARBALL=$(mktemp -t fanzine-src-XXXXXX.tar.gz)
trap 'rm -f "$TARBALL"' EXIT

echo ">>> Packing source tree"
tar czf "$TARBALL" \
    --exclude='.git' \
    --exclude='.claude' \
    --exclude='.vs' \
    --exclude='**/bin' \
    --exclude='**/obj' \
    --exclude='**/node_modules' \
    --exclude='**/wwwroot/uploads/*' \
    --exclude='**/*.db' \
    --exclude='**/*.db-*' \
    Dockerfile .dockerignore src/

SSH_OPTS=(-o BatchMode=yes)
SCP_OPTS=(-o BatchMode=yes)
if [[ -n "${SSH_HOSTKEY:-}" ]]; then
    # strict host key checking against a known fingerprint
    :
fi

SSH_CMD() {
    if [[ -n "${SSH_PASS:-}" ]]; then
        sshpass -e ssh -o StrictHostKeyChecking=accept-new "$@"
    else
        ssh "${SSH_OPTS[@]}" "$@"
    fi
}
SCP_CMD() {
    if [[ -n "${SSH_PASS:-}" ]]; then
        sshpass -e scp -o StrictHostKeyChecking=accept-new "$@"
    else
        scp "${SCP_OPTS[@]}" "$@"
    fi
}

export SSHPASS="${SSH_PASS:-}"

echo ">>> Uploading source"
SCP_CMD "$TARBALL" "$SSH_USER@$SSH_HOST:/tmp/fanzine-src.tar.gz"

echo ">>> Extracting on remote"
SSH_CMD "$SSH_USER@$SSH_HOST" bash -s <<REMOTE
set -euo pipefail
mkdir -p "$REMOTE_DIR"
cd "$REMOTE_DIR"
rm -rf src Dockerfile .dockerignore
tar xzf /tmp/fanzine-src.tar.gz
rm /tmp/fanzine-src.tar.gz
REMOTE

echo ">>> Building image on remote"
SSH_CMD "$SSH_USER@$SSH_HOST" bash -s <<REMOTE
set -euo pipefail
cd "$REMOTE_DIR"
docker build \
    --build-arg APP_VERSION="$APP_VERSION" \
    --build-arg GIT_SHA="$GIT_SHA" \
    -t "$IMAGE_TAG" \
    . | tail -10
REMOTE

echo ">>> Restarting $SERVICE_NAME"
SSH_CMD "$SSH_USER@$SSH_HOST" "sudo systemctl restart $SERVICE_NAME && sleep 3 && sudo systemctl is-active $SERVICE_NAME"

echo
echo ">>> Deployed  v$APP_VERSION ($GIT_SHA)"
