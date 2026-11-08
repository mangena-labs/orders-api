# templates/_helpers.tpl
{{- define "orders-api.labels" -}}
app.kubernetes.io/name: orders-api
app.kubernetes.io/part-of: orders
app.kubernetes.io/version: {{ .Values.image.tag | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "orders-api.selectorLabels" -}}
app.kubernetes.io/name: orders-api
{{- end }}

{{/* Entra login, no password: the pod's Workload Identity is the credential */}}
{{- define "orders-api.connectionString" -}}
Server=tcp:{{ .Values.config.sqlServer }},1433;Database={{ .Values.config.sqlDatabase }};Authentication=Active Directory Default;Encrypt=True;
{{- end }}

{{- define "orders-api.podSecurityContext" -}}
runAsNonRoot: true
runAsUser: 1654                 # the numeric APP_UID from the .NET image
runAsGroup: 1654
seccompProfile:
  type: RuntimeDefault
{{- end }}

{{- define "orders-api.containerSecurityContext" -}}
allowPrivilegeEscalation: false
readOnlyRootFilesystem: true
capabilities:
  drop: ["ALL"]
{{- end }}
