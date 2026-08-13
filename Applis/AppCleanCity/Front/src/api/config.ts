// En dev, Vite proxifie /api vers l'API .NET (voir vite.config.ts). En prod, VITE_API_BASE_URL
// permet de pointer vers une URL absolue si le front n'est pas servi derrière le même reverse proxy.
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

// En dev, pointe vers le serveur de tuiles auto-hébergé (voir start-tileserver.ps1) pour ne plus
// dépendre de tile.openstreetmap.org, dont la politique d'usage interdit l'usage intensif et qui
// provoquait des tuiles manquantes. En prod, retombe sur le serveur public OSM tant qu'aucun
// serveur de tuiles n'y est déployé.
export const TILE_SERVER_URL = import.meta.env.VITE_TILE_SERVER_URL ?? 'https://tile.openstreetmap.org/{z}/{x}/{y}.png'
