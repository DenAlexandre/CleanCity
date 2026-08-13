from pymongo import MongoClient

HOST = "mongodb://localhost:27017/"
DATABASE_NAME = "CleanCity"

TABLES = [
    "parcours",
    "dechets",
    "points_parcours"
]

# Données utilisées dans les fonctions
initDB = False
client = None
database = None
tables = {}

def initDataBase():
    global HOST, TABLES
    global initDB, client, database, tables

    if initDB == False:
        client = MongoClient(HOST)
        database = client[DATABASE_NAME]

        for table_name in TABLES:
            tables[table_name] = database[table_name]

        initDB = True

def getParcoursID():
    global initDB, tables

    if initDB == False:
        initDataBase()

    return tables["parcours"].count_documents({}) + 1

def addDocument(tableName, jsonData):
    global initDB, tables

    if initDB == False:
        initDataBase()

    tables[tableName].insert_one(jsonData)

def addDocuments(tableName, jsonData):
    global initDB, tables

    if initDB == False:
        initDataBase()

    tables[tableName].insert_many(jsonData)