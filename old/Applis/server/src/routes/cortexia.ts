import { Request, Response, Router } from "express";
import { z } from "zod";
import { requireAuth } from "../middleware/auth";
import {
  getAggregatedSnapshots,
  getEdgesAndPlacesCci,
  getEdgesAndPlacesGeojson,
  loginToCortexia,
} from "../services/cortexia";

const router = Router();

router.use(requireAuth);

const loginSchema = z
  .object({
    username: z.string().min(1).optional(),
    password: z.string().min(1).optional(),
  })
  .refine((data) => Boolean(data.username) === Boolean(data.password), {
    message: "Fournissez username et password ensemble, ou aucun des deux.",
  });

function parseDateRange(req: Request, res: Response) {
  const { start, end } = req.query;
  if (typeof start !== "string" || typeof end !== "string") {
    res.status(400).json({ error: "Les paramètres start et end sont requis." });
    return null;
  }
  return { start, end };
}

/**
 * @openapi
 * /cortexia/login:
 *   post:
 *     tags: [Cortexia]
 *     summary: Authentifier un compte Cortexia et récupérer un token d'accès
 *     description: >
 *       Sans body (ou body vide), utilise les identifiants configurés côté serveur
 *       (CORTEXIA_USERNAME / CORTEXIA_PASSWORD) et renvoie le token mis en cache, réutilisé
 *       automatiquement par les autres endpoints Cortexia ci-dessous. Si username/password sont
 *       fournis dans le body, authentifie ce compte à la place (sans toucher au cache partagé).
 *     security: [{ cookieAuth: [] }]
 *     requestBody:
 *       required: false
 *       content:
 *         application/x-www-form-urlencoded:
 *           schema:
 *             type: object
 *             properties:
 *               username: { type: string }
 *               password: { type: string, format: password }
 *     responses:
 *       200:
 *         description: Token d'accès Cortexia
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 accessToken: { type: string }
 *                 expiresAt: { type: string, format: date-time }
 *       400:
 *         description: username et password doivent être fournis ensemble
 *         content:
 *           application/json:
 *             schema: { $ref: '#/components/schemas/Error' }
 *       401: { description: Non authentifié }
 *       500: { description: Échec de l'authentification auprès de Cortexia }
 */
router.post("/login", async (req, res) => {
  const parsed = loginSchema.safeParse(req.body ?? {});
  if (!parsed.success) {
    return res.status(400).json({ error: parsed.error.issues[0].message });
  }
  const { username, password } = parsed.data;
  const data = await loginToCortexia(username && password ? { username, password } : undefined);
  res.json(data);
});

/**
 * @openapi
 * /cortexia/edges-and-places/geojson:
 *   get:
 *     tags: [Cortexia]
 *     summary: Récupérer le geojson des edges/places Cortexia
 *     security: [{ cookieAuth: [] }]
 *     responses:
 *       200: { description: GeoJSON brut renvoyé par Cortexia }
 *       401: { description: Non authentifié }
 *       500: { description: Échec de l'appel à l'API Cortexia }
 */
router.get("/edges-and-places/geojson", async (_req, res) => {
  const data = await getEdgesAndPlacesGeojson();
  res.json(data);
});

/**
 * @openapi
 * /cortexia/aggregated-snapshots:
 *   get:
 *     tags: [Cortexia]
 *     summary: Récupérer les snapshots agrégés Cortexia sur une période
 *     security: [{ cookieAuth: [] }]
 *     parameters:
 *       - in: query
 *         name: start
 *         required: true
 *         schema: { type: string, example: "2024-06-17 13:45:00" }
 *       - in: query
 *         name: end
 *         required: true
 *         schema: { type: string, example: "2026-06-17 14:00:00" }
 *     responses:
 *       200: { description: Données agrégées renvoyées par Cortexia }
 *       400: { description: Paramètres start/end manquants }
 *       401: { description: Non authentifié }
 *       500: { description: Échec de l'appel à l'API Cortexia }
 */
router.get("/aggregated-snapshots", async (req, res) => {
  const range = parseDateRange(req, res);
  if (!range) return;
  const data = await getAggregatedSnapshots(range.start, range.end);
  res.json(data);
});

/**
 * @openapi
 * /cortexia/edges-and-places/cci:
 *   get:
 *     tags: [Cortexia]
 *     summary: Récupérer l'indice CCI des edges/places sur une période
 *     security: [{ cookieAuth: [] }]
 *     parameters:
 *       - in: query
 *         name: start
 *         required: true
 *         schema: { type: string, example: "2024-06-17 15:50:00" }
 *       - in: query
 *         name: end
 *         required: true
 *         schema: { type: string, example: "2026-06-17 16:30:00" }
 *     responses:
 *       200: { description: Données CCI renvoyées par Cortexia }
 *       400: { description: Paramètres start/end manquants }
 *       401: { description: Non authentifié }
 *       500: { description: Échec de l'appel à l'API Cortexia }
 */
router.get("/edges-and-places/cci", async (req, res) => {
  const range = parseDateRange(req, res);
  if (!range) return;
  const data = await getEdgesAndPlacesCci(range.start, range.end);
  res.json(data);
});

export default router;
