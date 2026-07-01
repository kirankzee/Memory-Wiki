#!/usr/bin/env bash
# End-to-end smoke test against the running stack (docker compose up).
# Usage: ./scripts/demo.sh
set -euo pipefail

API="${API:-http://localhost:8080}"
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "1) Get JWT token (admin/admin)…"
TOKEN=$(curl -s -X POST "$API/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}' | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')
echo "   token acquired (${#TOKEN} chars)"

echo "2) Upload transcript…"
ID=$(curl -s -X POST "$API/api/transcripts" \
  -H "Authorization: Bearer $TOKEN" \
  -F "title=MemoryWiki Project Kickoff" \
  -F "file=@$DIR/sample-transcript.txt" | sed -n 's/.*"id":"\([^"]*\)".*/\1/p')
echo "   transcript id: $ID"

echo "3) Poll status until Completed…"
for _ in $(seq 1 30); do
  STATUS=$(curl -s "$API/api/transcripts/$ID" -H "Authorization: Bearer $TOKEN" | sed -n 's/.*"status":"\([^"]*\)".*/\1/p')
  echo "   status: $STATUS"
  [ "$STATUS" = "Completed" ] && break
  [ "$STATUS" = "Failed" ] && { echo "   processing failed"; exit 1; }
  sleep 2
done

echo "4) ls /people"
curl -s "$API/api/memory/ls?path=/people" -H "Authorization: Bearer $TOKEN"; echo

echo "5) ls /projects"
curl -s "$API/api/memory/ls?path=/projects" -H "Authorization: Bearer $TOKEN"; echo

echo "6) cat /people/alice.md"
curl -s "$API/api/memory/cat?path=/people/alice.md" -H "Authorization: Bearer $TOKEN"; echo

echo "7) grep RabbitMQ"
curl -s "$API/api/memory/grep?q=RabbitMQ" -H "Authorization: Bearer $TOKEN"; echo

echo "Done."
