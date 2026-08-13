import path from "path";
import swaggerJsdoc from "swagger-jsdoc";

const options: swaggerJsdoc.Options = {
  definition: {
    openapi: "3.0.3",
    info: {
      title: "CleanCity API",
      version: "1.0.0",
      description: "API d'authentification et d'intégration Cortexia (edges/places, snapshots, CCI).",
    },
    servers: [{ url: "/api" }],
    components: {
      securitySchemes: {
        cookieAuth: {
          type: "apiKey",
          in: "cookie",
          name: "token",
        },
      },
      schemas: {
        Error: {
          type: "object",
          properties: {
            error: { type: "string" },
          },
        },
        User: {
          type: "object",
          properties: {
            id: { type: "integer" },
            username: { type: "string" },
            firstName: { type: "string" },
            lastName: { type: "string" },
            email: { type: "string", format: "email" },
            phone: { type: "string" },
            role: { type: "string", enum: ["admin", "user"] },
          },
        },
      },
    },
  },
  // glob interprets backslashes as escapes, so normalize Windows paths to forward slashes.
  apis: [path.join(__dirname, "routes", "*.{ts,js}").split(path.sep).join("/")],
};

export const swaggerSpec = swaggerJsdoc(options);
