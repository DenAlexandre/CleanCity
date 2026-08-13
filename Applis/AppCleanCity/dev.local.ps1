<#
    Reglages de dev partages (voir dev.local.ps1.example pour la liste des options). Charge
    automatiquement par start-dev.ps1 s'il existe.
#>

# A $false pour ne pas demarrer le serveur de tuiles local (voir start-tileserver.ps1), par exemple
# si Docker n'est pas installe ou si tu n'as pas besoin de la cartographie. Le Front retombe alors
# sur le serveur public OpenStreetMap.
$UseLocalTileServer = $false
