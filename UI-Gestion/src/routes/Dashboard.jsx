import { useState, useEffect } from "react";
import { useAuth } from "../auth/AuthProvider";
import Mapa from "../components/Mapa.jsx";
import "../styles/Dashboard.css";
import Ficha from "../components/Ficha";
import Modal from "../components/Modal";
import ApiService from "../services/ApiService";
import { FaFilter } from "react-icons/fa";
import FiltrosGenericos from "../components/FiltrosGenericos.jsx";
import { useNavigate } from "react-router-dom";
import Notificar from "../utils/Notificaciones";
import { FaTimes } from "react-icons/fa";


const VISIBLE_COUNT = 3;
const ITEM_HEIGHT_VH = 27;

export default function Dashboard() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [mapKey, setMapKey] = useState(0);
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedEmergencia, setSelectedEmergencia] = useState(null);
  const [emergencias, setEmergencias] = useState([]);
  const [valoresFiltro, setValoresFiltro] = useState({});
  const [filtroOpen, setFiltroOpen] = useState(false);
  const [mostrarLista, setMostrarLista] = useState(false);
  const [tareas, setTareas] = useState([]);
  const [modoEdicion, setModoEdicion] = useState(false);
  const [modalTareaOpen, setModalTareaOpen] = useState(false);
  const [modalOpcionesTareaOpen, setModalOpcionesTareaOpen] = useState(false);
  const [tareaSeleccionadaOpciones, setTareaSeleccionadaOpciones] = useState(null);

  const [tareaForm, setTareaForm] = useState({
        nombre: "",
        ubicacion: "",
        descripcion: "",
        gravedad: "baja",
        estado: "SinEmpezar",
        habilidadesMin: "",
  });

  


  useEffect(() => {
    cargarEmergencias();
  }, [valoresFiltro,mapKey]);

  const cargarEmergencias = async () => {
    await VerificacionSeguridad()
    const { isAuthenticated, checkAuth } = auth;
    await checkAuth();

    if (!isAuthenticated) {
      Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
      return;
    }

    const queryParams = { ...valoresFiltro };
    const token = auth.getAccessToken();
    setEmergencias([]);

    const result = await ApiService.get("/emergencias", token, queryParams);

    if (result.success) {
      setEmergencias(result.data);
    } else {
      console.error("Error al cargar emergencias");
    }
  };

  const VerificacionSeguridad = async () => {
        const { isAuthenticated, checkAuth } = auth;
        await checkAuth();
    
        if (!isAuthenticated) {
          Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
          return false;
        }
        return true;
      };

  const CreateTarea = () => {
    setTareaForm({
      nombre: "",
      ubicacion: "",
      descripcion: "",
      gravedad: "baja",
      estado: "SinEmpezar",
      habilidadesMin: "",
    });
    setModoEdicion(true);
    setModalTareaOpen(true);
  };

  const handleTareaFormChange = (e) => {
    const { name, value } = e.target;
    setTareaForm((prev) => ({ ...prev, [name]: value }));
  };

  const crearTarea = async () => {
    if (!selectedEmergencia?.id) return;
    await VerificacionSeguridad();
    const token = auth.getAccessToken();
    const payload = { ...tareaForm };
    const result = await ApiService.post(`/tareas/${selectedEmergencia.id}`, payload, token);

    if (result.success) {
      await cargarTareas(selectedEmergencia.id);
      Notificar.Exito("Tarea creada con éxito");
      setModalTareaOpen(false);
    } else {
      Notificar.Error("Error al crear la tarea");
    }
  };

  const actualizarTarea = async () => {
    if (!tareaForm?.id) return;
    await VerificacionSeguridad();
    const token = auth.getAccessToken();
    const result = await ApiService.patch(`/tareas/${tareaForm.id}`, tareaForm, token);

    if (result.success) {
      await cargarTareas(selectedEmergencia.id);
      Notificar.Exito("Tarea actualizada correctamente");
      setModalTareaOpen(false);
    } else {
      Notificar.Error("Error al actualizar la tarea");
    }
  };


  const abrirModalOpcionesTarea = (tarea) => {
    setTareaSeleccionadaOpciones(tarea);
    setModalOpcionesTareaOpen(true);
  };


  const cargarTareas = async (emergenciaId) => {
    await VerificacionSeguridad()
    const { isAuthenticated } = auth;

    if (!isAuthenticated) {
      Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
      return;
    }
    try{
      const token = auth.getAccessToken();
      const queryParams = { emergencia_id: emergenciaId };

      const result = await ApiService.get("/tareas", token, queryParams);

      if (result.success) {
        if (result.data.length === 0) {
          Notificar.Info("No hay tareas para esta emergencia.");
          setTareas([]);
          setMostrarLista(true);
        } else {
          setTareas(result.data);
          setMostrarLista(true);
        }
      } else {
        Notificar.Error("No se pudieron cargar las tareas.");
        setTareas([]);
        setMostrarLista(true);
      }
    }catch{
      Notificar.Error("No se ha podido conectar");
      setTareas([]);
      setMostrarLista(true);
    }
    

  };

  // Esta función se llama al clicar un polígono en el mapa
  const onEmergenciaClick = (emergencia) => {
    setSelectedEmergencia(emergencia);
    cargarTareas(emergencia.id);
  };

  const closeListaTareas = () => {
    setMostrarLista(false);
    setTareas([]);
    setSelectedEmergencia(null);
    setMapKey(prev => prev + 1);

      setTimeout(() => {
        const mapContainer = document.querySelector(".leaflet-container");
        if (mapContainer?._leaflet_map) {
          mapContainer._leaflet_map.invalidateSize(); // recalcula el canvas
          mapContainer._leaflet_map.setView([35.5, -6.5], 5.4); // vista base
        }
      }, 300);
  };


  const handleUp = () => {
    setStartIndex((prev) => (prev - 1 + emergencias.length) % emergencias.length);
  };

  const handleDown = () => {
    setStartIndex((prev) => (prev + 1) % emergencias.length);
  };

  const eliminarTarea = async (tarea) => {
    if (!tarea) return;
    await VerificacionSeguridad();
    const token = auth.getAccessToken();


    const result = await ApiService.delete(`/tareas/${tarea.id}`, token);

    if (result.success) {
      await cargarTareas(tarea.emergencia_id);
      Notificar.Exito("Tarea eliminada correctamente");
    } else {
      Notificar.Error("Error eliminando tarea");
    }
  };

  const Eliminar = async () => {
    if (selectedEmergencia?.id) {
      await VerificacionSeguridad();
      const token = auth.getAccessToken();
      const result = await ApiService.delete(`/emergencias/${selectedEmergencia.id}`, token);

      if (result.success) {
        setMapKey(prev => prev + 1);
        setModalOpen(false);
        setSelectedEmergencia(null);
        setMostrarLista(false);

        Notificar.Exito("Emergencia eliminada con éxito");
      } else {
        Notificar.Error("Error eliminando emergencia");
        setModalOpen(false);
      }
    }
  };

  const Create = () => {
    navigate(`/formulario/emergencia?action=create`);
  };

  const Modificar = () => {
    if (selectedEmergencia?.id) {
      navigate(`/formulario/emergencia?action=update&id=${selectedEmergencia.id}`);
      setModalOpen(false);
    }
  };


  const filtrosActivosCount = Object.values(valoresFiltro).filter((valor) => {
    if (valor === null || valor === undefined) return false;
    if (typeof valor === "string") return valor.trim() !== "";
    if (typeof valor === "number") return true;
    return true;
  }).length;

  return (
    <div key={mapKey} className="dashboard">
      <div className="mapa-container" style={{ width: mostrarLista ? "60%" : "100%" }}>
        <Mapa
          geoJsonList={emergencias.map((e) => e.areaAfectadaGeoJson)}
          emergenciasList={emergencias}
          onPolygonClick={onEmergenciaClick}
          selectedEmergencia={selectedEmergencia}
          mostrarLista={mostrarLista}
          onModificarClick={Modificar}
          onEliminarClick={Eliminar}
        />
      </div>

      {mostrarLista && (
        <div className="info-container vertical-carousel" style={{ width: "40%" }}>
          {tareas.length > 0 ? (
            <>
              <button onClick={handleUp} className="flecha">▲</button>
              <div
                className="ficha-lista-viewport"
                style={{ height: `${VISIBLE_COUNT * ITEM_HEIGHT_VH}vh`, overflow: "hidden" }}
              >
                <div className="ficha-lista-slider">
                  {tareas.map((tarea) => (
                    <Ficha
                      key={tarea.id}
                      gravedad={tarea.gravedad.toLowerCase()}
                      onClick={() => abrirModalOpcionesTarea(tarea)}
                    >
                      <h2>{tarea.nombre}</h2>
                      <p><strong>Ubicación:</strong> {tarea.ubicacion}</p>
                      <p><strong>Descripción:</strong> {tarea.descripcion}</p>
                      <p><strong>Gravedad:</strong> {tarea.gravedad}</p>
                      <p><strong>Estado:</strong> {tarea.estado}</p>
                      <p><strong>Habilidades mínimas:</strong> {tarea.habilidadesMin}</p>
                    </Ficha>
                  ))}
                </div>
              </div>
              <button onClick={handleDown} className="flecha">▼</button>
            </>
          ) : (
            <div className="sin-tareas">
              <h3>No hay tareas disponibles para esta emergencia</h3>
              <p>Puedes añadir nuevas tareas con el botón ＋</p>
            </div>
          )}
        </div>
      )}

      {mostrarLista && !modalTareaOpen && (
        <button
          className="boton-cerrar-lista"
          onClick={closeListaTareas}
          title="Cerrar lista de tareas"
        >
          <FaTimes />
        </button>
      )}



      <button className="boton-filtro" onClick={() => setFiltroOpen(true)}>
        <FaFilter />
        {filtrosActivosCount > 0 && <span className="filtro-count">{filtrosActivosCount}</span>}
      </button>

      <button className="boton-flotante" onClick={mostrarLista ? CreateTarea : Create}
        title={mostrarLista ? "Añadir nueva tarea" : "Añadir nueva emergencia"}
      >
        ＋
      </button>



      <Modal isOpen={filtroOpen} closeModal={() => setFiltroOpen(false)} textoBoton={"Cerrar"}>
        <h2>Filtros de Emergencias</h2>
        <FiltrosGenericos
          config={[
            { nombre: "tipo_emergencia", tipo: "text", etiqueta: "Tipo de emergencia" },
            { nombre: "zona_afectada", tipo: "text", etiqueta: "Zona afectada" },
            { nombre: "descripcion", tipo: "text", etiqueta: "Descripción" },
            { nombre: "estado", tipo: "select", etiqueta: "Estado", opciones: ["EnProgreso", "Completada"] },
            { nombre: "gravedad", tipo: "select", etiqueta: "Gravedad", opciones: ["baja", "media", "alta", "critica"] },
            { nombre: "lat", tipo: "number", etiqueta: "Latitud", min: -90, max: 90 },
            { nombre: "lon", tipo: "number", etiqueta: "Longitud", min: -180, max: 180 },
            { nombre: "distanciaMetros", tipo: "number", etiqueta: "Radio (m)", min: 0 },
          ]}
          valoresIniciales={valoresFiltro}
          onCambio={(campo, valor) => setValoresFiltro((prev) => ({ ...prev, [campo]: valor }))}
          onAplicar={(filtrosAplicados) => {
            console.log("🧪 Filtros aplicados:", filtrosAplicados);
            setFiltroOpen(false);
          }}
        />
      </Modal>

     <Modal isOpen={modalTareaOpen} closeModal={() => setModalTareaOpen(false)} textoBoton="Cerrar">
        <div className="modal-tarea-container">
          <h2>{modoEdicion ? (tareaForm.id ? "Editar tarea" : "Nueva tarea") : tareaForm.nombre}</h2>

          <form className="formulario-tarea" onSubmit={(e) => e.preventDefault()}>
            <div className="formulario-campo">
              <label htmlFor="nombre">Nombre</label>
              <input id="nombre" name="nombre" value={tareaForm.nombre} onChange={handleTareaFormChange} />
            </div>

            <div className="formulario-campo">
              <label htmlFor="ubicacion">Ubicación</label>
              <input id="ubicacion" name="ubicacion" value={tareaForm.ubicacion} onChange={handleTareaFormChange} />
            </div>

            <div className="formulario-campo">
              <label htmlFor="descripcion">Descripción</label>
              <textarea id="descripcion" name="descripcion" value={tareaForm.descripcion} onChange={handleTareaFormChange} />
            </div>

            <div className="formulario-fila">
              <div className="formulario-campo">
                <label htmlFor="gravedad">Gravedad</label>
                <select id="gravedad" name="gravedad" value={tareaForm.gravedad} onChange={handleTareaFormChange}>
                  <option value="baja">baja</option>
                  <option value="media">media</option>
                  <option value="alta">alta</option>
                  <option value="critica">critica</option>
                </select>
              </div>

              <div className="formulario-campo">
                <label htmlFor="estado">Estado</label>
                <select id="estado" name="estado" value={tareaForm.estado} onChange={handleTareaFormChange}>
                  <option value="SinEmpezar">SinEmpezar</option>
                  <option value="EnProgreso">EnProgreso</option>
                  <option value="Completada">Completada</option>
                </select>
              </div>
            </div>

            <div className="formulario-campo">
              <label htmlFor="habilidadesMin">Habilidades mínimas</label>
              <input id="habilidadesMin" name="habilidadesMin" value={tareaForm.habilidadesMin} onChange={handleTareaFormChange} />
            </div>

            <button
              type="button"
              className="boton-accion-principal"
              onClick={tareaForm.id ? actualizarTarea : crearTarea}
            >
              {tareaForm.id ? "Guardar cambios" : "Crear tarea"}
            </button>
          </form>
        </div>
      </Modal>


      <Modal
        isOpen={modalOpcionesTareaOpen}
        closeModal={() => setModalOpcionesTareaOpen(false)}
        textoBoton="Cancelar"
      >
        <h2>¿Qué deseas hacer con la tarea "{tareaSeleccionadaOpciones?.nombre}"?</h2>
        <div style={{ display: "flex", justifyContent: "space-around", marginTop: "1rem" }}>
          <button
            onClick={() => {
              setModalOpcionesTareaOpen(false);
              setTareaForm(tareaSeleccionadaOpciones);
              setModoEdicion(true);
              setModalTareaOpen(true);
            }}
          >
            Editar tarea
          </button>
          <button
            onClick={() => {
              setModalOpcionesTareaOpen(false);
              navigate(`/grupos/tarea?id=${tareaSeleccionadaOpciones?.id}`);
            }}
          >
            Ver grupos asociados
          </button>
          <button
            onClick={async () => {
              setModalOpcionesTareaOpen(false);
              const confirmado = window.confirm(`¿Estás seguro de eliminar la tarea "${tareaSeleccionadaOpciones?.nombre}"?`);
              if (!confirmado) return;
              await eliminarTarea(tareaSeleccionadaOpciones);
            }}
            style={{ backgroundColor: "red", color: "white" }}
          >
            Eliminar tarea
          </button>
        </div>
      </Modal>

    </div>
  );
}
