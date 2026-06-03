param(
  [string] $WebhookUrl = "https://raymondneil.app.n8n.cloud/webhook/ftap-faq-chat"
)

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$knowledgePath = Join-Path $root "data\faq-knowledge.json"
$knowledge = Get-Content -Raw -Encoding utf8 $knowledgePath | ConvertFrom-Json
$failures = @()

foreach ($entry in $knowledge.entries) {
  $payload = @{
    message = $entry.question
    sessionId = "exact-answer-test-$($entry.number)"
  } | ConvertTo-Json -Compress

  $body = [System.Text.Encoding]::UTF8.GetBytes($payload)
  $response = Invoke-RestMethod -Method Post -Uri $WebhookUrl -ContentType "application/json; charset=utf-8" -Body $body -TimeoutSec 120

  if ($response.answer -ne $entry.answer) {
    $failures += [pscustomobject]@{
      number = $entry.number
      question = $entry.question
      expected = $entry.answer
      actual = $response.answer
    }
  } else {
    Write-Host "FAQ #$($entry.number): matched answer exactly"
  }
}

if ($failures.Count -gt 0) {
  $failures | ConvertTo-Json -Depth 5
  throw "$($failures.Count) FAQ answer(s) did not match the knowledge base exactly."
}

Write-Host "All $($knowledge.entries.Count) FAQ answers matched exactly."

$unrelatedPayload = @{
  message = "who is jesse"
  sessionId = "unrelated-question-test"
} | ConvertTo-Json -Compress

$unrelatedBody = [System.Text.Encoding]::UTF8.GetBytes($unrelatedPayload)
$unrelatedResponse = Invoke-RestMethod -Method Post -Uri $WebhookUrl -ContentType "application/json; charset=utf-8" -Body $unrelatedBody -TimeoutSec 120

if ($null -ne $unrelatedResponse.matchedFaq -or $unrelatedResponse.answer -notlike "*FTAP SAM FAQ knowledge base*") {
  $unrelatedResponse | ConvertTo-Json -Depth 5
  throw "Unrelated question did not return the FAQ-scope fallback."
}

Write-Host "Unrelated question: returned FAQ-scope fallback."
