Below is the Claude Code prompt converted into a structured requirements table for easier review and implementation planning. It is based on the uploaded MemoryWiki architecture and setup guide.

| **Category**    | **Requirement**    | **Technology / Details**             |
| --------------- | ------------------ | ------------------------------------ |
| Project Name    | Solution Name      | MemoryWiki                           |
| Framework       | Backend            | .NET 8, ASP.NET Core Web API         |
| Language        | Programming        | C#                                   |
| Architecture    | Pattern            | Clean Architecture + DDD + CQRS      |
| API Style       | REST API           | Minimal APIs                         |
| Messaging       | Queue              | RabbitMQ                             |
| Database        | Relational         | PostgreSQL                           |
| Object Storage  | Memory Files       | AWS S3 / MinIO                       |
| Authentication  | Security           | JWT Bearer                           |
| AI Integration  | LLM                | OpenAI SDK with provider abstraction |
| Logging         | Observability      | Serilog                              |
| Monitoring      | Telemetry          | OpenTelemetry                        |
| Documentation   | API                | Swagger / OpenAPI                    |
| Validation      | Input              | FluentValidation                     |
| Mapping         | DTO Mapping        | Mapster                              |
| Retry Policy    | Resilience         | Polly                                |
| Background Jobs | Worker             | .NET BackgroundService               |
| Testing         | Unit & Integration | xUnit + TestContainers               |
| Deployment      | Containers         | Docker & Docker Compose              |
| CI/CD           | Pipeline           | GitHub Actions                       |

---

# Solution Structure

| **Project**               | **Purpose**                |
| ------------------------- | -------------------------- |
| MemoryWiki.Api            | REST API                   |
| MemoryWiki.Application    | CQRS, Commands, Queries    |
| MemoryWiki.Domain         | Domain Models              |
| MemoryWiki.Infrastructure | EF Core, RabbitMQ, Storage |
| MemoryWiki.Worker         | Background Processing      |
| MemoryWiki.Contracts      | DTOs & Contracts           |
| MemoryWiki.Shared         | Shared Utilities           |
| Application.Tests         | Unit Tests                 |
| Integration.Tests         | Integration Tests          |
| Api.Tests                 | API Tests                  |

---

# Domain Layer

| **Component** | **Items**                                                |
| ------------- | -------------------------------------------------------- |
| Entities      | Transcript, MemoryDocument, MemoryNode, ProcessingJob    |
| Value Objects | TranscriptId, MemoryPath, PersonName, ProjectName        |
| Enums         | JobStatus, TranscriptStatus, MemoryType                  |
| Repositories  | ITranscriptRepository, IMemoryRepository, IJobRepository |
| Domain Events | TranscriptSubmitted, MemoryGenerated                     |

---

# Application Layer

| **Category** | **Components**                                                          |
| ------------ | ----------------------------------------------------------------------- |
| Commands     | CreateTranscriptCommand, GenerateMemoryCommand                          |
| Queries      | GetTranscriptQuery, ListMemoryQuery, ReadMemoryQuery, SearchMemoryQuery |
| Handlers     | MediatR Handlers                                                        |
| Validation   | FluentValidation                                                        |
| Mapping      | Mapster                                                                 |
| DTOs         | Request/Response Models                                                 |

---

# Infrastructure Layer

| **Component**          | **Description**                |
| ---------------------- | ------------------------------ |
| PostgreSQL             | Transcript Metadata            |
| RabbitMQ               | Queue Processing               |
| OpenAI Service         | LLM Generation                 |
| S3 / MinIO             | Markdown Memory Storage        |
| Object Storage Service | Upload, Download, List, Search |
| Prompt Builder         | Generates AI Prompt            |
| Retry Policy           | Polly                          |

---

# API Endpoints

| **Endpoint**          | **Method** | **Description**     |
| --------------------- | ---------- | ------------------- |
| /api/auth/token       | POST       | Generate JWT Token  |
| /api/transcripts      | POST       | Upload Transcript   |
| /api/transcripts/{id} | GET        | Transcript Status   |
| /api/memory/ls        | GET        | List Memory Folder  |
| /api/memory/cat       | GET        | Read Markdown File  |
| /api/memory/grep      | GET        | Search Memory Files |
| /api/health           | GET        | Health Check        |
| /swagger              | GET        | API Documentation   |

---

# Worker Pipeline

| **Step** | **Action**                      |
| -------- | ------------------------------- |
| 1        | Receive RabbitMQ Message        |
| 2        | Load Transcript                 |
| 3        | Build AI Prompt                 |
| 4        | Call OpenAI                     |
| 5        | Generate Markdown               |
| 6        | Save Markdown to Object Storage |
| 7        | Update PostgreSQL               |
| 8        | Publish Completion Event        |

---

# Database Tables

| **Table**   | **Purpose**                  |
| ----------- | ---------------------------- |
| Transcripts | Uploaded Transcript Metadata |
| Jobs        | Queue Processing Status      |
| MemoryFiles | Markdown Metadata            |
| AuditLogs   | Audit Trail                  |

---

# Object Storage Structure

| **Folder** | **Purpose**             |
| ---------- | ----------------------- |
| /people    | Person Memories         |
| /projects  | Project Knowledge       |
| /topics    | Topic Knowledge         |
| /events    | Meeting & Event History |

---

# Security

| **Feature**     | **Implementation**          |
| --------------- | --------------------------- |
| Authentication  | JWT Bearer                  |
| Authorization   | Role-Based                  |
| Rate Limiting   | ASP.NET Rate Limiter        |
| File Validation | File Type & Size Validation |
| Virus Scan      | Abstract Scanner Interface  |

---

# Observability

| **Feature**         | **Technology**        |
| ------------------- | --------------------- |
| Logging             | Serilog               |
| Metrics             | OpenTelemetry         |
| Distributed Tracing | OpenTelemetry         |
| Health Checks       | ASP.NET Health Checks |

---

# Docker Services

| **Container** | **Purpose**             |
| ------------- | ----------------------- |
| API           | REST Service            |
| Worker        | Background Processing   |
| PostgreSQL    | Database                |
| RabbitMQ      | Message Queue           |
| MinIO         | Object Storage          |
| pgAdmin       | Database Administration |

---

# Testing

| **Test Type**     | **Framework**     |
| ----------------- | ----------------- |
| Unit Tests        | xUnit             |
| Integration Tests | TestContainers    |
| API Tests         | ASP.NET Test Host |
| Worker Tests      | xUnit             |

---

# CI/CD Pipeline

| **Stage** | **Action**                       |
| --------- | -------------------------------- |
| Restore   | Restore NuGet Packages           |
| Build     | Compile Solution                 |
| Test      | Execute Unit & Integration Tests |
| Publish   | Build Docker Images              |
| Deploy    | Push to Registry                 |

---

# Optional Enterprise Features

| **Feature**             | **Description**                         |
| ----------------------- | --------------------------------------- |
| Semantic Search         | pgvector / Vector Embeddings            |
| SignalR                 | Real-Time Upload Progress               |
| Multi-LLM Support       | OpenAI, Azure OpenAI, Ollama, Anthropic |
| Memory Versioning       | Track Document History                  |
| Audit History           | Record Changes                          |
| Soft Delete             | Recover Deleted Files                   |
| Graph Relationships     | Knowledge Graph                         |
| Hybrid Search           | Keyword + Vector Search                 |
| Redis Cache             | Faster Memory Retrieval                 |
| Kubernetes              | Helm Charts + KEDA Autoscaling          |
| CloudEvents             | Event-Driven Integration                |
| Multi-Tenant Support    | Tenant Isolation                        |
| Large File Upload       | Resumable Uploads                       |
| AI Provider Abstraction | Pluggable LLM Providers                 |

This tabular format can be used directly as a **software requirements specification (SRS)**, **GitHub Copilot instruction**, or **Claude Code implementation checklist**.
