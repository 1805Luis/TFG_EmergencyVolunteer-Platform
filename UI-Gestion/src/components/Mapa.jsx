import {
  MapContainer,
  TileLayer,
  Polygon,
  Marker,
  Popup,
  useMap,
  useMapEvent
} from "react-leaflet";
import "leaflet/dist/leaflet.css";
import "../styles/Mapa.css";
import { useEffect, useState } from "react";
import L from "leaflet";
import { FaEdit, FaTrashAlt } from "react-icons/fa";

// Centrado automático y apertura controlada del popup
function MapaControl({ emergenciaSeleccionada, setZoom, mostrarLista, setPopupEmergencia }) {
  const map = useMap();

  useMapEvent("zoomend", () => {
    setZoom(map.getZoom());
  });

  useEffect(() => {
    if (!emergenciaSeleccionada) {
    setPopupEmergencia(null); // cerrar popup si no hay emergencia seleccionada
  } else {

      const geoJson = emergenciaSeleccionada.areaAfectadaGeoJson;
      const positions = geoJson.coordinates[0].map(coord => [coord[1], coord[0]]);
      if (positions.length === 0) return;

      const bounds = L.latLngBounds(positions);
      map.invalidateSize();

      setTimeout(() => {
        map.fitBounds(bounds, {
          padding: [20, 20],
          maxZoom: 12
        });

        const centroide = bounds.getCenter();
        setPopupEmergencia({
          latlng: centroide,
          emergencia: emergenciaSeleccionada
        });
      }, 350);
    }
  }, [emergenciaSeleccionada, mostrarLista, map, setPopupEmergencia]);

  return null;
}

// Calcular centroide
function calcularCentroide(coords) {
  let latSum = 0, lonSum = 0;
  coords.forEach(([lon, lat]) => {
    latSum += lat;
    lonSum += lon;
  });
  return [latSum / coords.length, lonSum / coords.length];
}

// Marcadores emoji por gravedad
const crearIconoGravedad = (gravedad) => {
  const emojis = {
    baja: "🟢",
    media: "🟠",
    alta: "🔴",
    critica: "🟣"
  };

  return new L.DivIcon({
    html: `<div class="emergencia-marker">${emojis[gravedad] || "❓"}</div>`,
    className: "custom-marker-wrapper",
    iconSize: [30, 30],
    iconAnchor: [15, 15],
    popupAnchor: [0, -30]
  });
};

export default function Mapa({
  geoJsonList,
  onPolygonClick,
  emergenciasList,
  selectedEmergencia,
  mostrarLista,
  onModificarClick,
  onEliminarClick
}) {
  const [zoom, setZoom] = useState(5.4);
  const [popupEmergencia, setPopupEmergencia] = useState(null);
  const hayPoligonos = geoJsonList.length > 0 && emergenciasList.length > 0;

  const colores = {
    baja: { color: "green", fillColor: "#e6fbe6", fillOpacity: 0.6 },
    media: { color: "orange", fillColor: "#fff3e0", fillOpacity: 0.6 },
    alta: { color: "red", fillColor: "#fdecea", fillOpacity: 0.6 },
    critica: { color: "purple", fillColor: "#f3e5f5", fillOpacity: 0.6 }
  };

  const handleEmergenciaClick = (emergencia) => {
    onPolygonClick(emergencia);

    const centroide = calcularCentroide(emergencia.areaAfectadaGeoJson.coordinates[0]);

    // Si ya está abierto, lo cerramos primero para forzar reapertura
    setPopupEmergencia(null);

    setTimeout(() => {
      setPopupEmergencia({ latlng: centroide, emergencia });
    }, 100); // pequeño delay para que React cierre y luego vuelva a abrir
  };


  return (
    <MapContainer center={[35.5, -6.5]} zoom={zoom} style={{ height: "100%", width: "100%" }}>
      <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />

      {hayPoligonos &&
        emergenciasList.map((emergencia, i) => {
          const geoJson = emergencia.areaAfectadaGeoJson;
          const positions = geoJson.coordinates[0].map(coord => [coord[1], coord[0]]);
          const gravedad = emergencia.gravedad.toLowerCase();
          const esSeleccionada = selectedEmergencia?.id === emergencia.id;

          const estiloBase = colores[gravedad] || { color: "gray", fillColor: "lightgray", fillOpacity: 0.4 };
          const estilo = {
            ...estiloBase,
            weight: esSeleccionada ? 5 : 2,
            dashArray: esSeleccionada ? null : "4"
          };

          const centroide = calcularCentroide(geoJson.coordinates[0]);

          return (
            <div key={i}>
              <Polygon
                positions={positions}
                pathOptions={estilo}
                eventHandlers={{ click: () => handleEmergenciaClick(emergencia) }}
              />
              {zoom <= 9 && (
                <Marker
                  position={centroide}
                  icon={crearIconoGravedad(gravedad)}
                  eventHandlers={{ click: () => handleEmergenciaClick(emergencia) }}
                />
              )}
            </div>
          );
        })}

      <MapaControl
        emergenciaSeleccionada={selectedEmergencia}
        setZoom={setZoom}
        mostrarLista={mostrarLista}
        setPopupEmergencia={setPopupEmergencia}
      />

      {popupEmergencia && (
        <Popup
          position={popupEmergencia.latlng}
          closeButton={true}
          onClose={() => setPopupEmergencia(null)}
        >
          <div className="popup-content">
            <strong>{popupEmergencia.emergencia.tipo_emergencia}</strong><br />
            <span>📍 {popupEmergencia.emergencia.zona_afectada}</span><br />
            <span>📝 {popupEmergencia.emergencia.descripcion}</span><br />
            <div className="popup-actions">
              <button className="popup-button popup-button-modificar" onClick={() => onModificarClick(popupEmergencia.emergencia)}>
                <FaEdit /> Modificar
              </button>
              <button className="popup-button popup-button-eliminar" onClick={() => onEliminarClick(popupEmergencia.emergencia)}>
                <FaTrashAlt /> Eliminar
              </button>
            </div>
          </div>
        </Popup>
      )}

      <div className="map-legend leaflet-bottom leaflet-left">
        <h4>Gravedad</h4>
        <ul>
          <li><span className="emoji">🟢</span> Baja</li>
          <li><span className="emoji">🟠</span> Media</li>
          <li><span className="emoji">🔴</span> Alta</li>
          <li><span className="emoji">🟣</span> Crítica</li>
        </ul>
      </div>
    </MapContainer>
  );
}
