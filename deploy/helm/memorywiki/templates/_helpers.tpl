{{- define "memorywiki.env" -}}
- name: ConnectionStrings__Postgres
  value: {{ .Values.config.connectionStrings.postgres | quote }}
- name: S3__ServiceUrl
  value: {{ .Values.config.s3.serviceUrl | quote }}
- name: S3__Bucket
  value: {{ .Values.config.s3.bucket | quote }}
- name: S3__AccessKey
  valueFrom: { secretKeyRef: { name: memorywiki-secrets, key: s3AccessKey } }
- name: S3__SecretKey
  valueFrom: { secretKeyRef: { name: memorywiki-secrets, key: s3SecretKey } }
- name: RabbitMQ__Host
  value: {{ .Values.config.rabbitmq.host | quote }}
- name: Llm__Provider
  value: {{ .Values.config.llm.provider | quote }}
- name: OpenAI__ApiKey
  valueFrom: { secretKeyRef: { name: memorywiki-secrets, key: openAiApiKey } }
{{- end -}}
