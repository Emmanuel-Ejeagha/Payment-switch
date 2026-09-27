#!/usr/bin/env bash
# rotate-jwt.sh — rotate the JWT signing secret with a zero-downtime
# dual-write window.
#
# Procedure (dual-write window, supported by AddPaymentSwitchJwtBearer):
#   1. Set Jwt:Secret (JWT_SECRET) to the NEW key.
#   2. Set Jwt:PreviousSecret (JWT_PREVIOUS_SECRET) to the OLD key and redeploy.
#      Both keys validate until the window closes, so existing tokens keep
#      working while new tokens are signed with the new key.
#   3. After the window (>= max token lifetime), clear Jwt:PreviousSecret and
#      redeploy. Only the new key is then accepted.
#
# Usage:
#   ./infra/secrets/rotate-jwt.sh <new-secret> [compose|k8s|helm]
#
#   compose  – writes JWT_SECRET/JWT_PREVIOUS_SECRET into .env
#   k8s      – updates the payment-switch-secret (Jwt__Secret/Jwt__PreviousSecret)
#   helm     – prints --set flags for a helm upgrade
#
# For compose, the OLD value of JWT_SECRET is automatically moved to
# JWT_PREVIOUS_SECRET before JWT_SECRET is replaced.

set -euo pipefail

NEW_SECRET="${1:?usage: rotate-jwt.sh <new-secret> [compose|k8s|helm]}"
TARGET="${2:-compose}"
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ENV_FILE="$PROJECT_ROOT/.env"
NAMESPACE="${K8S_NAMESPACE:-payment-switch}"
SECRET_NAME="payment-switch-secret"

if [[ ${#NEW_SECRET} -lt 32 ]]; then
  echo "ERROR: new JWT secret must be at least 32 characters." >&2
  exit 1
fi

case "$TARGET" in
  compose)
    [[ -f "$ENV_FILE" ]] || { echo "ERROR: $ENV_FILE not found." >&2; exit 1; }

    OLD_SECRET="$(grep -E '^JWT_SECRET=' "$ENV_FILE" | head -1 | cut -d= -f2- || true)"
    OLD_SECRET="${OLD_SECRET#\"}"
    OLD_SECRET="${OLD_SECRET%\"}"

    # Move the current secret to the previous slot if it is non-empty and
    # different from the new one.
    if [[ -n "$OLD_SECRET" && "$OLD_SECRET" != "$NEW_SECRET" ]]; then
      sed -i.bak "/^JWT_PREVIOUS_SECRET=/d" "$ENV_FILE"
      echo "JWT_PREVIOUS_SECRET=$OLD_SECRET" >> "$ENV_FILE"
    fi
    sed -i.bak "/^JWT_SECRET=/d" "$ENV_FILE"
    echo "JWT_SECRET=$NEW_SECRET" >> "$ENV_FILE"
    rm -f "$ENV_FILE.bak"

    echo "Updated $ENV_FILE:"
    echo "  JWT_SECRET=<new>"
    if [[ -n "$OLD_SECRET" ]]; then echo "  JWT_PREVIOUS_SECRET=<old>"; fi
    echo "Redeploy (docker compose up -d). Remove JWT_PREVIOUS_SECRET after the window."
    ;;

  k8s)
    OLD_SECRET="$(kubectl get secret "$SECRET_NAME" -n "$NAMESPACE" \
      -o jsonpath='{.data.Jwt__Secret}' 2>/dev/null | base64 --decode || true)"

    if [[ -n "$OLD_SECRET" && "$OLD_SECRET" != "$NEW_SECRET" ]]; then
      kubectl patch secret "$SECRET_NAME" -n "$NAMESPACE" --type merge \
        -p "{\"stringData\":{\"Jwt__PreviousSecret\":\"$OLD_SECRET\"}}"
    fi
    kubectl patch secret "$SECRET_NAME" -n "$NAMESPACE" --type merge \
      -p "{\"stringData\":{\"Jwt__Secret\":\"$NEW_SECRET\"}}"

    echo "Patched secret $SECRET_NAME in $NAMESPACE."
    echo "Roll all deployments, then remove Jwt__PreviousSecret after the window."
    ;;

  helm)
    echo "Rotate via helm upgrade with the new secret, moving the old one to"
    echo "config.jwt.previousSecret. First add a values override for the old"
    echo "secret (config.jwt.previousSecret), then set config.jwt.secret to"
    echo "the new value and upgrade. Clear previousSecret after the window."
    echo
    echo "Example:"
    echo "  helm upgrade payment-switch helm/payment-switch --reuse-values \\"
    echo "    --set config.jwt.secret='$NEW_SECRET' \\"
    echo "    --set config.jwt.previousSecret='<old-secret>'"
    ;;

  *)
    echo "ERROR: unknown target '$TARGET' (expected compose|k8s|helm)." >&2
    exit 1
    ;;
esac
