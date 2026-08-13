import json
from StreetFinderAPIHandler import *
from shapely.geometry import Point

# Print iterations progress
def printProgressBar (iteration, total, prefix = '', suffix = '', length = 100, fill = '█', printEnd = "\r"):
    
    filledLength = int(length * iteration // total)
    bar = fill * filledLength + '-' * (length - filledLength)
    print(f'\r{prefix} |{bar}| {suffix}', end = printEnd)
    # Print New Line on Complete
    if iteration == total: 
        print()

def FiltrageDechetsProches(dechetsData):
    l = len(dechetsData)
    buffer = 0.0001

    if l == 0:
        return []

    filteredData = [dechetsData[0]]

    for i in range (1, l):
        loc = Point(dechetsData[i]["lat"], dechetsData[i]["long"])

        isDechetUnique = True

        for j in range (len(filteredData)):
            loc_j = Point(filteredData[j]["lat"], filteredData[j]["long"])

            dist = loc.distance(loc_j)
            if loc.distance(loc_j) < buffer:
                isDechetUnique = False

        if isDechetUnique:
            filteredData.append(dechetsData[i])

    return filteredData

def TraitementDechets(FilePath, parcoursID):
    # Ouvre les données du parcours
    with open(FilePath) as f:
        dechetsData = json.load(f)

    # Filtre
    dechetsData = FiltrageDechetsProches(dechetsData)

    # Ajoute l'ID du parcours et de la rue aux entrées
    l = len(dechetsData)
    for i in range(l):
        dechetsData[i]["parcoursID"] = parcoursID
        dechetsData[i]["rue"] = get_street(dechetsData[i]["lat"], dechetsData[i]["long"])

        # Affichage de la progression
        percent = ("{0:.1f}").format(100 * ((i+1) / float(l)))
        printProgressBar(i+1, l, prefix="Déchets traités:", suffix=f"{percent}% ({i+1}/{l}) traités", length=50)

    return dechetsData