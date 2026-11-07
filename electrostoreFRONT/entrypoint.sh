#!/bin/sh
# Generate the runtime configuration served as /config.js (see nginx.conf).
set -eu

js_escape() {
    printf '%s' "$1" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g'
}

cat > /tmp/config.js <<EOF
window.__APP_CONFIG__ = {
  API_URL: "$(js_escape "${VUE_API_URL:-}")",
  DEMO_MODE: "$(js_escape "${APP_DEMO_MODE:-false}")",
  VAPID_PUBLIC_KEY: "$(js_escape "${VUE_VAPID_PUBLIC_KEY:-}")"
};
EOF

exec "$@"
