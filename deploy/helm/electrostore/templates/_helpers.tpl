{{/* Common labels */}}
{{- define "es.labels" -}}
app.kubernetes.io/part-of: electrostore
app.kubernetes.io/instance: {{ .Values.instance.name | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version | quote }}
{{- end -}}

{{/* Selector labels. Usage: include "es.selector" (dict "root" . "component" "api") */}}
{{- define "es.selector" -}}
app.kubernetes.io/component: {{ .component }}
app.kubernetes.io/instance: {{ .root.Values.instance.name | quote }}
{{- end -}}

{{/* Image of an application service. Usage: include "es.image" (dict "root" . "name" "api") */}}
{{- define "es.image" -}}
{{- $svc := index .root.Values .name -}}
{{- printf "%s/%s:%s" .root.Values.image.registry .name (default .root.Values.image.tag $svc.image.tag) -}}
{{- end -}}

{{/* Public hostnames and scheme */}}
{{- define "es.frontHost" -}}
{{- default (printf "%s.%s" .Values.instance.name .Values.baseDomain) .Values.ingress.frontHost -}}
{{- end -}}

{{- define "es.apiHost" -}}
{{- default (printf "api-%s.%s" .Values.instance.name .Values.baseDomain) .Values.ingress.apiHost -}}
{{- end -}}

{{- define "es.scheme" -}}
{{- if .Values.ingress.tls -}}https{{- else -}}http{{- end -}}
{{- end -}}

{{- define "es.frontUrl" -}}
{{- printf "%s://%s" (include "es.scheme" .) (include "es.frontHost" .) -}}
{{- end -}}

{{- define "es.apiUrl" -}}
{{- printf "%s://%s" (include "es.scheme" .) (include "es.apiHost" .) -}}
{{- end -}}

{{/* Deterministic 64-char hex secret. Usage: include "es.derive" (dict "root" . "purpose" "jwt") */}}
{{- define "es.derive" -}}
{{- printf "%s/%s/%s" .root.Values.secrets.salt .root.Values.instance.name .purpose | sha256sum -}}
{{- end -}}

{{/* Derived credentials */}}
{{- define "es.mariadbRootPassword" -}}{{ include "es.derive" (dict "root" . "purpose" "mariadb-root") | trunc 32 }}{{- end -}}
{{- define "es.mariadbPassword" -}}{{ include "es.derive" (dict "root" . "purpose" "mariadb-user") | trunc 32 }}{{- end -}}
{{- define "es.mqttPassword" -}}{{ include "es.derive" (dict "root" . "purpose" "mqtt") | trunc 32 }}{{- end -}}
{{- define "es.jwtKey" -}}{{ include "es.derive" (dict "root" . "purpose" "jwt") }}{{- end -}}
{{- define "es.aesKey" -}}{{ default (include "es.derive" (dict "root" . "purpose" "aes")) .Values.secrets.aesKey }}{{- end -}}
{{- define "es.s3AccessKey" -}}GK{{ include "es.derive" (dict "root" . "purpose" "s3-access") | trunc 24 }}{{- end -}}
{{- define "es.s3SecretKey" -}}{{ include "es.derive" (dict "root" . "purpose" "s3-secret") }}{{- end -}}
{{- define "es.garageRpcSecret" -}}{{ include "es.derive" (dict "root" . "purpose" "garage-rpc") }}{{- end -}}
{{- define "es.garageAdminToken" -}}{{ include "es.derive" (dict "root" . "purpose" "garage-admin") }}{{- end -}}

{{/*
Hardened pod/container defaults for the .NET services (read-only root filesystem, non-root `app` user
of the aspnet images, writable /tmp mounted separately).
*/}}
{{- define "es.dotnetPodSecurity" -}}
runAsNonRoot: true
runAsUser: 1654
runAsGroup: 1654
seccompProfile:
  type: RuntimeDefault
{{- end -}}

{{- define "es.containerSecurity" -}}
allowPrivilegeEscalation: false
readOnlyRootFilesystem: true
capabilities:
  drop: ["ALL"]
{{- end -}}

{{/* Environment shared by the .NET services */}}
{{- define "es.dotnetEnv" -}}
- name: ASPNETCORE_ENVIRONMENT
  value: {{ .Values.environment | quote }}
- name: HOME
  value: /tmp
- name: DOTNET_EnableDiagnostics
  value: "0"
{{- end -}}
