import { useAuth } from "../auth/AuthProvider"
import ListadoConFiltros from "../components/ListadoConFiltros";
import Modal from "../components/Modal";
import ApiService from "../services/ApiService";
import Notificar from "../utils/Notificaciones";
import"../styles/VisualTablas.css"
import "../styles/MisAsignaciones.css"
import { useState } from "react";
import { useNavigate  } from "react-router-dom";

export default function MisAsignaciones(){
    const auth = useAuth();
    const navigate = useNavigate();
    const [claveListado, setClaveListado] = useState(0);
    const [miembrosGrupo, setMiembrosGrupo] = useState([]);
    const [modalAbierto, setModalAbierto] = useState(false);
    const [grupoActivoId, setGrupoActivoId] = useState(null);
    const [tareaSeleccionada, setTareaSeleccionada] = useState(null);
    const [modalTareaAbierto, setModalTareaAbierto] = useState(false);




    const columnas = ["tarea_Id","grupo_Id", "nombreGrupo", "rol", "estadoTarea","acciones"];

    const renderCell = (row, column) => {
        if (column === "acciones") {
            return (
                <div className="boton-rol-selector">
                    <button
                        className="boton-rol-modal"
                        onClick={() => VerMiembros(row.grupo_Id)}
                        aria-label={`Ver miembros del grupo ${row.grupo_Id}`}
                    >
                        👥 Ver miembros
                    </button>

                    <button
                        className="boton-cancelar"
                        onClick={() => DarmeBaja(row.grupo_Id)}
                        aria-label={`Darme de baja del grupo ${row.grupo_Id}`}
                    >
                        ❌ Desapuntarme
                    </button>
                </div>
            );
        }
        if (column === "estadoTarea") {
            return (
                <div className="tooltip-container">
                    <span
                        className={row.estadoTarea === "EnProgreso" ? "estado-EnProgreso" : "estado-Completada"}
                        onClick={() => CambiarEstado(row)}
                    >
                        {row.estadoTarea}
                    </span>
                    <span className="tooltip">Marcar como terminada</span>
                </div>
            );
        }
        if (column === "grupo_Id") {
            return (
                <div className="tooltip-container">
                    <span className="id-interactivo" onClick={() => irAGrupoRecursos(row.grupo_Id)}>
                        {row.grupo_Id}
                    </span>
                    <span className="tooltip">Ver Grupo y recursos</span>
                </div>
            );
        }
        if (column === "tarea_Id") {
            return (
                <div className="tooltip-container">
                    <span className="id-interactivo" onClick={() => obtenerInfoTarea(row.tarea_Id)}>
                        {row.tarea_Id}
                    </span>
                    <span className="tooltip">Ver Tarea</span>
                </div>
            );
        }
        if (column === "rol") {
            return (
                <select
                    aria-label={`Cambiar rol del grupo ${row.nombreGrupo}`}
                    className={`select rol-${row.rol}`}
                    value={row.rol}
                    onChange={(e) => { if (e.target.value !== row.rol) { CambiarRol(row, e.target.value);}}}
                >
                {[
                    { nombre: "Lider", emoji: "🧭" },
                    { nombre: "Sanitario", emoji: "🏥" },
                    { nombre: "Logistica", emoji: "📦" },
                    { nombre: "Explorador", emoji: "🔍" },
                    { nombre: "Tecnico", emoji: "🛠️" },
                    { nombre: "Comunicaciones", emoji: "📡" },
                    { nombre: "Apoyo_Emocional", emoji: "💜" },
                    { nombre: "Voluntario_General", emoji: "👥" }
                ].map(({ nombre, emoji }) => (
                    <option key={nombre} value={nombre}>
                    {emoji} {nombre}
                    </option>
                ))}
                </select>
            );
        }
        return row[column];
    };

    const CambiarEstado = async(row) =>{

    const { isAuthenticated, checkAuth } = auth;
    await checkAuth();

    if (!isAuthenticated) {
    Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
    return;
    }
    const token = auth.getAccessToken();
    const id_tarea = row.tarea_Id;
    const id_grupo = row.grupo_Id

    const result = await ApiService.patch(`/tarea_grupos/tarea/${id_tarea}/grupo/${id_grupo}`,null,token);
    if(result.success){
        Notificar.Exito("Actualización hecha");
        setClaveListado(prev => prev + 1);
    }
    else Notificar.Error("No se ha podido actualizar");
    }

    const CambiarRol = async (row, nuevoRol) => {
        const { isAuthenticated, checkAuth } = auth;
        await checkAuth();

        if (!isAuthenticated) {
            Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
            return;
        }

        if (nuevoRol === "Lider") {
            Notificar.Error("No puedes asignarte como Líder. Solo un Líder puede asignarte ese rol.");
            return;
        }

        const token = auth.getAccessToken();
        const id_grupo = row.grupo_Id;
        const body = { rol: nuevoRol };
 

        try {
            const result = await ApiService.patch(`/grupos_voluntarios/mirol/grupo/${id_grupo}`, body, token);
            if (result.success) {
                Notificar.Exito("Rol actualizado correctamente.");
                setClaveListado(prev => prev + 1); 
            } else {
                Notificar.Error("No se ha podido actualizar el rol");
            }
        } catch (error) {
            Notificar.Error("No se ha podido actualizar el rol");
        }
    };

    const VerMiembros = async (grupo_Id) => {
        const { isAuthenticated, checkAuth } = auth;
        await checkAuth();

        if (!isAuthenticated) {
            Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
            return;
        }

        const token = auth.getAccessToken();
        const result = await ApiService.get(`/grupos_voluntarios/migrupo/${grupo_Id}`, token);

        if (result.success) {
            setGrupoActivoId(grupo_Id);
            setMiembrosGrupo(result.data);
            setModalAbierto(true);
        } else {
            Notificar.Error("No se pudo obtener la lista de miembros.");
        }
    };

    const CambiarRolMiembro = async (idUsuario, nuevoRol) => {
        const { isAuthenticated, checkAuth } = auth;
        await checkAuth();

        if (!isAuthenticated) {
            Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
            return;
        }

        const token = auth.getAccessToken();
        const body = { rol: nuevoRol };

        try {
            const result = await ApiService.patch(`/grupos_voluntarios/rol/grupo/${grupoActivoId}/voluntario/${idUsuario}`, body, token);
            if (result.success) {
            Notificar.Exito("Rol del miembro actualizado");
            setClaveListado(prev => prev + 1);
            setMiembrosGrupo(prev =>
                prev.map(m => m.id === idUsuario ? { ...m, rol: nuevoRol } : m)
            );
            } else {
            Notificar.Error("No se pudo actualizar el rol.");
            }
        } catch (error) {
            Notificar.Error("Error al cambiar rol.");
        }
    };

    const DarmeBaja = async(grupo_Id) => {
        const { isAuthenticated, checkAuth } = auth;
        await checkAuth();

        if (!isAuthenticated) {
            Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
            return;
        }

        const token = auth.getAccessToken();
        try {
            const result = await ApiService.delete(`/grupos_voluntarios/grupo/${grupo_Id}`, token);
            if (result.success) {
                Notificar.Exito("Rol del miembro actualizado");
                setClaveListado(prev => prev + 1);
            } else {
            Notificar.Error("No se pudo actualizar el rol.");
            }
        } catch (error) {
            Notificar.Error("Error al cambiar rol.");
        }

    }

    const irAGrupoRecursos = (grupoId) => {
        navigate(`/asignaciones/grupoRecursos?id=${grupoId}`);
    };

    const obtenerInfoTarea = async (tarea_Id) => {
        const { isAuthenticated, checkAuth } = auth;
        await checkAuth();

        if (!isAuthenticated) {
            Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
            return;
        }

        const token = auth.getAccessToken();

        const result = await ApiService.get(`/tareas`, token, { id_tarea: tarea_Id });

        if (result.success && result.data) {
            setTareaSeleccionada(result.data[0]); 
            setModalTareaAbierto(true);
        } else {
            Notificar.Error("No se pudo obtener la información de la tarea.");
        }
    };



    const configFiltros = [
        {
            nombre: "grupo_id",
            tipo: "text",
            etiqueta: "Id del grupo",
            valorPorDefecto: ""
        },
        {
            nombre: "rol",
            tipo: "select",
            etiqueta: "Rol",
            opciones: [ "Lider","Sanitario","Logistica","Explorador","Tecnico","Comunicaciones","Apoyo_Emocional","Voluntario_General"],
            valorPorDefecto: ""
        },
        {
            nombre: "estado",
            tipo: "select",
            etiqueta: "Estado",
            opciones: [ "EnProgreso","Completada"],
            valorPorDefecto: "EnProgreso"
        },,

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

        const result = await ApiService.get("/grupos_voluntarios", token, queryParams);

        return {
            datos: result.data,
            paginacion: result.pagination,
        };
    };

      const soyLider = miembrosGrupo.some((m) => m.esMiUsuario && m.rol === "Lider");

   return (
  <>
    <Modal isOpen={modalAbierto} closeModal={() => setModalAbierto(false)} textoBoton="Cerrar">
      <div className="modal-contenido-blur tarjeta-equipo">
        <h2>🧑‍🤝‍🧑 Miembros del Grupo</h2>
        <ul className="lista-miembros">
          {miembrosGrupo.map((miembro) => (
            <li
              key={miembro.id}
              className={`item-miembro ${miembro.esMiUsuario ? "miembro-actual" : ""}`}
            >
              <div className="caja-miembro">
                <span className="nombre-miembro">
                  {miembro.esMiUsuario
                    ? `🟢 Tú (${miembro.nombre})`
                    : miembro.nombre}
                </span>

                {!miembro.esMiUsuario && soyLider ? (
                  <select
                    className={`selector-rol rol-${miembro.rol}`}
                    value={miembro.rol}
                    onChange={(e) =>
                      CambiarRolMiembro(miembro.id, e.target.value)
                    }
                  >
                    {[
                      "Lider",
                      "Sanitario",
                      "Logistica",
                      "Explorador",
                      "Tecnico",
                      "Comunicaciones",
                      "Apoyo_Emocional",
                      "Voluntario_General"
                    ].map((rol) => (
                      <option key={rol} value={rol}>
                        {rol}
                      </option>
                    ))}
                  </select>
                ) : (
                  <span className={`rol-tag rol-${miembro.rol}`}>
                    {miembro.rol}
                  </span>
                )}
              </div>
            </li>
          ))}
        </ul>
      </div>
    </Modal>
    <Modal isOpen={modalTareaAbierto} closeModal={() => setModalTareaAbierto(false)} textoBoton="Cerrar">
        {tareaSeleccionada ? (
            <div className="modal-contenido-blur tarjeta-equipo">
            <h2>📋 Información de la Tarea</h2>
            <ul className="info-tarea-lista">
                <li><strong>📝 Nombre:</strong> {tareaSeleccionada.nombre}</li>
                <li><strong>📍 Ubicación:</strong> {tareaSeleccionada.ubicacion}</li>
                <li><strong>🧾 Descripción:</strong> {tareaSeleccionada.descripcion}</li>
                <li><strong>⚠️ Gravedad:</strong> {tareaSeleccionada.gravedad}</li>
                <li><strong>🛠️ Habilidades mínimas:</strong> {tareaSeleccionada.habilidadesMin}</li>
            </ul>
            </div>
        ) : (
            <p>Cargando información...</p>
        )}
    </Modal>


    <div className="listado-glass">
      <ListadoConFiltros
        key={claveListado}
        titulo="Mis asignaciones"
        columnas={columnas}
        renderCell={renderCell}
        configFiltros={configFiltros}
        fetchDatos={fetchDatos}
      />
    </div>
  </>
);

}