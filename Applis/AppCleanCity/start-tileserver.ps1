<#
.SYNOPSIS
    Demarre un serveur de tuiles OpenStreetMap auto-heberge (Docker) pour le secteur de Palaiseau,
    afin que le Front ne depende plus de tile.openstreetmap.org en developpement.

.DESCRIPTION
    1. Telecharge l'extrait Geofabrik "Ile-de-France" (donnees brutes OSM, licence ODbL -
       telechargement en masse autorise, contrairement aux tuiles deja rendues de tile.openstreetmap.org).
    2. Le decoupe (osmium-tool, execute dans un conteneur jetable) sur la zone Palaiseau + ~15 km.
    3. Importe cet extrait dans un volume Docker Postgres/PostGIS (une seule fois) via l'image
       overv/openstreetmap-tile-server, qui rend ensuite les tuiles a la demande et les met en cache.
    4. Demarre le conteneur de service : tuiles disponibles sur http://localhost:8080/tile/{z}/{x}/{y}.png
#>

$RootDir       = $PSScriptRoot
$DataDir       = Join-Path $RootDir ".tileserver-data"
$FullExtract   = Join-Path $DataDir "ile-de-france-latest.osm.pbf"
$ClippedExtract = Join-Path $DataDir "palaiseau.osm.pbf"
$ExtractUrl    = "https://download.geofabrik.de/europe/france/ile-de-france-latest.osm.pbf"

# Palaiseau (48.7159, 2.2465) +/- ~15 km
$MinLon = 2.0665
$MinLat = 48.5959
$MaxLon = 2.4265
$MaxLat = 48.8359

$DataVolume     = "cleancity-osm-data"
$ContainerName  = "cleancity-tileserver"
$BaseImage      = "overv/openstreetmap-tile-server"
$Image          = $BaseImage
$Port           = 8080

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker n'est pas installe ou n'est pas dans le PATH."
    exit 1
}

docker info > $null 2>&1
if (-not $?) {
    Write-Error "Docker Desktop ne semble pas demarre. Lance-le puis reessaie."
    exit 1
}

New-Item -ItemType Directory -Force -Path $DataDir | Out-Null

# Reseau d'entreprise avec inspection TLS (Zscaler) : le conteneur du serveur de tuiles telecharge
# lui-meme des donnees externes (polygones terre/mer) en HTTPS pendant l'import, et echoue avec une
# erreur de certificat s'il ne connait pas le CA d'interception. On construit une image derivee qui
# ajoute ce CA (deja approuve par Windows) au magasin de confiance du conteneur.
$zscalerCert = Get-ChildItem -Path Cert:\LocalMachine\Root -ErrorAction SilentlyContinue | Where-Object { $_.Subject -like "*Zscaler*" } | Select-Object -First 1
if ($zscalerCert) {
    Write-Host "Certificat d'inspection TLS d'entreprise (Zscaler) detecte, construction d'une image avec ce CA de confiance..."
    $BuildDir = Join-Path $DataDir "docker-build"
    New-Item -ItemType Directory -Force -Path $BuildDir | Out-Null
    Export-Certificate -Cert $zscalerCert -FilePath (Join-Path $BuildDir "zscaler-root-ca.cer") | Out-Null
    certutil -encode (Join-Path $BuildDir "zscaler-root-ca.cer") (Join-Path $BuildDir "zscaler-root-ca.crt") | Out-Null
    @"
FROM $BaseImage
COPY zscaler-root-ca.crt /usr/local/share/ca-certificates/zscaler-root-ca.crt
RUN update-ca-certificates
"@ | Set-Content -Path (Join-Path $BuildDir "Dockerfile") -Encoding ascii

    $Image = "cleancity-tileserver-base:local"
    docker build -t $Image $BuildDir
    if (-not $?) {
        Write-Error "Echec de la construction de l'image avec le CA d'entreprise."
        exit 1
    }
}

if (-not (Test-Path $ClippedExtract)) {
    if (-not (Test-Path $FullExtract)) {
        Write-Host "Telechargement de l'extrait Ile-de-France depuis Geofabrik (peut prendre plusieurs minutes, ~400 Mo)..."
        Invoke-WebRequest -Uri $ExtractUrl -OutFile $FullExtract
    }

    Write-Host "Decoupage de l'extrait sur le secteur Palaiseau (osmium-tool, conteneur jetable)..."
    $volArg = "${DataDir}:/data"
    docker run --rm -v $volArg debian:bookworm-slim bash -c @"
set -e
apt-get update -qq && apt-get install -y -qq --no-install-recommends osmium-tool ca-certificates > /dev/null
osmium extract -b $MinLon,$MinLat,$MaxLon,$MaxLat /data/ile-de-france-latest.osm.pbf -o /data/palaiseau.osm.pbf --overwrite
"@
    if (-not $? -or -not (Test-Path $ClippedExtract)) {
        Write-Error "Echec du decoupage de l'extrait OSM."
        exit 1
    }
}

$volumeExists = docker volume ls -q --filter "name=^${DataVolume}$"
if (-not $volumeExists) {
    Write-Host "Premier lancement : import des donnees dans PostGIS (image $Image). Cette etape telecharge aussi les polygones terre/mer (~1 Go) et peut prendre 10-20 min..."
    docker run --rm `
        -v "${ClippedExtract}:/data/region.osm.pbf" `
        -v "${DataVolume}:/data/database/" `
        $Image import
    if (-not $?) {
        Write-Error "Echec de l'import des donnees OSM. Verifie les logs ci-dessus."
        exit 1
    }
}
else {
    Write-Host "Le volume '$DataVolume' existe deja, import saute (donnees deja presentes)."
}

$existing = docker ps -a --filter "name=^/$ContainerName$" --format "{{.Names}}"

if ($existing -eq $ContainerName) {
    $running = docker ps --filter "name=^/$ContainerName$" --format "{{.Names}}"
    if ($running -eq $ContainerName) {
        Write-Host "Le conteneur '$ContainerName' tourne deja."
    }
    else {
        Write-Host "Le conteneur '$ContainerName' existe mais est arrete, demarrage..."
        docker start $ContainerName | Out-Null
    }
}
else {
    Write-Host "Creation du conteneur '$ContainerName' (rendu de tuiles sur le port $Port)..."
    docker run -d `
        --name $ContainerName `
        -p "${Port}:80" `
        -e UPDATES=disabled `
        -v "${DataVolume}:/data/database/" `
        $Image run | Out-Null
}

Write-Host "Attente de la disponibilite du serveur de tuiles..."
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        Invoke-WebRequest -Uri "http://localhost:$Port/" -UseBasicParsing -TimeoutSec 2 | Out-Null
        $ready = $true
        break
    }
    catch {
        Start-Sleep -Seconds 2
    }
}

if ($ready) {
    Write-Host "Serveur de tuiles pret : http://localhost:$Port/tile/{z}/{x}/{y}.png"
}
else {
    Write-Error "Le serveur de tuiles n'a pas repondu dans le delai imparti. Verifie 'docker logs $ContainerName'."
    exit 1
}
