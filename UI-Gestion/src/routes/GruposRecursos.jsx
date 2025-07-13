import { useState, useEffect } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import ApiService from "../services/ApiService";
import TarjetaPerfil from "../components/Tarjeta";
import Notificar from "../utils/Notificaciones";
import Modal from "../components/Modal";
import "../styles/GruposRecursos.css";

export default function GruposRecursos() {
  const [searchParams] = useSearchParams();
  const grupoId = searchParams.get("id");
  const auth = useAuth();
  const navigate = useNavigate();

  const [grupo, setGrupo] = useState(null);
  const [recursoActivo, setRecursoActivo] = useState(0);
  const [claveGrupos, setClaveGrupos] = useState(0);

  const [nombreGrupo, setNombreGrupo] = useState("");
  const [zonaGrupo, setZonaGrupo] = useState("");
  const [nombreRecurso, setNombreRecurso] = useState("");

  const [giradaGrupo, setGiradaGrupo] = useState(false);
  const [giradaRecurso, setGiradaRecurso] = useState(false);

  const [modalActivo, setModalActivo] = useState(false);
  const [nuevoNombreRecurso, setNuevoNombreRecurso] = useState("");

  useEffect(() => {
    const cargarDatos = async () => {
      const token = auth.getAccessToken();
      const result = await ApiService.get("/grupos", token, { grupoId });

      if (result.success && result.data.length > 0) {
        const datosGrupo = result.data[0];
        setGrupo(datosGrupo);
        setNombreGrupo(datosGrupo.nombre || "");
        setZonaGrupo(datosGrupo.zonaAsignada || "");
        setNombreRecurso(datosGrupo.recursos?.[0]?.nombre || "");
      } else {
        Notificar.Error("No se pudo cargar el grupo.");
      }
    };

    cargarDatos();
  }, [grupoId, claveGrupos]);

  const VerificacionSeguridad = async () => {
    const { isAuthenticated, checkAuth } = auth;
    await checkAuth();

    if (!isAuthenticated) {
      Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
      return false;
    }
    return true;
  };

  const actualizarGrupo = async () => {
    if (!(await VerificacionSeguridad())) return;
    const token = auth.getAccessToken();
    const body = {
      nombre: nombreGrupo,
      zona_asignada: zonaGrupo,
    };

    const result = await ApiService.patch(`/grupos/${grupo.id}`, body, token);
    if (result.success) {
      Notificar.Exito("Grupo actualizado");
      setClaveGrupos(prev => prev + 1);
    } else {
      Notificar.Error("No se pudo actualizar el grupo.");
    }
  };

  const eliminarGrupo = async () => {
    const confirmar = window.confirm("¿Eliminar este grupo?");
    if (!confirmar) return;

    const token = auth.getAccessToken();
    const result = await ApiService.delete(`/grupos/${grupo.id}`, token);
    if (result.success) {
      navigate("/", { replace: true });
      Notificar.Exito("Grupo eliminado");
    } else {
      Notificar.Error("No se pudo eliminar el grupo.");
    }
  };

  const crearRecurso = () => {
    setNuevoNombreRecurso("");
    setModalActivo(true);
  };

  const confirmarCreacionRecurso = async () => {
    if (!nuevoNombreRecurso.trim()) {
      Notificar.Error("El nombre no puede estar vacío");
      return;
    }

    if (!(await VerificacionSeguridad())) return;

    const token = auth.getAccessToken();
    const body = { nombre: nuevoNombreRecurso };
    const result = await ApiService.post(`/recursos/${grupoId}`, body, token);

    if (result.success) {
      setClaveGrupos(prev => prev + 1);
      Notificar.Exito("Recurso creado");
    } else {
      Notificar.Error("No se pudo crear el recurso.");
    }

    setModalActivo(false);
  };

  const recurso = grupo?.recursos?.[recursoActivo];
  const esMiRecurso = (IdUsuario, VoluntarioId) => IdUsuario === VoluntarioId;

  const actualizarRecurso = async () => {
    if (!(await VerificacionSeguridad())) return;

    const token = auth.getAccessToken();
    const IdUsuario = auth.getUsuarioId(token);
    const VoluntarioId = recurso?.voluntarioId;

    if (esMiRecurso(IdUsuario, VoluntarioId)) {
      const result = await ApiService.patch(`/recursos/${recurso.id}`, { nombre: nombreRecurso }, token);
      if (result.success) {
        Notificar.Exito("Actualización hecha");
        setClaveGrupos(prev => prev + 1);
      }
    } else {
      Notificar.Error("No puedes modificar recursos que no son tuyos");
    }
  };

  const eliminarRecurso = async () => {
    if (!(await VerificacionSeguridad())) return;

    const token = auth.getAccessToken();
    const IdUsuario = auth.getUsuarioId(token);
    const VoluntarioId = recurso?.voluntarioId;

    if (esMiRecurso(IdUsuario, VoluntarioId)) {
      const result = await ApiService.delete(`/recursos/${recurso.id}`, token);
      if (result.success) {
        Notificar.Exito("Eliminación hecha correctamente");
        setClaveGrupos(prev => prev + 1);
      }
    } else {
      Notificar.Error("No puedes modificar recursos que no son tuyos");
    }
  };

  const avanzar = () => {
    if (!grupo?.recursos?.length) return;
    const siguiente = (recursoActivo + 1) % grupo.recursos.length;
    setRecursoActivo(siguiente);
    setNombreRecurso(grupo.recursos[siguiente]?.nombre || "");
    setGiradaRecurso(false);
  };

  return (
    <div className="layout-recursos">
      {/* 🛡️ Grupo */}
      {grupo && (
        <div className="tarjeta-grupo-lateral">
          <TarjetaPerfil
            titulo="🛡️ Grupo táctico"
            tipoFondo="bg-personales"
            girada={giradaGrupo}
            onGirar={() => setGiradaGrupo(prev => !prev)}
            onAceptar={actualizarGrupo}
            onCancelar={() => {
              setNombreGrupo(grupo.nombre);
              setZonaGrupo(grupo.zonaAsignada);
              setGiradaGrupo(false);
            }}
            campos={[
              { label: "Nombre", valor: nombreGrupo, tipo: "text", onChange: setNombreGrupo },
              { label: "Zona asignada", valor: zonaGrupo, tipo: "text", onChange: setZonaGrupo },
            ]}
          />

          <div className="botones-accion">
            <button onClick={eliminarGrupo}>🗑️ Eliminar grupo</button>
            <button onClick={crearRecurso}>➕ Crear recurso</button>
          </div>
        </div>
      )}

      {/* 🔧 Recurso activo */}
      {grupo?.recursos?.length > 0 && (
        <div className="zona-recurso-activo">
          <TarjetaPerfil
            titulo="🔧 Recurso asignado"
            tipoFondo="bg-credenciales"
            girada={giradaRecurso}
            onGirar={() => setGiradaRecurso(prev => !prev)}
            onAceptar={actualizarRecurso}
            onCancelar={() => {
              setNombreRecurso(recurso?.nombre);
              setGiradaRecurso(false);
            }}
            campos={[
              { label: "Nombre", valor: nombreRecurso, tipo: "text", onChange: setNombreRecurso },
            ]}
          />

          <div className="botones-accion">
            <p className="indice-recurso">
              {recurso?.nombre
                ? `🔧 "${recurso.nombre}" (${recursoActivo + 1} de ${grupo.recursos.length})`
                : `Recurso ${recursoActivo + 1} de ${grupo.recursos.length}`}
            </p>
            <button onClick={eliminarRecurso}>🗑️ Eliminar recurso</button>
            <button onClick={avanzar}>➡️ Siguiente recurso</button>
          </div>
        </div>
      )}

      {/* 🧩 Modal para crear recurso */}
      <Modal isOpen={modalActivo} closeModal={() => setModalActivo(false)} textoBoton="Cancelar">
        <h3>📦 Crear nuevo recurso</h3>
        <input
          type="text"
          placeholder="Nombre del recurso"
          value={nuevoNombreRecurso}
          onChange={(e) => setNuevoNombreRecurso(e.target.value)}
        />
        <div className="botones-modal">
          <button onClick={confirmarCreacionRecurso}>Confirmar</button>
        </div>
      </Modal>
    </div>
  );
}
