import { useEffect, useState } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import ApiService from "../services/ApiService";
import MapaCoordenadas from "../components/MapaCoordenadas";
import "../styles/FormularioEmergencia.css";
import Notificar from "../utils/Notificaciones";
import Modal from "../components/Modal";
import { useAuth } from "../auth/AuthProvider";



export default function FormularioEmergencia() {
   const auth = useAuth();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const action = searchParams.get("action");
  const id_emergencia = searchParams.get("id");

  const isCreate = action === "create";
  const isUpdate = action === "update" && id_emergencia;

  const [formulario, setFormulario] = useState({
    tipo_emergencia: "",
    zona_afectada: "",
    descripcion: "",
    gravedad: "baja",
    Coordenadas: [],
  });

  const [tareas, setTareas] = useState([]);
  const [showModal, setShowModal] = useState(false);
  const [formularioOriginal, setFormularioOriginal] = useState(null);


  useEffect(() => {
    

    if (action === "update" && id_emergencia) {
      cargarEmergencia(id_emergencia);
    }
  }, []); // ← se ejecuta solo una vez al montar el componente


  const cargarEmergencia = async (id) => {
    await ValidacionSeguridad();
    const token = auth.getAccessToken();
    const result = await ApiService.get(`/emergencias`, token, { id });

    if (result.success) {
      const emergencia = result.data[0];
      const coords = 
      emergencia.areaAfectadaGeoJson?.coordinates?.[0] || [];

      setFormulario({
        tipo_emergencia: emergencia.tipo_emergencia || "",
        zona_afectada: emergencia.zona_afectada || "",
        descripcion: emergencia.descripcion || "",
        gravedad: emergencia.gravedad || "baja",
        Coordenadas: coords,
      });

      setFormularioOriginal({
        tipo_emergencia: emergencia.tipo_emergencia || "",
        zona_afectada: emergencia.zona_afectada || "",
        descripcion: emergencia.descripcion || "",
        gravedad: emergencia.gravedad || "baja",
        Coordenadas: coords,
      });

    } else {
      Notificar.Error("No se pudo cargar la emergencia");
    }
};


  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormulario((prev) => ({ ...prev, [name]: value }));
  };

  const actualizarCoordenadas = (coords) => {
    setFormulario((prev) => ({ ...prev, Coordenadas: coords }));
  };

  const handleTareaChange = (index, e) => {
    const { name, value } = e.target;
    const nuevas = [...tareas];
    nuevas[index][name] = value;
    setTareas(nuevas);
  };

  const agregarTarea = () => {
    setTareas((prev) => [
      ...prev,
      { Nombre: "", Ubicacion: "", Descripcion: "", gravedad: "baja", HabilidadesMin: "" },
    ]);
  };

  const eliminarTarea = (index) => {
    setTareas((prev) => prev.filter((_, i) => i !== index));
  };

    const validarFormulario = () => {
        // Validar campos del formulario principal
        if (!formulario.tipo_emergencia.trim()) {
            Notificar.Error("Por favor, rellena el campo 'Tipo de emergencia'.");
            return false;
        }
        if (!formulario.zona_afectada.trim()) {
            Notificar.Error("Por favor, rellena el campo 'Zona afectada'.");
            return false;
        }
        if (!formulario.descripcion.trim()) {
            Notificar.Error("Por favor, rellena el campo 'Descripción'.");
            return false;
        }
        
        const opcionesGravedad = ["baja", "media", "alta", "critica"];
        if (!opcionesGravedad.includes(formulario.gravedad)) {
            Notificar.Error("Selecciona una gravedad válida en la emergencia.");
            return false;
        }

        if (formulario.Coordenadas.length < 3) {
            Notificar.Error("Por favor, añade al menos 3 coordenadas para definir la zona.");
            return false;
        }

        // Validar tareas (debe haber al menos una)
        if (tareas.length === 0) {
            Notificar.Error("Debes añadir al menos una tarea asociada.");
            return false;
        }

        for (let i = 0; i < tareas.length; i++) {
            const tarea = tareas[i];
            if (!tarea.Nombre.trim()) {
                Notificar.Error(`Por favor, rellena el campo 'Nombre' en la tarea #${i + 1}.`);
                return false;
            }
            if (!tarea.Ubicacion.trim()) {
                Notificar.Error(`Por favor, rellena el campo 'Ubicación' en la tarea #${i + 1}.`);
                return false;
            }
            if (!tarea.Descripcion.trim()) {
                Notificar.Error(`Por favor, rellena el campo 'Descripción' en la tarea #${i + 1}.`);
                return false;
            }
            if (!opcionesGravedad.includes(tarea.gravedad)) {
                Notificar.Error(`Selecciona una gravedad válida en la tarea #${i + 1}.`);
                return false;
            }
            if (!tarea.HabilidadesMin.trim()) {
                Notificar.Error(`Por favor, rellena el campo 'Habilidades mínimas' en la tarea #${i + 1}.`);
                return false;
            }
        }

        return true;
    };

    const obtenerCamposModificados = (original, actualizado) => {
      const cambios = {};

      for (const clave in actualizado) {
        const valorActual = actualizado[clave];
        const valorOriginal = original?.[clave];

        // Comparación profunda si es un array de coordenadas
        if (Array.isArray(valorActual) && Array.isArray(valorOriginal)) {
          const jsonActual = JSON.stringify(valorActual);
          const jsonOriginal = JSON.stringify(valorOriginal);
          if (jsonActual !== jsonOriginal) {
            cambios[clave] = valorActual;
          }
        } else if (valorActual !== valorOriginal) {
          cambios[clave] = valorActual;
        }
      }

      return cambios;
    };


    const resetFormulario = () => {
        setFormulario({
        tipo_emergencia: "",
        zona_afectada: "",
        descripcion: "",
        gravedad: "baja",
        Coordenadas: [],
        });
        setTareas([]);
    };

    const ValidacionSeguridad = async () =>{
        const { isAuthenticated, checkAuth } = auth;
        await checkAuth();
    
        if (!isAuthenticated) {
            Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
            return;
        }
    }


  // Función submit (modifica según tu API)
  const handleSubmit = async () => {
    // Aquí construyes el objeto con datos
    const payload = { ...formulario, tareas };

    await ValidacionSeguridad();

    const token = auth.getAccessToken();

    if (isCreate) {
        if (!validarFormulario()) return;
        const result = await ApiService.post("/emergencias",payload, token);
        if(result.success){
            Notificar.Exito("Se ha creado la emergencia y las tareas correctamente");
            setShowModal(true);
        }
        else Notificar.Error("No se ha podido crear");
    } else if (isUpdate) {
        const cambios = obtenerCamposModificados(formularioOriginal, formulario);
        const payload = { ...cambios };
        const result = await ApiService.patch(`/emergencias/${id_emergencia}`,payload, token);
        if(result.success){
            Notificar.Exito("Se ha actualizado la emergencia");
            setShowModal(true);
            
        }
        else Notificar.Error("No se ha podido actualizar");
    }
    // Luego navegar o mostrar notificaciones
  };

  return (
    <div className="formulario-emergencia-container">
      <div className="mapa-columna">
        <MapaCoordenadas coordenadas={formulario.Coordenadas} onChangeCoordenadas={actualizarCoordenadas} />
      </div>

      <div className="formulario-columna">
        <h2>{isCreate ? "Crear Emergencia" : "Editar Emergencia"}</h2>

        <label>
          Tipo de emergencia:
          <input name="tipo_emergencia" value={formulario.tipo_emergencia} onChange={handleChange} />
        </label>

        <label>
          Zona afectada:
          <input name="zona_afectada" value={formulario.zona_afectada} onChange={handleChange} />
        </label>

        <label>
          Descripción:
          <textarea name="descripcion" value={formulario.descripcion} onChange={handleChange} />
        </label>

        <label>
          Gravedad:
          <select name="gravedad" value={formulario.gravedad} onChange={handleChange}>
            <option value="baja">baja</option>
            <option value="media">media</option>
            <option value="alta">alta</option>
            <option value="critica">critica</option>
          </select>
        </label>

        {isCreate && (
          <div className="tareas-section">
            <h3>Tareas asociadas</h3>
            {tareas.map((tarea, index) => (
              <div key={index} className="tarea-card">
                <label>
                  Nombre:
                  <input name="Nombre" value={tarea.Nombre?? ""} onChange={(e) => handleTareaChange(index, e)} />
                </label>
                <label>
                  Ubicación:
                  <input name="Ubicacion" value={tarea.Ubicacion?? ""} onChange={(e) => handleTareaChange(index, e)} />
                </label>
                <label>
                  Descripción:
                  <input name="Descripcion" value={tarea.Descripcion?? ""} onChange={(e) => handleTareaChange(index, e)} />
                </label>
                <label>
                  Gravedad:
                  <select name="gravedad" value={tarea.gravedad?? "baja"} onChange={(e) => handleTareaChange(index, e)}>
                    <option value="baja">baja</option>
                    <option value="media">media</option>
                    <option value="alta">alta</option>
                    <option value="critica">critica</option>
                  </select>
                </label>
                <label>
                  Habilidades mínimas:
                  <input name="HabilidadesMin" value={tarea.HabilidadesMin?? ""} onChange={(e) => handleTareaChange(index, e)} />
                </label>
                <button type="button" onClick={() => eliminarTarea(index)} className="boton-eliminar-tarea">
                  Eliminar tarea
                </button>
              </div>
            ))}
            <button type="button" onClick={agregarTarea} className="boton-agregar-tarea">
              Añadir tarea
            </button>
          </div>
        )}

        {/* Botones al final */}
        <div style={{ marginTop: "1.5rem", display: "flex", gap: "1rem" }}>
          <button type="button" onClick={() => navigate(-1)} className="boton-cancelar">
            Cancelar
          </button>

          <button type="button" onClick={handleSubmit} className="boton-accion">
            {isCreate ? "Crear" : isUpdate ? "Actualizar" : "Guardar"}
          </button>
        </div>
      </div>
        <Modal
            isOpen={showModal}
            closeModal={() => setShowModal(false)}
            textoBoton={null} // Quitamos el botón que cierra por defecto
        >
            <h3>{isCreate ? "¿Quieres crear otra emergencia?" : "¿Quieres seguir actualizando la emergencia?"}</h3>
            <div style={{ display: "flex", justifyContent: "center", gap: "1.5rem", marginTop: "1rem" }}>
            <button
                className="boton-accion"
                onClick={() => {
                if (isCreate) resetFormulario();
                setShowModal(false);
                }}
            >
                Sí
            </button>
            <button
                className="boton-cancelar"
                onClick={() => {
                setShowModal(false);
                navigate("/dashboard"); // Cambia si quieres a otra ruta
                }}
            >
                No
            </button>
            </div>
        </Modal>
    </div>
  );
}
