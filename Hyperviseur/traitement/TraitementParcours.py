import json
from shapely.geometry import Point

# Print iterations progress
def printProgressBar (iteration, total, prefix = '', suffix = '', length = 100, fill = '█', printEnd = "\r"):
    
    filledLength = int(length * iteration // total)
    bar = fill * filledLength + '-' * (length - filledLength)
    print(f'\r{prefix} |{bar}| {suffix}', end = printEnd)
    # Print New Line on Complete
    if iteration == total: 
        print()

def FiltragePointsProches(dechetsData):
    l = len(dechetsData)
    buffer = 0.0001

    if l == 0:
        return []

    filteredData = [dechetsData[0]]

    for i in range (1, l):
        loc = Point(dechetsData[i]["latitude"], dechetsData[i]["longitude"])

        isDechetUnique = True

        for j in range (len(filteredData)):
            loc_j = Point(filteredData[j]["latitude"], filteredData[j]["longitude"])

            dist = loc.distance(loc_j)
            if loc.distance(loc_j) < buffer:
                isDechetUnique = False

        if isDechetUnique:
            filteredData.append(dechetsData[i])

    return filteredData

def TraitementParcours(FilePath, parcoursID):
    # Ouvre les données du parcours
    with open(FilePath) as f:
        parcoursData = json.load(f)

    # Filtre
    parcoursData = FiltragePointsProches(parcoursData)

    # Ajoute l'ID du parcours aux entrées
    l = len(parcoursData)
    for i in range(l):
        parcoursData[i]["parcoursID"] = parcoursID

        # Affichage de la progression
        percent = ("{0:." + str(1) + "f}").format(100 * ((i+1) / float(l)))
        printProgressBar(i+1, l, prefix="Points traités: ", suffix=f"{percent}% ({i+1}/{l}) traités", length=50)

    return parcoursData