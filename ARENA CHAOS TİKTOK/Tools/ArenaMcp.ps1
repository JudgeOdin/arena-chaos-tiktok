param([string]$Tool, [string]$Json = '{}', [string]$Resource)
$ErrorActionPreference = 'Stop'
$endpoint = 'http://127.0.0.1:8080/mcp'
$headers = @{Accept='application/json, text/event-stream'}
function Send-Rpc([string]$method, $parameters, [int]$id=1) {
    $body = @{jsonrpc='2.0';id=$id;method=$method;params=$parameters} | ConvertTo-Json -Depth 35 -Compress
    $response = Invoke-WebRequest -UseBasicParsing -Uri $endpoint -Method Post -Headers $headers -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 50
    $script:lastHeaders = $response.Headers
    $payloads = @($response.Content -split "`n" | Where-Object { $_.StartsWith('data: ') })
    if ($payloads.Count -gt 0) { return ($payloads[-1].Substring(6) | ConvertFrom-Json) }
    return ($response.Content | ConvertFrom-Json)
}
$init = Send-Rpc 'initialize' @{protocolVersion='2024-11-05';capabilities=@{};clientInfo=@{name='arena-development';version='1.0'}}
$headers['Mcp-Session-Id'] = $lastHeaders['mcp-session-id']
if ($Tool -ne 'set_active_instance') {
    $selection = Send-Rpc 'tools/call' @{name='set_active_instance';arguments=@{instance='2e93c82439773362'}} 2
}
if ($Resource) { $result = Send-Rpc 'resources/read' @{uri=$Resource} }
else { $arguments = $Json | ConvertFrom-Json; $result = Send-Rpc 'tools/call' @{name=$Tool;arguments=$arguments} }
if ($result.error) { $result.error | ConvertTo-Json -Depth 20; exit 1 }
if ($result.result.structuredContent) { $result.result.structuredContent | ConvertTo-Json -Depth 30 }
elseif ($result.result.contents) { $result.result.contents | ForEach-Object { $_.text } }
else { $result.result.content | ForEach-Object { $_.text } }
