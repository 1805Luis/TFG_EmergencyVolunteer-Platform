import { useEffect, useState } from "react";
import ApiService from "../services/ApiService";
import Ficha from "../components/Ficha";
import Modal from "../components/Modal";
import "../styles/GruposTarea.css";
import { useSearchParams } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import Notificar from "../utils/Notificaciones";
import { FaPlus } from "react-icons/fa";
import { useNavigate  } from "react-router-dom";


export default function GruposTarea() {
  const [grupos, setGrupos] = useState([]);
  const [modalUnirse, setModalUnirse] = useState(false);
  const [modalRecursos, setModalRecursos] = useState(false);
  const [modalCrearGrupo, setModalCrearGrupo] = useState(false);
  const [grupoSeleccionado, setGrupoSeleccionado] = useState(null);
  const [recursos, setRecursos] = useState([""]);
  const [rolSeleccionado, setRolSeleccionado] = useState("");
  const [nuevoGrupo, setNuevoGrupo] = useState({ nombre: "", zona_asignada: "", recursos: [""] });
  const [searchParams] = useSearchParams();
  const tareaId = searchParams.get("id");
  const auth = useAuth();
  const navigate = useNavigate();


  useEffect(() => {
    cargarGrupos();
  }, []);

  const cargarGrupos = async () => {
    try {
      const token = auth.getAccessToken();
      const query = { tarea_id: tareaId, estado: "EnProgreso" };
      const result = await ApiService.get("/tarea_grupos", token, query);

      if (result.success) {
        setGrupos(result.data);
      } else {
        setGrupos([]);
        Notificar.Error("No se encontraron grupos.");
      }
    } catch (error) {
      setGrupos([]);
    }
  };

  const volverAtras = () => {
    navigate(-1); 
  };

  const abrirModalUnirse = (grupo) => {
    setGrupoSeleccionado(grupo);
    setModalUnirse(true);
  };

  const cerrarModalUnirse = () => {
    setModalUnirse(false);
    setGrupoSeleccionado(null);
  };

  const confirmarUnirse = () => {
    setModalUnirse(false);
    setModalRecursos(true);
  };

  const cerrarModalRecursos = () => {
    setModalRecursos(false);
    setGrupoSeleccionado(null);
    setRecursos([""]);
  };

  const handleRecursoChange = (index, value) => {
    const nuevos = [...recursos];
    nuevos[index] = value;
    setRecursos(nuevos);
  };

   const handleRolChange = (e) => {
    setRolSeleccionado(e.target.value);
  };

  const agregarRecurso = () => {
    setRecursos([...recursos, ""]);
  };

  const eliminarRecurso = (index) => {
    setRecursos(recursos.filter((_, i) => i !== index));
  };

  const enviarRecursos = async () => {
    if(!grupoSeleccionado.grupoId) return;
    await ValidacionSeguridad()
    try {
        const token = auth.getAccessToken();

        const recursosFiltrados = recursos
            .filter((r) => r.trim() !== "")
            .map((r) => ({ nombre: r.trim() }));

        let successRecursos = true;
        let successRol = true;

        if (recursosFiltrados.length > 0) {
            var resultRecursos = await ApiService.post(`/recursos/${grupoSeleccionado.grupoId}`, recursosFiltrados, token);
            successRecursos = resultRecursos?.success;
        };

        if (rolSeleccionado) {
            const resultRol = await ApiService.post(`/grupos_voluntarios/grupo/${grupoSeleccionado.grupoId}`, { rol: rolSeleccionado }, token);
            successRol = resultRol?.success;
        }   
        
        if (successRecursos && successRol) {
            Notificar.Exito("Te has unido al grupo exitosamente.");
            cerrarModalRecursos();
            cargarGrupos();
            volverAtras();
        } else {
            Notificar.Error("Hubo un error al unirse al grupo.");
        }

    } catch (err) {
      console.error(err);
      Notificar.Error("Fallo en el envío de recursos.");
    }
  };

  const abrirModalCrearGrupo = () => {
    setNuevoGrupo({ nombre: "", zona_asignada: "", recursos: [""] });
    setModalCrearGrupo(true);
  };

  const cerrarModalCrearGrupo = () => {
    setModalCrearGrupo(false);
  };

  const handleNuevoGrupoChange = (e) => {
    const { name, value } = e.target;
    setNuevoGrupo((prev) => ({ ...prev, [name]: value }));
  };

  const handleNuevoGrupoRecursoChange = (index, value) => {
    const nuevos = [...nuevoGrupo.recursos];
    nuevos[index] = value;
    setNuevoGrupo((prev) => ({ ...prev, recursos: nuevos }));
  };

  const agregarRecursoNuevoGrupo = () => {
    setNuevoGrupo((prev) => ({ ...prev, recursos: [...prev.recursos, ""] }));
  };

  const eliminarRecursoNuevoGrupo = (index) => {
    setNuevoGrupo((prev) => ({
      ...prev,
      recursos: prev.recursos.filter((_, i) => i !== index),
    }));
  };

  const ValidacionSeguridad = async() => {
    const { isAuthenticated, checkAuth } = auth;
    await checkAuth();

    if (!isAuthenticated) {
        Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
        return;
    }
  }

  const crearGrupo = async () => {
    console.log("Crear grupo")
    try {
        await ValidacionSeguridad();
        const token = auth.getAccessToken();
        const payload = {
            id_tarea: tareaId,
            grupo: {
                nombre: nuevoGrupo.nombre,
                zona_asignada: nuevoGrupo.zona_asignada,
                recursos: nuevoGrupo.recursos
                .filter((r) => r.trim() !== "")
                .map((r) => ({ nombre: r.trim() })),
            },
        };



      const result = await ApiService.post("/grupos", payload, token);

      if (result.success) {
        Notificar.Exito("Grupo creado con éxito");
        cerrarModalCrearGrupo();
        volverAtras();
      } else {
        Notificar.Error("Error al crear grupo");
      }
    } catch (err) {
      Notificar.Error("Error en la creación del grupo");
      console.error("Error en la creación del grupo:", err);
    }
  };

  return (
    <div className="tareas-panel-grupos">
      {grupos.length === 0 && (
        <div className="sin-grupos">
          <h3>No se han encontrado grupos activos para esta tarea.</h3>
          <p>¡Puedes crear uno nuevo pulsando el botón verde abajo!</p>
        </div>
      )}

      {grupos.map((grupo) => (
        <Ficha key={grupo.grupoId} onClick={() => abrirModalUnirse(grupo)}gravedad={"media"}>
          <h2>{grupo.nombreGrupo}</h2>
          <p>
            <strong>Zona Asignada:</strong> {grupo.zonaAsignada}
          </p>
        </Ficha>
      ))}

     {/* Modal: Confirmar unión */}
      <Modal isOpen={modalUnirse} closeModal={cerrarModalUnirse} textoBoton="Cancelar">
        <div className="modal-contenido">
          <h3>¿Deseas unirte a este grupo?</h3>
          <div className="modal-buttons-tarea">
            <button onClick={confirmarUnirse}>Sí, añadir recursos y rol</button>
          </div>
        </div>
      </Modal>

      {/* Modal: Añadir recursos y rol */}
      <Modal isOpen={modalRecursos} closeModal={cerrarModalRecursos} textoBoton="Cancelar">
        <div className="modal-contenido">
          <h3>Añade tus recursos (opcional) y selecciona tu rol</h3>
          <div className="formulario-campo">
            <label htmlFor="rol">Rol:</label>
            <select id="rol" value={rolSeleccionado} onChange={handleRolChange}>
              <option value="">-- Selecciona un rol --</option>
              <option value="Sanitario">Sanitario</option>
              <option value="Logistica">Logistica</option>
              <option value="Explorador">Explorador</option>
              <option value="Tecnico">Tecnico</option>
              <option value="Comunicaciones">Comunicaciones</option>
              <option value="Apoyo_Emocional">Apoyo_Emocional</option>
              <option value="Voluntario_General">Voluntario_General</option>
            </select>
          </div>

          <div className="recursos-section">
            {recursos.map((recurso, i) => (
              <div key={i} className="recurso-item">
                <input
                  type="text"
                  value={recurso}
                  onChange={(e) => handleRecursoChange(i, e.target.value)}
                  placeholder="Nombre del recurso"
                />
                <button onClick={() => eliminarRecurso(i)}>X</button>
              </div>
            ))}
            <button onClick={agregarRecurso} className="boton-secundario">Añadir recurso</button>
          </div>

          <button className="boton-accion" onClick={enviarRecursos}>Confirmar unión</button>
        </div>
      </Modal>

      {/* Modal: Crear grupo */}
      <Modal isOpen={modalCrearGrupo} closeModal={cerrarModalCrearGrupo} textoBoton="Cancelar">
        <div className="modal-contenido">
          <h2>Crear nuevo grupo</h2>

          <div className="formulario-campo">
            <label htmlFor="nombreGrupo">Nombre del grupo:</label>
            <input
              id="nombreGrupo"
              name="nombre"
              value={nuevoGrupo.nombre}
              onChange={handleNuevoGrupoChange}
            />
          </div>

          <div className="formulario-campo">
            <label htmlFor="zonaAsignada">Zona asignada:</label>
            <input
              id="zonaAsignada"
              name="zona_asignada"
              value={nuevoGrupo.zona_asignada}
              onChange={handleNuevoGrupoChange}
            />
          </div>

          <div className="recursos-section">
            <h4>Recursos</h4>
            {nuevoGrupo.recursos.map((recurso, index) => (
              <div key={index} className="recurso-item">
                <input
                  value={recurso}
                  onChange={(e) => handleNuevoGrupoRecursoChange(index, e.target.value)}
                  placeholder="Nombre del recurso"
                />
                <button type="button" onClick={() => eliminarRecursoNuevoGrupo(index)}>Eliminar</button>
              </div>
            ))}
            <button type="button" onClick={agregarRecursoNuevoGrupo} className="boton-secundario">Añadir recurso</button>
          </div>

          <button className="boton-accion" onClick={crearGrupo}>Crear grupo</button>
        </div>
      </Modal>


      <button className="boton-flotante" onClick={abrirModalCrearGrupo} title="Añadir nuevo grupo">
        <FaPlus />
      </button>
    </div>
  );
}
