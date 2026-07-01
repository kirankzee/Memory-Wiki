# End-to-end smoke test against the running stack (docker compose up).
# Usage: pwsh ./scripts/demo.ps1
$ErrorActionPreference = "Stop"
$api = if ($env:API) { $env:API } else { "http://localhost:8080" }
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "1) Get JWT token (admin/admin)…"
$auth = Invoke-RestMethod -Method Post -Uri "$api/api/auth/token" -ContentType "application/json" `
  -Body (@{ username = "admin"; password = "admin" } | ConvertTo-Json)
$token = $auth.accessToken
$headers = @{ Authorization = "Bearer $token" }
Write-Host "   token acquired ($($token.Length) chars)"

Write-Host "2) Upload transcript…"
$form = @{
  title = "MemoryWiki Project Kickoff"
  file  = Get-Item "$dir/sample-transcript.txt"
}
$created = Invoke-RestMethod -Method Post -Uri "$api/api/transcripts" -Headers $headers -Form $form
$id = $created.id
Write-Host "   transcript id: $id"

Write-Host "3) Poll status until Completed…"
for ($i = 0; $i -lt 30; $i++) {
  $t = Invoke-RestMethod -Uri "$api/api/transcripts/$id" -Headers $headers
  Write-Host "   status: $($t.status)"
  if ($t.status -eq "Completed") { break }
  if ($t.status -eq "Failed") { throw "processing failed: $($t.failureReason)" }
  Start-Sleep -Seconds 2
}

Write-Host "4) ls /people"
Invoke-RestMethod -Uri "$api/api/memory/ls?path=/people" -Headers $headers | ConvertTo-Json -Depth 6

Write-Host "5) cat /people/alice.md"
Invoke-RestMethod -Uri "$api/api/memory/cat?path=/people/alice.md" -Headers $headers

Write-Host "6) grep RabbitMQ"
Invoke-RestMethod -Uri "$api/api/memory/grep?q=RabbitMQ" -Headers $headers | ConvertTo-Json -Depth 6

Write-Host "Done."
