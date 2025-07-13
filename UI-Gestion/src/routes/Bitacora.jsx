import { useAuth } from "../auth/AuthProvider"
import ListadoConFiltros from "../components/ListadoConFiltros.jsx";
import Modal from "../components/Modal.jsx";
import ApiService from "../services/ApiService";
import  { useState } from "react";
import"../styles/VisualTablas.css"





export default function Bitacora(){
    const auth = useAuth();

    const [Payload, setPayload] = useState(null);
    const [mostrarModalInfo, setMostrarModalInfo] = useState(false);

    const columnas = ["descripcion","entityType", "action", "actorId", "timestamp", "cambios"];
    const renderCell = (row, column) => {
        if (column === "cambios") {
            return(
                <div className="modificar-buttons">
                <button className="boton-estado" onClick={() => mostrarPayload(row)}>
                    Ver Cambios
                </button>
                </div>
            );
        }
        if (column === "timestamp") {
            const fecha = new Date(row.timestamp).toLocaleString("es-ES", {
            dateStyle: "medium",
            timeStyle: "short",
        });
            return fecha;
        }
        return row[column];
    };

    const mostrarPayload = (row) => {
        setPayload(row.payload); 
        setMostrarModalInfo(true); 
        console.log("Payload:",row.payload );
    };


    const configFiltros = [
        {
            nombre: "entityType",
            tipo: "text",
            etiqueta: "Tipo de entidad",
            valorPorDefecto: ""
        },
        {
            nombre: "entityId",
            tipo: "text",
            etiqueta: "ID de entidad",
            valorPorDefecto: ""
        },
        {
            nombre: "actorId",
            tipo: "text", 
            etiqueta: "Actor",
            valorPorDefecto: ""
        },
        {
            nombre: "action",
            tipo: "select",
            etiqueta: "Acción",
            opciones: ["Create", "Update", "Delete", "Access"],
            valorPorDefecto: ""
        },
        {
            nombre: "startDate",
            tipo: "date", 
            etiqueta: "Desde",
            valorPorDefecto: ""
        },
        {
            nombre: "endDate",
            tipo: "date",
            etiqueta: "Hasta",
            valorPorDefecto: ""
        },
        {
            nombre: "latestCount",
            tipo: "number", 
            etiqueta: "Últimos eventos",
            valorPorDefecto: ""
        },
    ];
    const fetchDatos = async (filtros, paginacion) => {
        const { isAuthenticated, checkAuth } = auth;
        await checkAuth();
    
        if (!isAuthenticated) {
        Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
        return;
        }

        const token = auth.getAccessToken();
        const queryParams = {
        page: paginacion.page,
        pageSize: paginacion.pageSize,
        ...filtros,
        };

        const result = await ApiService.get("/audit", token, queryParams);

        if (result.success) {
            return {
                datos: result.data,
                paginacion: result.pagination,
            };
        } else {
            Notificar.Error(result.errors[0].message);
        }

        
    };

    return (
        <>
            {mostrarModalInfo && (
                  <Modal
                    isOpen={mostrarModalInfo}
                    closeModal={() => {
                      setMostrarModalInfo(false);
                      setPayload(null);
                    }} textoBoton= "Cerrar"
            >
                    <div className="modal-contenido-blur tarjeta-payload">
                      <h2> Modificaciones</h2>
            
                      <div className="grupo-datos">
                            <p><strong>Id entidad afectada:</strong> {Payload?.id}</p>

                            <div className="detalle-cambios">
                                <h4>Antes de la modificación:</h4>
                                <ul>
                                {Payload?.antes &&
                                    Object.entries(Payload.antes).map(([key, value]) => (
                                    <li key={key}>
                                        <strong>{key}:</strong> {value || "—"}
                                    </li>
                                    ))}
                                </ul>
                            </div>

                            <div className="detalle-cambios">
                                <h4>Después de la modificación:</h4>
                                <ul>
                                {Payload?.despues &&
                                    Object.entries(Payload.despues).map(([key, value]) => (
                                    <li key={key}>
                                        <strong>{key}:</strong> {value || "—"}
                                    </li>
                                    ))}
                                </ul>
                            </div>
                        </div>
                    </div>
                  </Modal>
            )}
        
            <div className="listado-glass">
                <ListadoConFiltros
                    titulo="Bitácora de eventos"
                    columnas={columnas}
                    renderCell={renderCell}
                    configFiltros={configFiltros}
                    fetchDatos={fetchDatos}
                />
            </div> 

    </>);
}
