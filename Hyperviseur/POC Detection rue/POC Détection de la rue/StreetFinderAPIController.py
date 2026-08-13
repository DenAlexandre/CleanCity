import flask
from StreetFinder import StreetFinder

mStreetFinder = None

host = "127.0.0.1"
port = 3000

def getStreet(lon, lat):
    street = mStreetFinder.find(lon, lat, 0.01)
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

    if "lon" not in data or "lat" not in data:
        return "bad request!", 400

    response = {}
    response["street"] = getStreet(data["lon"], data["lat"])

    return flask.jsonify(response), 201

if __name__ == "__main__":
    mStreetFinder = StreetFinder("Rues.csv")
    app.run(host=host, port=port, debug=True)