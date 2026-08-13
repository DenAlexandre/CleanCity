import flask
import json
from StreetFinder import StreetFinder

mStreetFinder = None

host = "127.0.0.1"
port = 3000

def getStreet(lat, long):
    street = mStreetFinder.find(lat, long, 0.01)
    if street is None:
        street = "No GPS Data Found"

    return street

app = flask.Flask(__name__)

@app.route("/")
def home():
    return "Clean City API"

@app.route("/find", methods=["POST"])
def find_street():
    data = flask.request.get_json()

    if "lat" not in data or "long" not in data:
        return "bad request!", 400

    response = {}
    response["street"] = getStreet(data["lat"], data["long"])

    return flask.jsonify(response), 201

if __name__ == "__main__":
    mStreetFinder = StreetFinder("resources/BDD/Rues.csv")

    # Garde les segments en intersections de la zone donnée
    with open("resources/BDD/bounds.json") as f:
        bounds = json.load(f)
        mStreetFinder.reduceStreetsToBounds(bounds["min_lat"], bounds["min_long"], bounds["max_lat"], bounds["max_long"])

    app.run(host=host, port=port, debug=True)