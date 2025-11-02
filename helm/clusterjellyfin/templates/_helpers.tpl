{{/*
Expand the name of the chart.
*/}}
{{- define "clusterjellyfin.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "clusterjellyfin.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "clusterjellyfin.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "clusterjellyfin.labels" -}}
helm.sh/chart: {{ include "clusterjellyfin.chart" . }}
{{ include "clusterjellyfin.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "clusterjellyfin.selectorLabels" -}}
app.kubernetes.io/name: {{ include "clusterjellyfin.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "clusterjellyfin.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "clusterjellyfin.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
PostgreSQL host
*/}}
{{- define "clusterjellyfin.postgresqlHost" -}}
{{- if .Values.postgresql.enabled }}
{{- printf "%s-postgresql" (include "clusterjellyfin.fullname" .) }}
{{- else }}
{{- .Values.externalPostgresql.host }}
{{- end }}
{{- end }}

{{/*
PostgreSQL port
*/}}
{{- define "clusterjellyfin.postgresqlPort" -}}
{{- if .Values.postgresql.enabled }}
{{- 5432 }}
{{- else }}
{{- .Values.externalPostgresql.port }}
{{- end }}
{{- end }}

{{/*
PostgreSQL database
*/}}
{{- define "clusterjellyfin.postgresqlDatabase" -}}
{{- if .Values.postgresql.enabled }}
{{- .Values.postgresql.auth.database }}
{{- else }}
{{- .Values.externalPostgresql.database }}
{{- end }}
{{- end }}

{{/*
PostgreSQL username
*/}}
{{- define "clusterjellyfin.postgresqlUsername" -}}
{{- if .Values.postgresql.enabled }}
{{- .Values.postgresql.auth.username }}
{{- else }}
{{- .Values.externalPostgresql.username }}
{{- end }}
{{- end }}

{{/*
PostgreSQL secret name
*/}}
{{- define "clusterjellyfin.postgresqlSecretName" -}}
{{- if .Values.postgresql.enabled }}
{{- printf "%s-postgresql" (include "clusterjellyfin.fullname" .) }}
{{- else if .Values.externalPostgresql.existingSecret }}
{{- .Values.externalPostgresql.existingSecret }}
{{- else }}
{{- printf "%s-postgresql" (include "clusterjellyfin.fullname" .) }}
{{- end }}
{{- end }}

{{/*
PostgreSQL secret password key
*/}}
{{- define "clusterjellyfin.postgresqlSecretPasswordKey" -}}
{{- if .Values.postgresql.enabled }}
{{- "password" }}
{{- else if .Values.externalPostgresql.existingSecret }}
{{- .Values.externalPostgresql.existingSecretPasswordKey }}
{{- else }}
{{- "password" }}
{{- end }}
{{- end }}

{{/*
Image pull secrets
*/}}
{{- define "clusterjellyfin.imagePullSecrets" -}}
{{- if .Values.image.pullSecrets }}
imagePullSecrets:
{{- range .Values.image.pullSecrets }}
  - name: {{ . }}
{{- end }}
{{- end }}
{{- end }}
