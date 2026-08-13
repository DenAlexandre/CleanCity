<#
.SYNOPSIS
    Demarre l'environnement de dev complet CortexiaAuth : Postgres (Docker), l'API .NET et le
    front React (Vite), chacun dans sa propre fenetre pour garder les logs visibles.
#>

$RootDir = $PSScriptRoot
$ApiDir = Join-Path $RootDir "Server"
$FrontDir = Join-Path $RootDir "Front"
$ApiUrl = "https://localhost:7085"
$FrontUrl = "https://localhost:5173"
$CertDir = Join-Path $FrontDir ".certs"
$CertPath = Join-Path $CertDir "localhost.pem"
$FrontLocalEnvPath = Join-Path $FrontDir ".env.development.local"

# Reglages personnels non versionnes : copie dev.local.ps1.example en dev.local.ps1 pour les modifier.
$UseLocalTileServer = $true
$Database = "Local"
$LocalConfigPath = Join-Path $RootDir "dev.local.ps1"
if (Test-Path $LocalConfigPath) {
    . $LocalConfigPath
}

# Secrets (chaine de connexion Neon) : jamais commit, voir dev.local.secrets.ps1.example.
$ProdConnectionString = $null
$SecretsConfigPath = Join-Path $RootDir "dev.local.secrets.ps1"
if (Test-Path $SecretsConfigPath) {
    . $SecretsConfigPath
}

if ($Database -eq "Prod") {
    if ([string]::IsNullOrWhiteSpace($ProdConnectionString)) {
        Write-Error "`$Database = 'Prod' (dev.local.ps1) mais `$ProdConnectionString n'est pas defini. Copie dev.local.secrets.ps1.example en dev.local.secrets.ps1 et renseigne la chaine de connexion Neon."
        exit 1
    }
    Write-Warning "MODE PROD : l'API locale va se connecter a la base de PRODUCTION (Neon), pas a Postgres local. Toute migration EF Core en attente sera appliquee a la prod au demarrage, et les taches d'import Cortexia en arriere-plan vont ecrire des donnees reelles."
    $env:ConnectionStrings__Default = $ProdConnectionString
}
else {
    Remove-Item Env:\ConnectionStrings__Default -ErrorAction SilentlyContinue
}

& (Join-Path $RootDir "start-postgres.ps1")
if (-not $?) {
    Write-Error "Postgres n'a pas pu demarrer, arret."
    exit 1
}

if ($UseLocalTileServer) {
    & (Join-Path $RootDir "start-tileserver.ps1")
    if ($?) {
        Set-Content -Path $FrontLocalEnvPath -Value "VITE_TILE_SERVER_URL=http://localhost:8080/tile/{z}/{x}/{y}.png" -Encoding ascii
    }
    else {
        Write-Warning "Le serveur de tuiles n'a pas pu demarrer, le Front retombera sur OpenStreetMap public. Mets `$UseLocalTileServer = `$false dans dev.local.ps1 pour ne plus tenter de le demarrer."
        Remove-Item -Path $FrontLocalEnvPath -ErrorAction SilentlyContinue
    }
}
else {
    Write-Host "Serveur de tuiles local desactive (dev.local.ps1: `$UseLocalTileServer = `$false), le Front utilisera OpenStreetMap public."
    Remove-Item -Path $FrontLocalEnvPath -ErrorAction SilentlyContinue
}

if (-not (Test-Path $CertPath)) {
    Write-Host "Export du certificat HTTPS local (dotnet dev-certs) pour le front Vite..."
    New-Item -ItemType Directory -Force -Path $CertDir | Out-Null
    dotnet dev-certs https --export-path $CertPath --format Pem --no-password | Out-Null
    if (-not (Test-Path $CertPath)) {
        Write-Error "Echec de l'export du certificat HTTPS (dotnet dev-certs). Le front ne pourra pas demarrer en HTTPS."
        exit 1
    }
}

function Wait-ForUrl($url, $label) {
    Write-Host "Attente de $label ($url)..."
    for ($i = 0; $i -lt 30; $i++) {
        try {
            Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2 | Out-Null
            Write-Host "$label est pret."
            return $true
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }
    Write-Warning "$label n'a pas repondu apres 60s (verifie sa fenetre de log)."
    return $false
}

Write-Host "Demarrage de l'API .NET dans une nouvelle fenetre (dotnet watch, redemarre automatiquement a chaque modification)..."
Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "Set-Location '$ApiDir'; dotnet watch run --launch-profile https"
)

Write-Host "Demarrage du front (Vite) dans une nouvelle fenetre..."
Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "Set-Location '$FrontDir'; npm run dev"
)

Wait-ForUrl "$ApiUrl/swagger/v1/swagger.json" "L'API .NET" | Out-Null
Wait-ForUrl $FrontUrl "Le front Vite" | Out-Null

Write-Host "Ouverture du navigateur sur $FrontUrl/login ..."
Start-Process "$FrontUrl/login"
