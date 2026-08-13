import "dotenv/config";

interface CachedToken {
  accessToken: string;
  expiresAt: number;
}

let cachedToken: CachedToken | null = null;

// Marge de sécurité pour renouveler le token avant son expiration réelle.
const EXPIRY_SAFETY_MARGIN_MS = 60_000;

function getBaseUrl(): string {
  const baseUrl = process.env.CORTEXIA_BASE_URL;
  if (!baseUrl) {
    throw new Error("CORTEXIA_BASE_URL doit être défini dans .env");
  }
  return baseUrl;
}

function getCredentials(): { username: string; password: string } {
  const username = process.env.CORTEXIA_USERNAME;
  const password = process.env.CORTEXIA_PASSWORD;
  if (!username || !password) {
    throw new Error("CORTEXIA_USERNAME et CORTEXIA_PASSWORD doivent être définis dans .env");
  }
  return { username, password };
}

function decodeTokenExpiry(token: string): number {
  const payload = token.split(".")[1];
  const { exp } = JSON.parse(Buffer.from(payload, "base64").toString("utf8")) as { exp: number };
  return exp * 1000;
}

async function fetchAccessToken(credentials?: { username: string; password: string }): Promise<CachedToken> {
  const { username, password } = credentials ?? getCredentials();

  const res = await fetch(`${getBaseUrl()}/login/access-token`, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({ username, password }),
  });

  if (!res.ok) {
    throw new Error(`Échec de l'authentification Cortexia (${res.status})`);
  }

  const data = (await res.json()) as { access_token: string };
  return { accessToken: data.access_token, expiresAt: decodeTokenExpiry(data.access_token) };
}

async function getAccessToken(): Promise<string> {
  if (cachedToken && cachedToken.expiresAt - EXPIRY_SAFETY_MARGIN_MS > Date.now()) {
    return cachedToken.accessToken;
  }
  cachedToken = await fetchAccessToken();
  return cachedToken.accessToken;
}

async function cortexiaFetch<T>(path: string, params?: Record<string, string>): Promise<T> {
  const query = params ? `?${new URLSearchParams(params)}` : "";
  const url = `${getBaseUrl()}${path}${query}`;

  const doFetch = (token: string) => fetch(url, { headers: { Authorization: `Bearer ${token}` } });

  let token = await getAccessToken();
  let res = await doFetch(token);

  if (res.status === 401) {
    cachedToken = null;
    token = await getAccessToken();
    res = await doFetch(token);
  }

  if (!res.ok) {
    throw new Error(`Échec de l'appel Cortexia ${path} (${res.status})`);
  }

  return (await res.json()) as T;
}

export async function loginToCortexia(credentials?: {
  username: string;
  password: string;
}): Promise<{ accessToken: string; expiresAt: string }> {
  // Avec des identifiants explicites, on ne touche pas au cache partagé (compte de service).
  if (credentials) {
    const token = await fetchAccessToken(credentials);
    return { accessToken: token.accessToken, expiresAt: new Date(token.expiresAt).toISOString() };
  }

  const accessToken = await getAccessToken();
  return { accessToken, expiresAt: new Date(cachedToken!.expiresAt).toISOString() };
}

export function getEdgesAndPlacesGeojson(): Promise<unknown> {
  return cortexiaFetch("/elastic/edges_and_places/geojson");
}

export function getAggregatedSnapshots(start: string, end: string): Promise<unknown> {
  return cortexiaFetch("/elastic/aggregated_snapshots", { start, end });
}

export function getEdgesAndPlacesCci(start: string, end: string): Promise<unknown> {
  return cortexiaFetch("/elastic/edges_and_places/cci", { start, end });
}
