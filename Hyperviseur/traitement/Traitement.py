from TraitementParcours import *
from TraitementDechets import *
import DataBaseHandler
import time

def CreationEntreeParcours(parcoursID, date):
    parcours = { 
        "id": parcoursID,
        "date": date
    }
        
    return parcours

def Traitement():
    start_time = time.time()

    DataBaseHandler.initDataBase()

    parcoursID = DataBaseHandler.getParcoursID()

    donneesPointsParcours = TraitementParcours("resources/Data Set/geolocalisation_rungis.json", parcoursID)
    donneesDechets = TraitementDechets("resources/Data Set/detections_rungis.json", parcoursID)

    date = ""
    if len(donneesDechets) > 0:
        date = donneesDechets[0]["date"]

    parcours = CreationEntreeParcours(parcoursID, date)

    DataBaseHandler.addDocuments("points_parcours", donneesPointsParcours)
    DataBaseHandler.addDocuments("dechets", donneesDechets)
    DataBaseHandler.addDocument("parcours", parcours)

    elapsed_time = time.time() - start_time
    print(f"\nDonnées traitées en {elapsed_time} secondes.")

    noStreetFound = 0
    for dechet in donneesDechets:
        if dechet["rue"] == "No GPS Data Found":
            noStreetFound += 1

    print(f"{noStreetFound} rues non trouvées.")

Traitement()