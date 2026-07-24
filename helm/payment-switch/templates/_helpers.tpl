{{- define "payment-switch.name" -}}
{{- default .Chart.Name .Values.global.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "payment-switch.fullname" -}}
{{- $name := default .Chart.Name .Values.global.nameOverride }}
{{- printf "%s-%s" $name .service.name | lower | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "payment-switch.labels" -}}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version }}
app.kubernetes.io/part-of: payment-switch
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "payment-switch.image" -}}
{{- $registry := .Values.global.imageRegistry }}
{{- $repository := .image.repository }}
{{- $tag := .image.tag | default .Chart.AppVersion }}
{{- if $registry }}
{{- printf "%s/%s:%s" $registry $repository $tag }}
{{- else }}
{{- printf "%s:%s" $repository $tag }}
{{- end }}
{{- end }}
