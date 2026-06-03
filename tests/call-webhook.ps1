param(
  [string] $WebhookUrl = "http://localhost:5678/webhook-test/ftap-faq-chat",
  [string] $Message = "What is the iAMS PIN code?"
)

$payload = @{
  message = $Message
  sessionId = "demo-1"
} | ConvertTo-Json

$body = [System.Text.Encoding]::UTF8.GetBytes($payload)
Invoke-RestMethod -Method Post -Uri $WebhookUrl -ContentType "application/json; charset=utf-8" -Body $body
