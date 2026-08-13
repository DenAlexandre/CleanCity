const express = require('express');
const mongoose = require('mongoose');
const cors = require('cors');

const app = express();
const port = 3000;

// Configuration pour faire des requêtes à partir du site
const corsOptions = {
    origin: "http://localhost:5173"
}
app.use(cors(corsOptions))

// Connexion à MongoDB (utiliser 127.0.0.1 au lieu de localhost)
mongoose.connect('mongodb://127.0.0.1:27017/CleanCity')
  .then(() => {
    console.log('Connexion à MongoDB réussie');
  })
  .catch((err) => {
    console.error('Erreur de connexion à MongoDB:', err);
  });

// Définir un chéma pour les tables
const DechetSchema = new mongoose.Schema({
    date: String,
    dechetId: String,
    imageName: String,
    confidence: Number,
    dechet: String,
    record: Number,
    uuid: String,
    lat: Number,
    long: Number,
    timestamp: Number,
    parcoursID: Number,
    rue: String
})

const ParcoursSchema = new mongoose.Schema({
    id: Number,
    date: String
})

const PointParcoursSchema = new mongoose.Schema({
    heading: String,
    latitude: String,
    uuid: String,
    speed: Number,
    timestamp: String,
    longitude: Number,
    parcoursID: String,
})

// Définir un modèle pour les tables
const DechetModel = mongoose.model("dechets", DechetSchema)
const ParcoursModel = mongoose.model("parcours", ParcoursSchema)
const PointsParcoursModel = mongoose.model("points_parcours", PointParcoursSchema)

// Routes
app.get('/dechets', async (req, res) => {
    DechetModel.find({}).then(function(dechets){
        res.json(dechets)
    }).catch(function(error){
        console.error("Erreur d'obtention des déchets:", error);
    })
})

app.get('/parcours', async (req, res) => {
    ParcoursModel.find({}).then(function(parcours){
        res.json(parcours)
    }).catch(function(error){
        console.error("Erreur d'obtention des parcours:", error);
    })
})

app.get('/points_parcours', async (req, res) => {
    PointsParcoursModel.find({}).then(function(points_parcours){
        res.json(points_parcours)
    }).catch(function(error){
        console.error("Erreur d'obtention des points des parcours:", error);
    })
})

// Démarrer le serveur
app.listen(port, () => {
    console.log(`Serveur démarré sur http://localhost:${port}`);
});
