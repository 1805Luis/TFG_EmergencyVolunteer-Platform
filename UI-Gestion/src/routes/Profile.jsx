import { useState, useEffect } from "react";
import TarjetaPerfil from "../components/Tarjeta.jsx";
import { useAuth } from "../auth/AuthProvider.jsx";
import ApiService from "../services/ApiService";
import Notificar from "../utils/Notificaciones";
import { useNavigate } from "react-router-dom";
import Modal from "../components/Modal.jsx";
import { FaTrashAlt } from "react-icons/fa"
import "../styles/Profile.css";

export default function Profile(){
  const auth = useAuth();
  const [usuario, setUsuario] = useState(null);
  const [giro, setGiro] = useState({ personales: false, credenciales: false });
  const [mostrarConfirmacion, setMostrarConfirmacion] = useState(false);
  const navigate = useNavigate();

  const [nombre, setNombre] = useState("");
  const [telf, setTelf] = useState("");
  const [habilidades, setHabilidades] = useState("");
  const [disponibilidad, setDisponibilidad] = useState("");
  const [correo, setCorreo] = useState("");
  const [clave, setClave] = useState("");

const cargarUsuario = async () => {
  const token = auth.getAccessToken();
  const result = await ApiService.get("/auth/me", token);
  if (result.success) {
    setUsuario(result.data);
    setNombre(result.data.name);
    setTelf(result.data.telf);
    setHabilidades(result.data.habilidades);
    setDisponibilidad(result.data.disponibilidad);
    setCorreo(result.data.correo);
    setClave(result.data.clave);
  } else {
    Notificar.Error("No se ha podido cargar el perfil.");
  }
};

useEffect(() => {
  cargarUsuario(); 
}, []);


    const prepararPayloadCredenciales = () => {
      const payload = {};

      if (usuario.correo !== correo) payload.correo = correo;
      if (clave.trim() !== "") payload.clave = clave;

      return payload;
    };


    const cambiarCredenciales = async () => {
      if (!usuario) return;

      const payload = prepararPayloadCredenciales();
      const cambios = Object.keys(payload);

      if (cambios.length === 0) {
        Notificar.Info("No hay datos que guardar.");
        return;
      }

      const { isAuthenticated, checkAuth } = auth;
      await checkAuth();
      
      if (!isAuthenticated) {
        Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
        return;
      }
      
      const token = auth.getAccessToken();

      try {
          const result = await ApiService.patch("/auth/cambio-credenciales", payload, token);
          if (result.success) {
          Notificar.Exito("Credenciales actualizadas correctamente.");
          await cargarUsuario();
          setGiro(p => ({ ...p, credenciales: false }));
          setClave("");
          } else {
            Notificar.Error("No se pudo actualizar las credenciales.");
          }
      } catch (error) {
          Notificar.Error("Error al actualizar credenciales.");
          console.error(error);
      }
          
    };

    const prepararPayloadDatosPersonales = () => {
      const payload = {};

      if (usuario.name !== nombre) payload.name = nombre;
      if (usuario.telf !== telf) payload.telf = telf;
      if (usuario.habilidades !== habilidades) payload.habilidades = habilidades;
      if (usuario.disponibilidad !== disponibilidad) payload.disponibilidad = disponibilidad;

      return payload;
    };



    const cambiarDatosPersonales = async () => {
      if (!usuario) return;

      const payload = prepararPayloadDatosPersonales();
      const cambios = Object.keys(payload);

      if (cambios.length === 0) {
        Notificar.Info("No hay datos que guardar.");
        return;
      }

      const { isAuthenticated, checkAuth } = auth;
      await checkAuth();
      
      if (!isAuthenticated) {
        Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
        return;
      }
      
      const token = auth.getAccessToken();

      try {
          const result = await ApiService.patch("/user/modificar-perfil", payload, token);
          if (result.success) {
          Notificar.Exito("Credenciales perfil actualizado con exito.");
          await cargarUsuario();
          setGiro(p => ({ ...p, personales: false }));
          } else {
            Notificar.Error("No se pudo actualizar el perfil.");
          }
      } catch (error) {
          Notificar.Error("Error al actualizar el perfil.");
          console.error(error);
      }
          
    };



    const eliminarDatos = async () => {
      const { isAuthenticated, checkAuth } = auth;
      await checkAuth();
      
      if (!isAuthenticated) {
        Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
        return;
      }

      const token = auth.getAccessToken();

      const result = await ApiService.delete("/auth/delete", token);

      if (result.success) {
          Notificar.Exito("Datos eliminados correctamente.");
          auth.logout(); 
          navigate("/", { replace: true }); 
      } else {
          Notificar.Error("No se pudo eliminar la información.");
      }
    };

  return (
    <div className="panel-perfil">
      <div className="contenedor-tarjetas">
        <TarjetaPerfil
            titulo="Datos personales"
            tipoFondo="bg-personales"
            campos={[
                { label: "Nombre", valor: nombre, onChange: setNombre },
                { label: "Teléfono", valor: telf, onChange: setTelf },
                { label: "Habilidades", valor: habilidades, tipo: "textarea", onChange: setHabilidades },
                { label: "Disponibilidad", valor: disponibilidad, onChange: setDisponibilidad },
            ]}
          girada={giro.personales}
          onGirar={() => setGiro(p => ({ ...p, personales: !p.personales }))}
          onCancelar={() => setGiro(p => ({ ...p, personales: false }))}
          onAceptar={cambiarDatosPersonales}
        />

        <TarjetaPerfil
            titulo="Credenciales de acceso"
            tipoFondo="bg-credenciales"
            campos={[
                { label: "Email", valor: correo, onChange: setCorreo },
                { label: "Clave", valor: clave, tipo: "password", onChange: setClave },
        ]}
          girada={giro.credenciales}
          onGirar={() => setGiro(p => ({ ...p, credenciales: !p.credenciales }))}
          onCancelar={() => setGiro(p => ({ ...p, credenciales: false }))}
          onAceptar={cambiarCredenciales}
        />
      </div>

      <div className="zona-eliminar">
        <button onClick={() => setMostrarConfirmacion(true)}>
            <FaTrashAlt style={{ marginRight: "0.5rem" }} />
            Eliminar todos mis datos
        </button>
        </div>

        <Modal isOpen={mostrarConfirmacion} closeModal={() => setMostrarConfirmacion(false)}>
            <h3>¿Eliminar todos tus datos?</h3>
            <p>Esta acción es irreversible. Se eliminará tu cuenta y se cerrará tu sesión.</p>
            <div className="botones-modal">
                <button className="btn-cancelar" onClick={() => setMostrarConfirmacion(false)}>
                Cancelar
                </button>
                <button className="btn-eliminar" onClick={eliminarDatos}>
                Eliminar definitivamente
                </button>
            </div>
        </Modal>

    </div>
  );
}