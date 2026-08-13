import geopandas as gpd
import pandas
from shapely import wkt
from shapely.geometry import Point, Polygon
import warnings

class StreetFinder:
    def __init__(self, FilePath):
        gdf = pandas.read_csv(FilePath, sep='\t', encoding='utf-8')
        gdf['geometry'] = gdf['geometry'].apply(wkt.loads)
        self.segments_gdf = gpd.GeoDataFrame(gdf, crs='EPSG:4326')

    # Fonction pour trouver la rue la plus proche d'un point donné (ex: coordonnées GPS)
    def find(self, lat, long, buffer=0.0001):
        point = Point(long, lat)

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
        
    def reduceStreetsToBounds(self, min_lat, min_long, max_lat, max_long):
        bounds = Polygon([(min_long, min_lat), (min_long, max_lat), (max_long, max_lat), (max_long, min_lat)])

        self.segments_gdf = self.segments_gdf[bounds.intersects(self.segments_gdf["geometry"])]
        """l = self.segments_gdf.shape[0]
        for i in range (l):
            if bounds.intersects(self.segments_gdf["geometry"].values[i]):
                print(self.segments_gdf["name"].values[i])"""