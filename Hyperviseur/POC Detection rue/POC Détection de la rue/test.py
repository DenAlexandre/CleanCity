from StreetFinderAPIHandler import get_street
from StreetFinder import StreetFinder
from ShpToCsv import ShpToCsv

# Pour les tests
import random
import time

testCoords = [[random.uniform(50.5654, 50.6925), random.uniform(2.9402, 3.1833)] for i in range (100)]

# Test en api
start_time = time.time()
for testCoord in testCoords:
    lon, lat = testCoord
    street = get_street(lon, lat)["street"]
    print(f"La rue la plus proche de ({lon}, {lat}) est : {street}")

elapsed_time = time.time() - start_time

print(f"\n100 coordonnées calculées en {elapsed_time} secondes.")

# Création de la bdd en csv
#mConverter = ShpToCsv("Rues.shp")
#mConverter.remove_nameless_streets()
#mConverter.to_csv("Rues.csv")

"""testCoords = [[50.6286602110038, 3.052691814038058],    # Rue Léon Gambetta
              [50.632820078608646, 3.0471246653861312], # Boulevard Vauban
              [50.63419784745115, 3.0454010070995126],  # Test croisement (Rue de Toul, Rue de Calais)
              [50.6362419538669, 3.069526702891159],    # Place de la gare Lille Flandre
              [50.59383154798333, 3.1318157562623257]   # SEMERU Fayat
              ]"""

# Test de la bdd en local
"""mStreetFinder = StreetFinder("Rues.csv")

for testCoord in testCoords:
    lon, lat = testCoord
    print(f"La rue la plus proche est : {mStreetFinder.find(lon, lat, 0.01)}")"""