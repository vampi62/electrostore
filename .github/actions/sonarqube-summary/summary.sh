#!/usr/bin/env bash
set -uo pipefail

# Required env: SONAR_HOST_URL, SONAR_TOKEN, PROJECT_KEY.
# Optional: SUMMARY_TITLE, CE_TIMEOUT (seconds to wait for the analysis to be processed, default 600),
#           FAIL_ON_SEVERITIES (comma-separated: HIGH, MEDIUM, LOW),
#           FAIL_ON_QUALITIES (comma-separated: MAINTAINABILITY, RELIABILITY, SECURITY).
#
# Without FAIL_ON_* this script only reports and never fails the job (the Quality Gate is
# enforced by the scan step itself through sonar.qualitygate.wait).
# With FAIL_ON_SEVERITIES and/or FAIL_ON_QUALITIES set, the job fails when at least one unresolved
# issue matches ALL the given filters (an empty filter matches everything), and also when the
# check cannot be performed (fail closed).
TITLE="${SUMMARY_TITLE:-$PROJECT_KEY}"
FAIL_ON_SEVERITIES=$(printf '%s' "${FAIL_ON_SEVERITIES:-}" | tr -d '[:space:]' | tr '[:lower:]' '[:upper:]')
FAIL_ON_QUALITIES=$(printf '%s' "${FAIL_ON_QUALITIES:-}" | tr -d '[:space:]' | tr '[:lower:]' '[:upper:]')
CE_TIMEOUT="${CE_TIMEOUT:-600}"
GATE_ENABLED=false
if [ -n "$FAIL_ON_SEVERITIES" ] || [ -n "$FAIL_ON_QUALITIES" ]; then
  GATE_ENABLED=true
fi

# Reports a problem that prevents the summary/gate: a warning (exit 0) when no issue gate is
# configured, an error (exit 1) otherwise.
bail() {
  if [ "$GATE_ENABLED" = true ]; then
    echo "::error::[$TITLE] $1 (issue gate enabled, failing)"
    exit 1
  fi
  echo "::warning::[$TITLE] $1, skipping SonarQube summary"
  exit 0
}

# Fails early on a typo in the filters rather than silently matching nothing.
validate_list() {
  local name="$1" value="$2" allowed="$3" item
  [ -z "$value" ] && return 0
  IFS=',' read -ra items <<< "$value"
  for item in "${items[@]}"; do
    case ",$allowed," in
      *",$item,"*) ;;
      *) echo "::error::[$TITLE] invalid value '$item' for $name (allowed: $allowed)"; exit 1 ;;
    esac
  done
}
validate_list FAIL_ON_SEVERITIES "$FAIL_ON_SEVERITIES" "HIGH,MEDIUM,LOW"
validate_list FAIL_ON_QUALITIES "$FAIL_ON_QUALITIES" "MAINTAINABILITY,RELIABILITY,SECURITY"

# SonarQube token auth is HTTP Basic with the token as user and an empty password.
sonar_auth=$(printf '%s:' "$SONAR_TOKEN" | base64 | tr -d '\n')
sonar_get() {
  curl -sf -H "Authorization: Basic $sonar_auth" "$SONAR_HOST_URL$1"
}

# report-task.txt is written by the scanner (location varies between the CLI
# and .NET scanners), so just look for it anywhere in the workspace.
REPORT_TASK_FILE=$(find "${GITHUB_WORKSPACE:-.}" -name 'report-task.txt' -print -quit 2>/dev/null)

[ -n "$REPORT_TASK_FILE" ] || bail "report-task.txt not found"

CE_TASK_ID=$(grep '^ceTaskId=' "$REPORT_TASK_FILE" | cut -d'=' -f2-)
[ -n "$CE_TASK_ID" ] || bail "ceTaskId not found in report-task.txt"

# With sonar.qualitygate.wait=true the analysis is already processed; otherwise wait for it.
CE_DEADLINE=$((SECONDS + CE_TIMEOUT))
while :; do
  RESPONSE=$(sonar_get "/api/ce/task?id=$CE_TASK_ID") || bail "Failed to call the SonarQube API (ce/task)"
  STATUS=$(echo "$RESPONSE" | jq -r '.task.status')
  case "$STATUS" in
    PENDING|IN_PROGRESS) ;;
    *) break ;;
  esac
  [ "$SECONDS" -lt "$CE_DEADLINE" ] || bail "SonarQube analysis still $STATUS after ${CE_TIMEOUT}s"
  sleep 5
done
[ "$STATUS" = "SUCCESS" ] || bail "SonarQube analysis not available (status=$STATUS)"
ANALYSIS_ID=$(echo "$RESPONSE" | jq -r '.task.analysisId // empty')

METRIC_KEYS="bugs,vulnerabilities,code_smells,security_hotspots,coverage,duplicated_lines_density,ncloc"
PROJECT_KEY_URI=$(printf '%s' "$PROJECT_KEY" | jq -sRr @uri)
MEASURES=$(sonar_get "/api/measures/component?component=$PROJECT_KEY_URI&metricKeys=$METRIC_KEYS") || bail "Failed to call the SonarQube API (measures/component)"

metric() {
  echo "$MEASURES" | jq -r --arg key "$1" '(.component.measures[]? | select(.metric == $key) | .value) // "N/A"'
}

percent() {
  local value="$1"
  if [ "$value" = "N/A" ]; then echo "N/A"; else echo "${value}%"; fi
}

BUGS=$(metric bugs)
VULNERABILITIES=$(metric vulnerabilities)
CODE_SMELLS=$(metric code_smells)
HOTSPOTS=$(metric security_hotspots)
COVERAGE=$(percent "$(metric coverage)")
DUPLICATION=$(percent "$(metric duplicated_lines_density)")
NCLOC=$(metric ncloc)

QG_ICON="⚪"
QG_STATUS="N/A"
if [ -n "$ANALYSIS_ID" ]; then
  QG=$(sonar_get "/api/qualitygates/project_status?analysisId=$ANALYSIS_ID") && {
    QG_STATUS=$(echo "$QG" | jq -r '.projectStatus.status // "N/A"')
    if [ "$QG_STATUS" = "OK" ]; then
      QG_ICON="✅"
    elif [ "$QG_STATUS" = "ERROR" ]; then
      QG_ICON="❌"
    fi
  }
fi

# Unresolved issues breakdown (Clean Code impact severity / software quality); skipped silently
# on servers that do not expose these facets.
facet_line() {
  echo "$FACETS" | jq -r --arg prop "$1" \
    '[.facets[]? | select(.property == $prop) | .values[] | select(.count > 0) | "\(.val): \(.count)"] | join(" · ")'
}
ISSUES_BY_SEVERITY=""
ISSUES_BY_QUALITY=""
if FACETS=$(sonar_get "/api/issues/search?componentKeys=$PROJECT_KEY_URI&resolved=false&ps=1&facets=impactSeverities,impactSoftwareQualities"); then
  ISSUES_BY_SEVERITY=$(facet_line impactSeverities)
  ISSUES_BY_QUALITY=$(facet_line impactSoftwareQualities)
fi

{
  echo "### SonarQube - $TITLE"
  echo ""
  echo "Quality Gate: $QG_ICON **$QG_STATUS** — [view dashboard]($SONAR_HOST_URL/dashboard?id=$PROJECT_KEY)"
  echo ""
  echo "| Bugs | Vulnerabilities | Code Smells | Security Hotspots | Coverage | Duplication | Lines of Code |"
  echo "|---|---|---|---|---|---|---|"
  echo "| $BUGS | $VULNERABILITIES | $CODE_SMELLS | $HOTSPOTS | $COVERAGE | $DUPLICATION | $NCLOC |"
  echo ""
} >> "$GITHUB_STEP_SUMMARY"
{
  if [ -n "$ISSUES_BY_SEVERITY" ]; then
    echo "Open issues by severity: $ISSUES_BY_SEVERITY"
    echo ""
    echo "Open issues by software quality: ${ISSUES_BY_QUALITY:-none}"
    echo ""
  fi
} >> "$GITHUB_STEP_SUMMARY"

if [ "$GATE_ENABLED" = true ]; then
  QUERY="/api/issues/search?componentKeys=$PROJECT_KEY_URI&resolved=false&ps=1"
  [ -n "$FAIL_ON_SEVERITIES" ] && QUERY="$QUERY&impactSeverities=$FAIL_ON_SEVERITIES"
  [ -n "$FAIL_ON_QUALITIES" ] && QUERY="$QUERY&impactSoftwareQualities=$FAIL_ON_QUALITIES"
  GATE_RESPONSE=$(sonar_get "$QUERY") || bail "Failed to call the SonarQube API (issues/search)"
  MATCHING=$(echo "$GATE_RESPONSE" | jq -r '.total // empty')
  [ -n "$MATCHING" ] || bail "Unexpected response from the SonarQube API (issues/search)"
  FILTERS="severity=${FAIL_ON_SEVERITIES:-any}, quality=${FAIL_ON_QUALITIES:-any}"
  if [ "$MATCHING" -gt 0 ]; then
    echo "Issue gate: ❌ **$MATCHING** open issue(s) matching ($FILTERS) — [see them]($SONAR_HOST_URL/project/issues?id=$PROJECT_KEY&resolved=false&impactSeverities=$FAIL_ON_SEVERITIES&impactSoftwareQualities=$FAIL_ON_QUALITIES)" >> "$GITHUB_STEP_SUMMARY"
    echo "::error::[$TITLE] $MATCHING open SonarQube issue(s) matching ($FILTERS)"
    exit 1
  fi
  echo "Issue gate: ✅ no open issue matching ($FILTERS)" >> "$GITHUB_STEP_SUMMARY"
  echo "" >> "$GITHUB_STEP_SUMMARY"
fi
