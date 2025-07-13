import { MapContainer, TileLayer, Polygon, Marker, useMapEvents, useMap } from "react-leaflet";
import { useState, useEffect, useRef } from "react";
import L from "leaflet";

export default function MapaCoordenadas({ coordenadas = [], onChangeCoordenadas }) {
  // Estado guarda en formato [lng, lat]
  const [points, setPoints] = useState(coordenadas);
  const justDragged = useRef(false);
  const [showMessage, setShowMessage] = useState(true);

  function FitBoundsHandler({ points }) {
    const map = useMap();

    useEffect(() => {
      if (points.length > 0) {
        const latLngs = points.map(([lng, lat]) => [lat, lng]); // Convertimos a [lat, lng]
        map.fitBounds(latLngs, { padding: [40, 40] }); // Centra y ajusta el zoom
      }
    }, [points, map]);

    return null;
}

  useEffect(() => {
    setPoints(coordenadas);
  }, [coordenadas]);

  function ClickHandler() {
    useMapEvents({
      click(e) {
        if (justDragged.current) {
          justDragged.current = false;
          return;
        }
        const { lat, lng } = e.latlng;
        setPoints((pts) => {
          // Guardamos en [lng, lat]
          const newPoints = [...pts, [lng, lat]];
          onChangeCoordenadas && onChangeCoordenadas(newPoints);
          return newPoints;
        });
      },
    });
    return null;
  }

  const movePoint = (index, newLatLng) => {
    justDragged.current = true;
    setPoints((pts) => {
      // Convertimos latlng a [lng, lat]
      const newPoints = pts.map((p, i) => (i === index ? [newLatLng.lng, newLatLng.lat] : p));
      onChangeCoordenadas && onChangeCoordenadas(newPoints);
      return newPoints;
    });
  };

  const draggableIcon = new L.Icon({
    iconUrl:
      "https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png",
    iconSize: [25, 41],
    iconAnchor: [12, 41],
  });

  return (
    <div style={{ position: "relative", height: "100%", width: "100%" }}>
      <MapContainer center={[41.4, 2.1]} zoom={13} style={{ height: "100%", width: "100%" }}>
        <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
        <ClickHandler />
        <FitBoundsHandler points={points} />
        {/* Convertimos points [lng, lat] a [lat, lng] para Leaflet */}
        {points.length > 0 && (
          <Polygon
            positions={points.map(([lng, lat]) => [lat, lng])}
            pathOptions={{ color: "blue" }}
          />
        )}

        {points.map(([lng, lat], i) => (
          <Marker
            key={i}
            position={[lat, lng]} // Leaflet espera [lat, lng]
            icon={draggableIcon}
            draggable={true}
            eventHandlers={{
              dragstart: () => {
                justDragged.current = true;
              },
              dragend: (e) => {
                movePoint(i, e.target.getLatLng());
              },
              click: () => {
                if (!justDragged.current) {
                  setPoints((pts) => {
                    const newPoints = pts.filter((_, index) => index !== i);
                    onChangeCoordenadas && onChangeCoordenadas(newPoints);
                    return newPoints;
                  });
                }
                justDragged.current = false;
              },
            }}
          />
        ))}
      </MapContainer>

      {showMessage && (
        <div
          style={{
            position: "absolute",
            top: 10,
            left: "50%",
            transform: "translateX(-50%)",
            backgroundColor: "rgba(0, 0, 0, 0.6)",
            color: "white",
            padding: "8px 16px",
            borderRadius: "8px",
            fontSize: "0.9rem",
            fontWeight: "500",
            zIndex: 1000,
            maxWidth: "100%",
            textAlign: "center",
            boxShadow: "0 2px 6px rgba(0,0,0,0.3)",
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            gap: "12px",
          }}
        >
          <span style={{ flex: 1 }}>
            Crea tu zona damnificada: haz click en el mapa para añadir, click en un punto para eliminar, y arrastra para mover.
          </span>
          <button
            onClick={() => setShowMessage(false)}
            style={{
              background: "transparent",
              border: "none",
              color: "white",
              fontSize: "1.2rem",
              cursor: "pointer",
              fontWeight: "700",
              lineHeight: "1",
            }}
            aria-label="Cerrar mensaje"
            title="Cerrar"
          >
            ×
          </button>
        </div>
      )}
    </div>
  );
}
