$ErrorActionPreference = 'Stop'

if (-not (Test-Path '.\VirtualTryOn.Api.csproj')) {
    throw 'Run this script inside the folder containing VirtualTryOn.Api.csproj.'
}

$tryOnUrl = Read-Host 'Paste the current Colab tunnel HTTPS URL'
$tryOnUri = $null
if (-not [Uri]::TryCreate($tryOnUrl, [UriKind]::Absolute, [ref]$tryOnUri) -or
    $tryOnUri.Scheme -ne 'https') {
    throw 'Enter the complete HTTPS tunnel URL.'
}

$tryOnSecret = Read-Host 'Paste the key from the downloaded .catvton_api_key file' -AsSecureString
$tryOnKey = [System.Net.NetworkCredential]::new('', $tryOnSecret).Password.Trim()
if ([string]::IsNullOrWhiteSpace($tryOnKey)) {
    throw 'The CatVTON key cannot be empty.'
}

& dotnet user-secrets init
if ($LASTEXITCODE -ne 0) { throw 'Could not initialize development secrets.' }

# Pass secrets on stdin so the private key is not embedded in a command argument.
$tryOnSettings = @{
    'TryOnAi:BaseUrl' = $tryOnUri.AbsoluteUri.TrimEnd('/') + '/'
    'TryOnAi:ApiKey' = $tryOnKey
} | ConvertTo-Json -Compress

$tryOnSettings | & dotnet user-secrets set
if ($LASTEXITCODE -ne 0) { throw 'Could not save the CatVTON configuration.' }

Remove-Variable tryOnKey, tryOnSettings, tryOnSecret
Write-Host 'CatVTON configured. Run dotnet run in Development mode.'
