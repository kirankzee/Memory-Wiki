# MemoryWiki — Complete Setup Guide (Step by Step)

This guide takes you from a clean machine to a fully running MemoryWiki stack and a
verified end-to-end demo. The default configuration runs **completely offline** — no
API keys required — by using a deterministic local "LLM". Switch to OpenAI in one step
when you want real model output.

---

## 0. What you'll end up with

| Component | URL / Port | Purpose |
|-----------|------------|---------|
| API + Swagger | http://localhost:8080/swagger | REST API (transcripts + memory ls/cat/grep) |
| Worker | (no port) | Background memory generator |
| PostgreSQL | localhost:5432 | Transcript / job / memory metadata |
| RabbitMQ UI | http://localhost:15672 (guest/guest) | Queue `memory.generate` |
| MinIO Console | http://localhost:9001 (minioadmin/minioadmin) | Object store (memory tree) |
| pgAdmin | http://localhost:5050 (admin@memorywiki.dev / admin) | DB browser |

---

## 1. Prerequisites

Install **one** of the two paths:

### Path A — Docker only (recommended, single command)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) 4.x+ (includes Docker Compose v2)
- 4 GB free RAM

Verify:
```bash
docker --version
docker compose version
```

### Path B — Local .NET development (optional)
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- Docker (still needed for Postgres/RabbitMQ/MinIO)

Verify:
```bash
dotnet --version   # 8.0.x or newer
```

---

## 2. Get the code

```bash
git clone <your-repo-url> memorywiki
cd memorywiki
```

Folder layout:
```
MemoryWiki/
├─ src/            # Api, Worker, Application, Domain, Infrastructure, Contracts, Shared
├─ tests/          # Application.Tests (unit), Integration.Tests (Testcontainers)
├─ deploy/helm/    # Kubernetes Helm chart (+ KEDA autoscaling)
├─ scripts/        # demo.sh / demo.ps1 + sample transcript
├─ docs/           # ARCHITECTURE.md, API.md
├─ docker-compose.yml
├─ SETUP.md        # (this file)
└─ README.md
```

---

## 3. Configure environment

Copy the example env file. The defaults work out of the box.

```bash
cp .env.example .env
```

Key settings in `.env`:
```ini
LLM__PROVIDER=Deterministic     # offline, no API key needed
OPENAI__APIKEY=                 # only used when LLM__PROVIDER=OpenAI
JWT__SIGNINGKEY=...             # change for anything public-facing
```

> **Tip:** You can skip this step entirely — `docker compose` falls back to the same
> defaults if `.env` is absent.

---

## 4. Run the entire stack — single command

```bash
docker compose up --build
```

This builds the API + Worker images and starts Postgres, RabbitMQ, MinIO and pgAdmin.
First build takes a few minutes; subsequent runs are cached.

**What happens on startup:**
1. API & Worker wait for Postgres/RabbitMQ/MinIO health checks.
2. The API applies EF Core migrations (`InitialCreate`) automatically.
3. Both services ensure the MinIO bucket `memorywiki` exists.
4. The Worker subscribes to the `memory.generate` queue.

Wait until you see:
```
memorywiki-api-1     ... Now listening on: http://[::]:8080
memorywiki-worker-1  ... Worker listening on memory.generate
```

Run detached instead with `docker compose up --build -d` and follow logs with
`docker compose logs -f api worker`.

---

## 5. Verify it's healthy

```bash
curl http://localhost:8080/api/health        # -> Healthy
curl http://localhost:8080/api/health/ready   # -> {"status":"Healthy",...}
```

Open Swagger UI: <http://localhost:8080/swagger>

---

## 6. End-to-end demo (automated)

The repo ships a one-shot script that authenticates, uploads the sample transcript,
waits for processing, then runs ls / cat / grep.

**Linux / macOS / Git Bash:**
```bash
chmod +x scripts/demo.sh
./scripts/demo.sh
```

**Windows PowerShell:**
```powershell
pwsh ./scripts/demo.ps1
```

Expected tail of output:
```
4) ls /people        -> alice.md, bob.md, carol.md
6) cat /people/alice.md  -> # Alice ... ## Responsibilities ...
7) grep RabbitMQ     -> matches in projects/*.md and people/*.md
```

---

## 7. End-to-end demo (manual, curl)

### 7.1 Get a JWT (demo users: `admin/admin`, `user/user`)
```bash
TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}' | jq -r .accessToken)
```

### 7.2 Upload a transcript (multipart/form-data)
```bash
curl -s -X POST http://localhost:8080/api/transcripts \
  -H "Authorization: Bearer $TOKEN" \
  -F "title=MemoryWiki Project Kickoff" \
  -F "file=@scripts/sample-transcript.txt"
# -> { "id": "....", "status": "Queued" }
```

### 7.3 Check status
```bash
curl -s http://localhost:8080/api/transcripts/<id> -H "Authorization: Bearer $TOKEN"
# status transitions: Queued -> Processing -> Completed
```

### 7.4 Browse the memory tree (ls / cat / grep)
```bash
curl -s "http://localhost:8080/api/memory/ls?path=/"          -H "Authorization: Bearer $TOKEN"
curl -s "http://localhost:8080/api/memory/ls?path=/people"    -H "Authorization: Bearer $TOKEN"
curl -s "http://localhost:8080/api/memory/cat?path=/people/alice.md" -H "Authorization: Bearer $TOKEN"
curl -s "http://localhost:8080/api/memory/grep?q=RabbitMQ"    -H "Authorization: Bearer $TOKEN"
```

### 7.5 See the files in MinIO
Open <http://localhost:9001> → bucket `memorywiki` → folders `people/`, `projects/`,
`topics/`, `events/`.

---

## 8. Demonstrate merge/update (a core evaluation point)

Upload a **second** transcript that mentions an existing person. MemoryWiki loads the
existing markdown file and **merges** new facts in (it never overwrites):

```bash
printf 'Alice: I will also own the deployment pipeline and Kubernetes Helm charts.\n' > /tmp/t2.txt
curl -s -X POST http://localhost:8080/api/transcripts \
  -H "Authorization: Bearer $TOKEN" \
  -F "title=Ops Sync" -F "file=@/tmp/t2.txt"

# After it completes, alice.md now contains BOTH the kickoff facts and the new ones,
# and the provenance footer lists both source transcripts:
curl -s "http://localhost:8080/api/memory/cat?path=/people/alice.md" -H "Authorization: Bearer $TOKEN"
# ... <!-- sources: MemoryWiki Project Kickoff | Ops Sync -->
```

---

## 9. Use a real LLM (OpenAI) — optional

1. Edit `.env`:
   ```ini
   LLM__PROVIDER=OpenAI
   OPENAI__APIKEY=sk-...your key...
   OPENAI__MODEL=gpt-4o-mini
   ```
2. Recreate the API + Worker:
   ```bash
   docker compose up -d --build api worker
   ```
The abstraction (`IGenerationService`) is provider-agnostic; if the model returns
unusable content the service falls back to the deterministic merger, so the pipeline
never hard-fails.

---

## 10. Local development without Docker (optional)

Start only the infrastructure:
```bash
docker compose up -d postgres rabbitmq minio
```
Run the API and Worker from your IDE or terminal (two shells):
```bash
dotnet run --project src/MemoryWiki.Api
dotnet run --project src/MemoryWiki.Worker
```
`appsettings.json` already points at `localhost` for all dependencies.

### EF Core migrations (manual)
Migrations are applied automatically on startup. To manage them by hand:
```bash
dotnet tool install --global dotnet-ef            # once
dotnet ef migrations add <Name> \
  --project src/MemoryWiki.Infrastructure \
  --startup-project src/MemoryWiki.Api
dotnet ef database update \
  --project src/MemoryWiki.Infrastructure \
  --startup-project src/MemoryWiki.Api
```

---

## 11. Run the tests

```bash
# Unit tests (fast, no Docker)
dotnet test tests/MemoryWiki.Application.Tests

# Integration tests (spins up PostgreSQL via Testcontainers — Docker required)
dotnet test tests/MemoryWiki.Integration.Tests

# Everything
dotnet test MemoryWiki.sln
```

---

## 12. Deploy to Kubernetes (optional, bonus)

```bash
# Build & push images to your registry first, then:
helm upgrade --install memorywiki deploy/helm/memorywiki \
  --set image.api.repository=<registry>/memorywiki-api \
  --set image.worker.repository=<registry>/memorywiki-worker \
  --set secrets.jwtSigningKey=$(openssl rand -base64 48)

kubectl port-forward svc/memorywiki-api 8080:80
```
The chart includes an HPA for the API and a **KEDA ScaledObject** that scales the Worker
on RabbitMQ queue depth. Provision Postgres/RabbitMQ/MinIO separately and point
`values.config.*` at them.

---

## 13. Stop / clean up

```bash
docker compose down            # stop containers
docker compose down -v         # stop + delete volumes (Postgres + MinIO data)
```

---

## 14. Troubleshooting

| Symptom | Fix |
|---------|-----|
| API exits with DB connection error | Postgres still starting — the API retries for ~30s; otherwise `docker compose restart api`. |
| `401 Unauthorized` on memory endpoints | Add the `Authorization: Bearer <token>` header (see step 7.1). |
| Upload returns `400` | File missing/empty, or over the 5 MB limit. |
| Transcript stuck in `Queued` | Check the worker: `docker compose logs -f worker`. Ensure RabbitMQ is healthy. |
| `cat` returns 404 | The entity name slugs to a different filename — run `ls /people` first to see exact names. |
| Port already in use | Change the host port mappings in `docker-compose.yml`. |

---

You're done — MemoryWiki is running end-to-end. See [README.md](README.md) for
architecture decisions and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for diagrams.
