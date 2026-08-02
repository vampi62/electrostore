#!/usr/bin/env bash
set -uo pipefail

# Required env: SONAR_HOST_URL, SONAR_TOKEN, PROJECT_KEY. Optional: SUMMARY_TITLE.
TITLE="${SUMMARY_TITLE:-$PROJECT_KEY}"

# report-task.txt is written by the scanner (location varies between the CLI
# and .NET scanners), so just look for it anywhere in the workspace.
REPORT_TASK_FILE=$(find "${GITHUB_WORKSPACE:-.}" -name 'report-task.txt' -print -quit 2>/dev/null)

if [ -z "$REPORT_TASK_FILE" ]; then
  echo "::warning::[$TITLE] report-task.txt not found, skipping SonarQube summary"
  exit 0
fi

CE_TASK_ID=$(grep '^ceTaskId=' "$REPORT_TASK_FILE" | cut -d'=' -f2-)
if [ -z "$CE_TASK_ID" ]; then
  echo "::warning::[$TITLE] ceTaskId not found in report-task.txt"
  exit 0
fi

STATUS="PENDING"
ANALYSIS_ID=""
for _ in $(seq 1 30); do
  RESPONSE=$(curl -sf -u "$SONAR_TOKEN:" "$SONAR_HOST_URL/api/ce/task?id=$CE_TASK_ID") || {
    echo "::warning::[$TITLE] Failed to call the SonarQube API (ce/task)"
    exit 0
  }
  STATUS=$(echo "$RESPONSE" | jq -r '.task.status')
  if [ "$STATUS" = "SUCCESS" ] || [ "$STATUS" = "FAILED" ] || [ "$STATUS" = "CANCELED" ]; then
    ANALYSIS_ID=$(echo "$RESPONSE" | jq -r '.task.analysisId // empty')
    break
  fi
  sleep 5
done

if [ "$STATUS" != "SUCCESS" ]; then
  echo "::warning::[$TITLE] SonarQube analysis did not succeed (status=$STATUS)"
  exit 0
fi

METRIC_KEYS="bugs,vulnerabilities,code_smells,security_hotspots,coverage,duplicated_lines_density,ncloc"
MEASURES=$(curl -sf -u "$SONAR_TOKEN:" "$SONAR_HOST_URL/api/measures/component?component=$(printf '%s' "$PROJECT_KEY" | jq -sRr @uri)&metricKeys=$METRIC_KEYS") || {
  echo "::warning::[$TITLE] Failed to call the SonarQube API (measures/component)"
  exit 0
}

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
  QG=$(curl -sf -u "$SONAR_TOKEN:" "$SONAR_HOST_URL/api/qualitygates/project_status?analysisId=$ANALYSIS_ID") && {
    QG_STATUS=$(echo "$QG" | jq -r '.projectStatus.status // "N/A"')
    if [ "$QG_STATUS" = "OK" ]; then
      QG_ICON="✅"
    elif [ "$QG_STATUS" = "ERROR" ]; then
      QG_ICON="❌"
    fi
  }
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
