import { Router } from "express";
import bcrypt from "bcrypt";
import { z } from "zod";
import { pool } from "../db/pool";
import { signToken } from "../utils/jwt";
import { requireAuth } from "../middleware/auth";

const router = Router();

const registerSchema = z.object({
  username: z
    .string()
    .min(3, "Le pseudo doit contenir au moins 3 caractères.")
    .max(50)
    .regex(/^[a-zA-Z0-9_.-]+$/, "Le pseudo ne peut contenir que lettres, chiffres, '.', '_' ou '-'."),
  firstName: z.string().min(1, "Le prénom est requis."),
  lastName: z.string().min(1, "Le nom est requis."),
  email: z.string().email("Email invalide."),
  phone: z.string().min(6, "Numéro de téléphone invalide."),
  password: z.string().min(8, "Le mot de passe doit contenir au moins 8 caractères."),
});

const loginSchema = z.object({
  username: z.string().min(1),
  password: z.string().min(1),
});

const USER_FIELDS = "id, username, first_name AS \"firstName\", last_name AS \"lastName\", email, phone, role";

const COOKIE_OPTIONS = {
  httpOnly: true,
  sameSite: "lax" as const,
  secure: process.env.NODE_ENV === "production",
  maxAge: 7 * 24 * 60 * 60 * 1000,
};

/**
 * @openapi
 * /auth/register:
 *   post:
 *     tags: [Auth]
 *     summary: Créer un compte utilisateur
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required: [username, firstName, lastName, email, phone, password]
 *             properties:
 *               username: { type: string, example: jdupont }
 *               firstName: { type: string, example: Jean }
 *               lastName: { type: string, example: Dupont }
 *               email: { type: string, format: email }
 *               phone: { type: string, example: "0600000000" }
 *               password: { type: string, format: password, minLength: 8 }
 *     responses:
 *       201:
 *         description: Compte créé, cookie de session posé
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 user: { $ref: '#/components/schemas/User' }
 *       400:
 *         description: Requête invalide
 *         content:
 *           application/json:
 *             schema: { $ref: '#/components/schemas/Error' }
 *       409:
 *         description: Pseudo ou email déjà utilisé
 *         content:
 *           application/json:
 *             schema: { $ref: '#/components/schemas/Error' }
 */
router.post("/register", async (req, res) => {
  const parsed = registerSchema.safeParse(req.body);
  if (!parsed.success) {
    return res.status(400).json({ error: parsed.error.issues[0].message });
  }
  const { username, firstName, lastName, email, phone, password } = parsed.data;

  const existing = await pool.query(
    "SELECT id FROM users WHERE email = $1 OR username = $2",
    [email, username]
  );
  if (existing.rowCount) {
    return res.status(409).json({ error: "Un compte existe déjà avec ce pseudo ou cet email." });
  }

  const passwordHash = await bcrypt.hash(password, 10);
  const result = await pool.query(
    `INSERT INTO users (username, first_name, last_name, email, phone, password_hash, role)
     VALUES ($1, $2, $3, $4, $5, $6, 'user')
     RETURNING ${USER_FIELDS}`,
    [username, firstName, lastName, email, phone, passwordHash]
  );
  const user = result.rows[0];

  const token = signToken({ id: user.id, role: user.role });
  res.cookie("token", token, COOKIE_OPTIONS);
  res.status(201).json({ user });
});

/**
 * @openapi
 * /auth/login:
 *   post:
 *     tags: [Auth]
 *     summary: Se connecter avec le pseudo et le mot de passe
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required: [username, password]
 *             properties:
 *               username: { type: string }
 *               password: { type: string, format: password }
 *     responses:
 *       200:
 *         description: Connecté, cookie de session posé
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 user: { $ref: '#/components/schemas/User' }
 *       401:
 *         description: Identifiants invalides
 *         content:
 *           application/json:
 *             schema: { $ref: '#/components/schemas/Error' }
 */
router.post("/login", async (req, res) => {
  const parsed = loginSchema.safeParse(req.body);
  if (!parsed.success) {
    return res.status(400).json({ error: "Pseudo ou mot de passe invalide." });
  }
  const { username, password } = parsed.data;

  const result = await pool.query(
    `SELECT ${USER_FIELDS}, password_hash FROM users WHERE username = $1`,
    [username]
  );
  const user = result.rows[0];
  if (!user || !(await bcrypt.compare(password, user.password_hash))) {
    return res.status(401).json({ error: "Pseudo ou mot de passe invalide." });
  }

  const token = signToken({ id: user.id, role: user.role });
  res.cookie("token", token, COOKIE_OPTIONS);
  delete user.password_hash;
  res.json({ user });
});

/**
 * @openapi
 * /auth/logout:
 *   post:
 *     tags: [Auth]
 *     summary: Se déconnecter (efface le cookie de session)
 *     responses:
 *       204: { description: Déconnecté }
 */
router.post("/logout", (_req, res) => {
  res.clearCookie("token", COOKIE_OPTIONS);
  res.status(204).send();
});

/**
 * @openapi
 * /auth/me:
 *   get:
 *     tags: [Auth]
 *     summary: Récupérer l'utilisateur actuellement connecté
 *     security: [{ cookieAuth: [] }]
 *     responses:
 *       200:
 *         description: Utilisateur connecté
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 user: { $ref: '#/components/schemas/User' }
 *       401:
 *         description: Non authentifié
 *         content:
 *           application/json:
 *             schema: { $ref: '#/components/schemas/Error' }
 */
router.get("/me", requireAuth, async (req, res) => {
  const result = await pool.query(`SELECT ${USER_FIELDS} FROM users WHERE id = $1`, [
    req.user!.id,
  ]);
  const user = result.rows[0];
  if (!user) {
    return res.status(404).json({ error: "Utilisateur introuvable." });
  }
  res.json({ user });
});

export default router;
