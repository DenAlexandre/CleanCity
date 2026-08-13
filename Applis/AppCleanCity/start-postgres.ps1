<#
.SYNOPSIS
    Demarre un conteneur Docker PostgreSQL + PostGIS pour l'API CortexiaAuth (connection string dans appsettings.json).
#>

$ContainerName = "cleancity-pg"
$Image         = "postgis/postgis:16-3.4"
$Port          = 5432
$Database      = "cortexia_auth"
$User          = "postgres"
$Password      = "postgres"
$VolumeName    = "cleancity-pg-data"

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker n'est pas installe ou n'est pas dans le PATH."
    exit 1
}

docker info > $null 2>&1
if (-not $?) {
    Write-Error "Docker Desktop ne semble pas demarre. Lance-le puis reessaie."
    exit 1
}

$existing = docker ps -a --filter "name=^/$ContainerName$" --format "{{.Names}}"

if ($existing -eq $ContainerName) {
    $currentImage = docker inspect --format "{{.Config.Image}}" $ContainerName
    if ($currentImage -ne $Image) {
        Write-Host "Le conteneur '$ContainerName' utilise une autre image ($currentImage), recreation avec '$Image' (le volume de donnees '$VolumeName' est conserve)..."
        docker rm -f $ContainerName | Out-Null
        $existing = $null
    }
}

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
    Write-Host "Creation du conteneur '$ContainerName' ($Image)..."
    docker run -d `
        --name $ContainerName `
        -e POSTGRES_USER=$User `
        -e POSTGRES_PASSWORD=$Password `
        -e POSTGRES_DB=$Database `
        -p "${Port}:5432" `
        -v "${VolumeName}:/var/lib/postgresql/data" `
        $Image | Out-Null
}

Write-Host "Attente de la disponibilite de PostgreSQL..."
$ready = $false
for ($i = 0; $i -lt 20; $i++) {
    docker exec $ContainerName pg_isready -U $User > $null 2>&1
    if ($?) {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 2
}

if ($ready) {
    Write-Host "PostgreSQL est pret sur localhost:$Port (base '$Database')."
    Write-Host "Connection string : Host=localhost;Port=$Port;Database=$Database;Username=$User;Password=$Password"
}
else {
    Write-Error "PostgreSQL n'a pas repondu dans le delai imparti. Verifie 'docker logs $ContainerName'."
    exit 1
}
