from dataclasses import dataclass

@dataclass
class DataModel:
    def __init__(self, dt, element, lat, lon):
        self.dt = dt
        self.element = element
        self.lat = lat
        self.lon = lon