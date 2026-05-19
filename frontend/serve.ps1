param(
    [int]$Port = 5173
)

$root = [System.IO.Path]::GetFullPath($PSScriptRoot)
$prefix = "http://localhost:$Port/"
$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add($prefix)

function Get-ContentType {
    param([string]$Path)

    switch ([System.IO.Path]::GetExtension($Path).ToLowerInvariant()) {
        ".css" { "text/css; charset=utf-8" }
        ".html" { "text/html; charset=utf-8" }
        ".js" { "text/javascript; charset=utf-8" }
        ".json" { "application/json; charset=utf-8" }
        ".svg" { "image/svg+xml" }
        ".webmanifest" { "application/manifest+json; charset=utf-8" }
        ".xml" { "application/xml; charset=utf-8" }
        default { "application/octet-stream" }
    }
}

function Set-CommonHeaders {
    param(
        [System.Net.HttpListenerResponse]$Response,
        [string]$Path
    )

    $fileName = [System.IO.Path]::GetFileName($Path).ToLowerInvariant()

    if ($fileName -in @("sw.js", "serviceworkercleanup.js", "config.js", "index.html", "sw-reset.html")) {
        $Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0"
        $Response.Headers["Pragma"] = "no-cache"
        $Response.Headers["Expires"] = "0"
    }

    if ($fileName -eq "sw.js") {
        $Response.Headers["Service-Worker-Allowed"] = "/"
    }
}

try {
    $listener.Start()
    Write-Host "Serving frontend at $prefix"
    Write-Host "Press Ctrl+C to stop."

    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $requestPath = [System.Uri]::UnescapeDataString($context.Request.Url.AbsolutePath.TrimStart("/"))

        if ([string]::IsNullOrWhiteSpace($requestPath)) {
            $requestPath = "index.html"
        }

        $candidatePath = [System.IO.Path]::GetFullPath((Join-Path $root $requestPath))
        if (-not $candidatePath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
            $context.Response.StatusCode = 403
            $context.Response.Close()
            continue
        }

        if ([System.IO.Directory]::Exists($candidatePath)) {
            $candidatePath = Join-Path $candidatePath "index.html"
        }

        if (-not [System.IO.File]::Exists($candidatePath)) {
            $context.Response.StatusCode = 404
            $context.Response.Close()
            continue
        }

        $bytes = [System.IO.File]::ReadAllBytes($candidatePath)
        $context.Response.ContentType = Get-ContentType $candidatePath
        Set-CommonHeaders $context.Response $candidatePath
        $context.Response.ContentLength64 = $bytes.Length
        $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        $context.Response.Close()
    }
}
finally {
    if ($listener.IsListening) {
        $listener.Stop()
    }

    $listener.Close()
}
