#!/usr/bin/env bash
# Compose smoke test, one assertion per API (Step 8.1).
#
# For each API it checks, through the same paths operators and Prometheus use:
#   1. nginx-routed liveness:  http://<nginx>/<prefix>/health/live -> 200
#   2. direct exposition:      http://127.0.0.1:<port>/metrics  -> 200 + OpenMetrics body
# Plus RabbitMQ's plugin endpoint (:15692/metrics).
#
# The direct checks mirror the Prometheus scrape jobs in
# infra/prometheus/prometheus.yml (Docker-network targets, explicit
# metrics_path). nginx denies /metrics publicly by design, so there is
# deliberately no nginx-routed metrics check (see docs/prod-exposure.md).
#
# Usage: bash infra/smoke/compose-smoke.sh [nginx-base]
#   nginx-base defaults to http://localhost
#
# Cold-start behavior (Step 9.3): services declare healthchecks and
# service_healthy gates in docker-compose.yml, so `docker compose up -d`
# already orders startup. This script additionally waits (bounded) for every
# service with a healthcheck to report healthy before asserting, so it can
# run immediately after `up` on a cold stack.
set -euo pipefail

NGINX="${1:-http://localhost}"
FAILURES=0

if command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
  echo "-- waiting for compose healthchecks (up to 10 min) ..."
  deadline=$((SECONDS + 600))
  while true; do
    unhealthy="$(docker compose ps --format '{{.Name}} {{.Health}}' 2>/dev/null | awk '$2 != "" && $2 != "healthy" {print $1}')"
    if [[ -z "$unhealthy" ]]; then
      echo "-- all compose healthchecks healthy"
      break
    fi
    if (( SECONDS > deadline )); then
      echo "FAIL compose health: still unhealthy after 10 min: $unhealthy"
      docker compose ps
      exit 1
    fi
    sleep 10
  done
else
  echo "-- docker compose not available; asserting endpoints directly"
fi

check() { # check <label> <expected-code> <url> [grep-pattern]
  local label="$1" expected="$2" url="$3" pattern="${4:-}"
  local code body
  code="$(curl -s -o /tmp/smoke-body.txt -w "%{http_code}" --max-time 10 "$url" || echo 000)"
  body="$(cat /tmp/smoke-body.txt 2>/dev/null || true)"
  if [[ "$code" != "$expected" ]]; then
    echo "FAIL $label: $url -> HTTP $code (want $expected)"
    FAILURES=$((FAILURES + 1))
    return 0
  fi
  if [[ -n "$pattern" && "$body" != *"$pattern"* ]]; then
    echo "FAIL $label: $url -> HTTP $code but body lacks '$pattern'"
    FAILURES=$((FAILURES + 1))
    return 0
  fi
  echo "ok   $label: $url -> HTTP $code"
}

# prefix:loopback-port per API (ports from docker-compose.yml).
APIS="identity:5146 merchant:5237 payment:5118 ledger:5320 notification:5281 settlement:5392"

for entry in $APIS; do
  prefix="${entry%%:*}"
  port="${entry##*:}"
  check "$prefix health (nginx)" 200 "$NGINX/$prefix/health/live" '"status":"Healthy"'
  check "$prefix metrics (direct)" 200 "http://127.0.0.1:$port/metrics" "# TYPE target_info"
done

check "rabbitmq metrics (direct)" 200 "http://127.0.0.1:15692/metrics" "rabbitmq_build_info"

if [[ "$FAILURES" -ne 0 ]]; then
  echo "SMOKE FAILED: $FAILURES check(s) failed"
  exit 1
fi
echo "SMOKE PASSED: all API health + metrics endpoints reachable"
