using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryWiki.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(MemoryWikiDbContext))]
[Migration("20260101000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "transcripts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                ObjectKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                FailureReason = table.Column<string>(type: "text", nullable: true),
                TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_transcripts", x => x.Id));

        migrationBuilder.CreateTable(
            name: "processing_jobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TranscriptId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                LastError = table.Column<string>(type: "text", nullable: true),
                IdempotencyKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_processing_jobs", x => x.Id));

        migrationBuilder.CreateTable(
            name: "memory_documents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                ObjectKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_memory_documents", x => x.Id));

        migrationBuilder.CreateTable(
            name: "audit_logs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EntityId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                DataJson = table.Column<string>(type: "text", nullable: true),
                Actor = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_audit_logs", x => x.Id));

        migrationBuilder.CreateIndex("IX_transcripts_TenantId", "transcripts", "TenantId");
        migrationBuilder.CreateIndex("IX_processing_jobs_TranscriptId", "processing_jobs", "TranscriptId", unique: true);
        migrationBuilder.CreateIndex("IX_memory_documents_Path", "memory_documents", "Path", unique: true);
        migrationBuilder.CreateIndex("IX_memory_documents_Type_IsDeleted", "memory_documents", new[] { "Type", "IsDeleted" });
        migrationBuilder.CreateIndex("IX_audit_logs_CreatedAtUtc", "audit_logs", "CreatedAtUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("audit_logs");
        migrationBuilder.DropTable("memory_documents");
        migrationBuilder.DropTable("processing_jobs");
        migrationBuilder.DropTable("transcripts");
    }
}
