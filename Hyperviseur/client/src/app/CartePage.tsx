import { useState, useEffect, FC } from 'react'
import { Map } from 'leaflet'
import L from "leaflet"
import { MapContainer, TileLayer, Marker, Popup, Polyline } from 'react-leaflet'
import MarkerClusterGroup from 'react-leaflet-cluster'
import { useNavigate } from "react-router-dom";
import './MainPage.css'
import { UseDechetsProvider, UsePointsParcoursProvider } from '../hooks';
import { LeafletTrackingMarker } from "react-leaflet-tracking-marker";

const CartePage: FC = () => {
  const navigate = useNavigate();

  // Fonction pour filtrer les markers à placer
  const isMarkerOnScreen = (lat: number, long: number): boolean => {
    if (map == null) return false;

    if (lat >= map.getBounds().getSouthWest().lat &&
      lat <= map.getBounds().getNorthEast().lat &&
      long >= map.getBounds().getSouthWest().lng &&
      long <= map.getBounds().getNorthEast().lng)
      return true;

    return false;
  };

  // Capture l'instance de la map pour l'utiliser dans des fonctions
  const [map, setMap] = useState<Map | null>(null);

  // Set date on (re)load
  const setCurrentDateStart = (currentPeriode: string): void => {
    let curr = new Date();
    curr.setHours(1, 0, 0, 0);
    const diff = curr.getDate() - curr.getDay() + (curr.getDay() === 0 ? -6 : 1);

    switch (currentPeriode) {
      case "week":
        curr = new Date(curr.setDate(diff));
        break;

      case "month":
        curr.setDate(1);
        break;
    }

    setDateStart(curr.toISOString().split(".")[0]);
  };

  const setCurrentDateEnd = (currentPeriode: string): void => {
    let curr = new Date();
    curr.setHours(24, 59, 0, 0);
    const diff = curr.getDate() + curr.getDay() - (curr.getDay() === 0 ? 6 : 1) + 1;

    switch (currentPeriode) {
      case "week":
        curr = new Date(curr.setDate(diff));
        break;

      case "month":
        curr = new Date(curr.getFullYear(), curr.getMonth() + 1, 0);
        break;
    }

    setDateEnd(curr.toISOString().split(".")[0]);
  };

  // Set date on load
  useEffect(() => { setCurrentDateStart(datePeriode); }, []);
  useEffect(() => { setCurrentDateEnd(datePeriode); }, []);

  const [dateStart, setDateStart] = useState("2024-10-21T00:00");
  const [dateEnd, setDateEnd] = useState("2024-10-21T23:59");
  const [datePeriode, setDatePeriode] = useState("today");

  const [bottleFilter, setBottleFilter] = useState(true);
  const [canFilter, setCanFilter] = useState(true);
  const [overflowBinFilter, setOverflowBinFilter] = useState(true);
  const [garbageBagFilter, setGarbageBagFilter] = useState(true);

  // Get trash
  const { useListDechets } = UseDechetsProvider();
  const { dechets, loadDechet } = useListDechets({
    dateStart: dateStart, dateEnd: dateEnd,
    bottleFilter: bottleFilter, canFilter: canFilter, overflowBinFilter: overflowBinFilter, garbageBagFilter: garbageBagFilter
  });
  useEffect(() => loadDechet, [loadDechet]);

  // Get Points Parcours
  const { useListPointsParcours } = UsePointsParcoursProvider();
  const { pointsParcours, loadPointParcours } = useListPointsParcours({ dateStart: dateStart, dateEnd: dateEnd });
  useEffect(() => loadPointParcours, [loadPointParcours]);

  const arrowIcon = L.icon({
    iconUrl: "assets/arrow.png",
    iconSize:[20, 20],
  });

  return (
    <div className="container" style={{ height: "100vh", width: "100%" }}>
      <div className="header">
        <div className="left-logos">
          <img src="assets/logo_palaiseau.png" alt="Logo Palaiseau"></img>
          <img src="assets/logo_semeru.png" alt="Logo Semeru"></img>
        </div>
        <div className="center-logo">
          <img src="assets/logo_cleancity.png" alt="Logo CleanCity"></img>
        </div>
      </div>

      <div className="horizontal-menu">
        <div className="horizontal-menu-item home-logo">
          <img src="assets/home.png" alt="Home" onClick={() => { navigate("/"); }}></img>
        </div>

        <div className="horizontal-menu-item">
          <div className='tdb'>Cartographie</div>
          <div className="vertical-divider"></div>
          <label>Sur la période :</label>
          <select id="select-period" value={datePeriode} onChange={(e: React.ChangeEvent<HTMLSelectElement>): void => {
            e.preventDefault();
            setDatePeriode(e.target.value);
            setCurrentDateStart(e.target.value);
            setCurrentDateEnd(e.target.value);
          }}>
            <option value="today">Aujourd'hui</option>
            <option value="week">Cette semaine</option>
            <option value="month">Ce mois-ci</option>
          </select>
        </div>

        <div className="horizontal-menu-item">
          <label>Du :</label>
          <input
            type="datetime-local"
            id="start-date"
            value={dateStart}
            onChange={(e: React.ChangeEvent<HTMLInputElement>): void => {
              e.preventDefault();
              setDateStart(e.target.value);
            }}></input>

          <label>Au :</label>
          <input
            type="datetime-local"
            id="end-date"
            value={dateEnd}
            onChange={(e: React.ChangeEvent<HTMLInputElement>): void => {
              e.preventDefault();
              setDateEnd(e.target.value);
            }}></input>
        </div>

        <div className="horizontal-menu-item alert-icon">
          <div className="vertical-divider"></div>
          <img src="assets/notification.png" style={{ height: 30, width: 30 }} alt="notification"></img>
        </div>
      </div>

      <div className="main-content" style={{ height: "100%", width: "100%" }}>
        <div className="sidebar" style={{ height: "100%" }}>
          <div className="sidebar-item">
            <img src="assets/ping.png" alt="Ping" onClick={() => { navigate("/carte"); }}></img>
          </div>
          <div className="sidebar-item">
            <img src="assets/parametres.png" alt="parametres"></img>
          </div>
        </div>

        <MapContainer center={[48.716, 2.259]} zoom={13} scrollWheelZoom={true} ref={setMap}>
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            maxZoom={23}
            maxNativeZoom={23}
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />

          <Polyline positions={ pointsParcours.map((pointParcours) => {return [pointParcours.latitude, pointParcours.longitude]; }) } />

          {dechets.filter((dechet) => { return isMarkerOnScreen(dechet.lat, dechet.long) }).map((dechet) => (
            <Marker key={dechet.uuid} position={[dechet.lat, dechet.long]}>
              <Popup>
                <img src="assets/image.png" alt="parametres"></img>
                <br></br>
                Rue: {dechet.rue}
                <br></br>
                Dechet: {dechet.dechet}
                <br></br>
                Nombre de déchets: {dechet.record}
              </Popup>
            </Marker>))}

          <MarkerClusterGroup chunkedLoading>

          
          </MarkerClusterGroup>

          {pointsParcours.filter((pointParcours) => { return isMarkerOnScreen(pointParcours.latitude, pointParcours.longitude) }).map((pointParcours) => (
            /*<CircleMarker
              key={pointParcours.uuid}
              center={[pointParcours.latitude, pointParcours.longitude]}
              fillColor='red'
              color='red'
              opacity={0.5}
              radius={2}
            ></CircleMarker>*/

            <LeafletTrackingMarker icon={arrowIcon} key={pointParcours.uuid} position={[pointParcours.latitude, pointParcours.longitude]} rotationAngle={pointParcours.heading} duration={1}>

            </LeafletTrackingMarker>
          ))}
        </MapContainer>

        <div className="filters">
          <h3>Filtres</h3>
          <label>
            <input type="checkbox" id="bottle" checked={bottleFilter} onChange={() => (setBottleFilter(!bottleFilter))} /> Bouteilles
          </label><br />

          <label>
            <input type="checkbox" id="can" checked={canFilter} onChange={() => (setCanFilter(!canFilter))} /> Canettes
          </label><br />

          <label>
            <input type="checkbox" id="overflow_bin" checked={overflowBinFilter} onChange={() => (setOverflowBinFilter(!overflowBinFilter))} /> Poubelles débordantes
          </label><br />

          <label>
            <input type="checkbox" id="garbage_bag" checked={garbageBagFilter} onChange={() => (setGarbageBagFilter(!garbageBagFilter))} /> Sacs poubelle
          </label>
        </div>
      </div>
    </div>
  )
}

export default CartePage