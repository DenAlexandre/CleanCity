from StreetFinderAPIController import host, port
import requests

mHost = host
if "http" not in mHost:
    mHost = f"http://{host}"

url = f"{mHost}:{port}"

def get_street(lon, lat):
    body = {"lon": lon, "lat": lat}
    request = requests.post(url=f"{url}/find", json=body)
    return request.json()