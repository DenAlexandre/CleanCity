from ShpToCsv import ShpToCsv

INPUT_FILE = "D:\\GitLab\\cleancity\\Hyperviseur\\CreateCSV\\ile-de-france-latest-free.shp\\gis_osm_roads_free_1.shp"
OUTPUT_FILE = "out/palaiseau.csv"

mConverter = ShpToCsv(INPUT_FILE)
mConverter.remove_nameless_streets()
mConverter.to_csv(OUTPUT_FILE)