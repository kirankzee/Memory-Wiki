# MemoryWiki — API Reference

Base URL (local): `http://localhost:8080`
Interactive OpenAPI/Swagger UI: `http://localhost:8080/swagger`
OpenAPI JSON: `http://localhost:8080/swagger/v1/swagger.json`

All endpoints except `/api/auth/token` and the health checks require a
`Authorization: Bearer <jwt>` header. Optional multi-tenancy via `X-Tenant-Id: <tenant>`.

---

## Auth

### `POST /api/auth/token`
Exchange demo credentials for a JWT. Seeded users: `admin/admin` (Admin), `user/user` (User).

Request:
```json
{ "username": "admin", "password": "admin" }
```
Response `200`:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "tokenType": "Bearer",
  "expiresUtc": "2026-06-28T12:00:00Z",
  "role": "Admin"
}
```
`401` on bad credentials.

---

## Transcripts

### `POST /api/transcripts`
`multipart/form-data` — fields: `title` (string, optional → defaults to filename), `file` (the transcript).

Request (curl):
```bash
curl -X POST http://localhost:8080/api/transcripts \
  -H "Authorization: Bearer $TOKEN" \
  -F "title=MemoryWiki Project Kickoff" \
  -F "file=@scripts/sample-transcript.txt"
```
Response `201`:
```json
{ "id": "f1e2d3c4-...-abc", "status": "Queued" }
```
Errors: `400` (missing/empty file, > 5 MB, empty content), `401`.

### `GET /api/transcripts/{id}`
Response `200`:
```json
{
  "id": "f1e2d3c4-...-abc",
  "title": "MemoryWiki Project Kickoff",
  "status": "Completed",
  "sizeBytes": 1423,
  "failureReason": null,
  "createdAtUtc": "2026-06-28T11:59:00Z",
  "updatedAtUtc": "2026-06-28T11:59:06Z"
}
```
`status` ∈ `Received | Queued | Processing | Completed | Failed`. `404` if unknown.

---

## Memory (unix-style)

### `GET /api/memory/ls?path=/people`  — list a directory
Response `200`:
```json
{
  "path": "/people",
  "entries": [
    { "name": "alice.md", "path": "/people/alice.md", "kind": "file", "sizeBytes": 612, "lastModifiedUtc": "2026-06-28T11:59:06Z" },
    { "name": "bob.md",   "path": "/people/bob.md",   "kind": "file", "sizeBytes": 540, "lastModifiedUtc": "2026-06-28T11:59:06Z" }
  ]
}
```
`path=/` lists the top-level directories (`people`, `projects`, `topics`, `events`).

### `GET /api/memory/cat?path=/people/alice.md`  — read a file
Returns raw `text/markdown`:
```markdown
# Alice
## Summary
Alice is a participant.
## Responsibilities
- Owns the transcript and memory endpoints.
...
<!-- sources: MemoryWiki Project Kickoff -->
```
`404` if the file does not exist. Directory paths return `404` (use `ls`).

### `GET /api/memory/grep?q=RabbitMQ&ignoreCase=true&max=200`  — search
`q` is treated as a regex (falls back to a literal if invalid). Response `200`:
```json
{
  "query": "RabbitMQ",
  "fileCount": 4,
  "matchCount": 3,
  "matches": [
    { "path": "/projects/memorywiki-project-kickoff.md", "lineNumber": 12, "line": "- We will publish a message to RabbitMQ ..." },
    { "path": "/people/alice.md", "lineNumber": 6, "line": "- We will publish a message to RabbitMQ ..." }
  ]
}
```

---

## Health

| Endpoint | Meaning |
|----------|---------|
| `GET /api/health` | Liveness — `Healthy` when the process is up |
| `GET /api/health/ready` | Readiness — checks PostgreSQL connectivity, returns JSON |

---

## Error format (RFC 7807)
```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation failed",
  "status": 400,
  "detail": "Content: Transcript content is empty.",
  "traceId": "00-...-01",
  "errors": ["Content: Transcript content is empty."]
}
```

| Status | When |
|--------|------|
| 400 | validation / bad input / traversal |
| 401 | missing/invalid JWT |
| 404 | unknown transcript or memory file |
| 409 | conflicting state |
| 429 | rate limit exceeded |
| 500 | unexpected error |
