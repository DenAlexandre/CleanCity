import geopandas as gpd
import os.path

# Fonction pour découper une rue en tronçons d'une certaine longueur (ex: 50m)
def segmenter_rue(geometry, longueur_segment=50):
    segments = []
    distance = 0
    while distance < geometry.length:
        segment = geometry.interpolate(distance), geometry.interpolate(min(distance + longueur_segment, geometry.length))
        segments.append(segment)
        distance += longueur_segment
    return segments

class ShpToCsv:
    def __init__(self, FilePath):
        # Charger les données de rues (ex: format GeoJSON ou shapefile)
        streets = gpd.read_file(FilePath)

        # (Optionnel) Transformer le système de coordonnées si nécessaire
        streets = streets.to_crs("EPSG:4326")  # Par exemple pour WGS84, utilisé pour les coordonnées GPS

        # Segmenter toutes les rues
        streets["segments"] = streets.geometry.apply(segmenter_rue)

        # Construire un index spatial pour rechercher les tronçons par proximité
        street_segments = []
        for _, row in streets.iterrows():
            for segment in row["segments"]:
                segment_geom = row.geometry
                street_segments.append({
                    'name': row['name'],
                    'geometry': segment_geom
                })

        # Créer un GeoDataFrame avec les segments pour une meilleure manipulation
        self.segments_gdf = gpd.GeoDataFrame(street_segments, crs=streets.crs)

    def remove_nameless_streets(self):
        self.segments_gdf = self.segments_gdf[self.segments_gdf.name.notnull()]

    def to_csv(self, FilePath, overwrite = True):
        if not os.path.exists(os.path.dirname(FilePath)):
            os.makedirs(os.path.dirname(FilePath))

        if overwrite == True:
            self.segments_gdf.to_csv(FilePath, sep='\t', encoding='utf-8', index = False)
        else:
            if(os.path.isfile(FilePath)):
                self.segments_gdf.to_csv(FilePath, sep = '\t', encoding='utf-8', mode='a', index= False)
            else:
                self.segments_gdf.to_csv(FilePath, sep='\t', encoding='utf-8', index = False)