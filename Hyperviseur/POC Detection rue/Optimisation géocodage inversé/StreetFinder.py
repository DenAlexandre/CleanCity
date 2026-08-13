import geopandas as gpd
import pandas
from shapely import wkt
from shapely.geometry import Point
import warnings

class StreetFinder:
    def __init__(self, FilePath):
        gdf = pandas.read_csv(FilePath, sep='\t', encoding='utf-8')
        gdf['geometry'] = gdf['geometry'].apply(wkt.loads)
        self.segments_gdf = gpd.GeoDataFrame(gdf, crs='EPSG:4326')

    # Fonction pour trouver la rue la plus proche d'un point donné (ex: coordonnées GPS)
    def find(self, lon, lat, buffer=0.0001):
        point = Point(lat, lon)

        # Pour ne pas mettre sur la console les avertissements de GeoPandas
        with warnings.catch_warnings():
            warnings.simplefilter("ignore", category=UserWarning)
            possible_segments = self.segments_gdf[self.segments_gdf.geometry.distance(point) < buffer]

        # Si l'opération ne trouve aucune rue assez proche (soit buffer trop petit, soit point trop isolé, soit la rue n'est pas dans BDD)
        if possible_segments.empty:
            return None
        
        # Pour ne pas mettre sur la console les avertissements de GeoPandas
        with warnings.catch_warnings():
            warnings.simplefilter("ignore", category=UserWarning)
            plus_proche = possible_segments.geometry.distance(point).idxmin()   
            return self.segments_gdf.loc[plus_proche, "name"]