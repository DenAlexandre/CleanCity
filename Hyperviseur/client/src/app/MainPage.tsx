import { useState, useEffect, FC } from 'react'
import { Map } from 'leaflet'
import { MapContainer, TileLayer, Marker } from 'react-leaflet'
import MarkerClusterGroup from 'react-leaflet-cluster'
import { useNavigate } from "react-router-dom";
import './MainPage.css'
import { UseDechetsProvider } from '../hooks';

const MainPage: FC = () => {
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
  const [isMapExpanded, setIsMapExpanded] = useState(false);

  // Fonction pour changer la taille de la map
  const toggleMapSize = (): void => {
    const mapPanel = document.querySelector('.panel.map');
    const expandButton = document.getElementById('expand-map-btn');

    if (mapPanel == null || expandButton == null || map == null) return;

    if (!isMapExpanded) {
      mapPanel.setAttribute("style", "position: fixed; top: 0px; left: 0px; width: 100%; height: 100%; z-index: 9999;");
      expandButton.textContent = "✖";
    } else {
      mapPanel.setAttribute("style", "position: relative; top: 0px; left: 0px; width: 100%; height: 100%; z-index: auto;");
      expandButton.textContent = "⛶";
    }
    map.invalidateSize();
    setIsMapExpanded(!isMapExpanded);
  };

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

  // Get trash
  const { useListDechets } = UseDechetsProvider();
  const { dechets, loadDechet } = useListDechets({ dateStart, dateEnd });
  useEffect(() => loadDechet, [loadDechet]);

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
          <div className='tdb'>Tableau de bord</div>
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

        <div className="dashboard" style={{ height: '100%', width: '100%' }}>
          <div className="panel">
            <iframe className="weather-widget" src="https://api.wo-cloud.com/content/widget/?geoObjectKey=14185368&language=fr&region=FR&timeFormat=HH:mm&windUnit=kmh&systemOfMeasurement=metric&temperatureUnit=celsius" style={{ border: 0, height: '100%', width: '100%' }}></iframe>
          </div>
          <div className="panel">
            <h3>Centres d'intérêt</h3>
            <div className="interest-grid">
              <div className="interest-item" data-score="4,5/5">
                <img src="assets/gares.png" alt="Gares"></img>
                <div className="label">Gares</div>
              </div>
              <div className="interest-item" data-score="4,2/5">
                <img src="assets/ecoles.png" alt="Écoles"></img>
                <div className="label">Écoles</div>
              </div>
              <div className="interest-item" data-score="3,9/5">
                <img src="assets/parcs.png" alt="Parcs"></img>
                <div className="label">Parcs</div>
              </div>
              <div className="interest-item" data-score="4,8/5">
                <img src="assets/culture.png" alt="Culture"></img>
                <div className="label">Culture</div>
              </div>
              <div className="interest-item" data-score="4,0/5">
                <img src="assets/commerces.png" alt="Commerces"></img>
                <div className="label">Commerces</div>
              </div>
              <div className="interest-item" data-score="3,5/5">
                <img src="assets/z_i.png" alt="Z-I"></img>
                <div className="label">Z - I</div>
              </div>
            </div>
          </div>
          <div className="panel map">
            <MapContainer center={[48.716, 2.259]} zoom={13} scrollWheelZoom={true} ref={setMap}>
              <TileLayer
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                maxZoom={23}
                maxNativeZoom={19}
                tileSize={512}
                zoomOffset={-1}
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
              />

              <MarkerClusterGroup 
                chunkedLoading
                MaxClusterRadius={50}
                disableCLusteringAtZoom={15}
                spiderfyOnMaxZoom={true}
                >
                {dechets.filter((dechet) => { return isMarkerOnScreen(dechet.lat, dechet.long) }).map((dechet) => (
                  <Marker key={dechet.uuid} position={[dechet.lat, dechet.long]}></Marker>
                ))}
              </MarkerClusterGroup>

              <button id="expand-map-btn" onClick={toggleMapSize}>⛶</button>
            </MapContainer>

          </div>

          <div className="panel">
            <h3>Note de propreté</h3>
            <h2>4,2 / 5</h2>
            <div>+23% sur les 7 derniers jours</div>
          </div>

          <div className="panel table">
            <h3><em>Rues les plus sales</em></h3>
            <div className="street-table">
              <div className="row">
                <span className="score red">2,7</span>
                <span className="street">Chemin de l'Épine Montain</span>
                <span className="percentage red">-10%</span>
              </div>
              <div className="row">
                <span className="score red">2,9</span>
                <span className="street">Rue Lazare Hoche</span>
                <span className="percentage green">+5%</span>
              </div>
              <div className="row">
                <span className="score red">3,1</span>
                <span className="street">Rue de la provence</span>
                <span className="percentage orange">-2%</span>
              </div>
              <div className="row">
                <span className="score red">3,1</span>
                <span className="street">Rue de lyon</span>
                <span className="percentage green">+7%</span>
              </div>
              <div className="row">
                <span className="score red">3,2</span>
                <span className="street">Avenue de Flandre</span>
                <span className="percentage red">-23%</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default MainPage