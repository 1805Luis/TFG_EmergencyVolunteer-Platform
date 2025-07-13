import { useAuth } from "../auth/AuthProvider";
import ListadoConFiltros from "../components/ListadoConFiltros";
import"../styles/VisualTablas.css"
import ApiService from "../services/ApiService";
import Notificar from "../utils/Notificaciones";
import Modal from "../components/Modal.jsx"
import  { useState } from "react";
import { FaUser, FaIdBadge, FaPhoneAlt, FaTools } from "react-icons/fa";
import { MdClose } from "react-icons/md"; 



export default function AdministrarUsuarios() {
  const auth = useAuth();
  const [mostrarModalRol, setMostrarModalRol] = useState(false);
  const [usuarioSeleccionado, setUsuarioSeleccionado] = useState(null);
  const [claveListado, setClaveListado] = useState(0);
  const [datosUsuarioInfo, setDatosUsuarioInfo] = useState(null);
  const [mostrarModalInfo, setMostrarModalInfo] = useState(false);





  const manejarCambioRol = async (usuario) => {
    const { isAuthenticated, checkAuth } = auth;
    await checkAuth();

    if (!isAuthenticated) {
      Notificar.Error("Sesión caducada. Por favor inicia sesión nuevamente.");
      return;
    }

    setUsuarioSeleccionado(usuario);
    setMostrarModalRol(true);
  };

  const cambiarRolUsuario = async (nuevoRol) => {
    const usuarioId = usuarioSeleccionado.id;
    const token = auth.getAccessToken();
    const result = await ApiService.patch("/auth/cambiarRol",{usuarioId,nuevoRol}, token);
    if(result.success)    Notificar.Exito(`Rol de ${usuarioSeleccionado?.email} cambiado a ${nuevoRol}`);
    else Notificar.Error("No se ha podido actualizar");
    setMostrarModalRol(false);
    setUsuarioSeleccionado(null);
    refrescar()
  };

  const refrescar = () => {
    setClaveListado(prev => prev + 1);
  }


  const manejarCambioEstado = async (usuario) => {
    const usuarioId = usuario.id;
    const token = auth.getAccessToken();

    const result = await ApiService.patch(`/auth/estado/${usuarioId}`, null, token);

    if(result.success) Notificar.Exito(`El estado ha cambiado cambiado`);
    else Notificar.Error("No se ha podido actualizar");

    refrescar()
  };

  const mostrarUsuario = async(usuario) =>{
    const usuarioId = usuario.id;
    const token = auth.getAccessToken();

    const result = await ApiService.get(`/user/${usuarioId}`,token, null);
    if(result.success){
          setDatosUsuarioInfo(result.data); 
          setMostrarModalInfo(true); 
    }
    else Notificar.Error("No se ha podido actualizar");
  }


  const columnas = ["id", "email", "rol", "estado", "Modificar"];

  const renderCell = (row, column) => {
    if (column === "id") {
      return (
        <span className="id-interactivo" onClick={() => mostrarUsuario(row)}>
          {row.id}
        </span>
      );
    }
    if (column === "Modificar") {
      return (
        <div className="modificar-buttons">
          <button className="boton-estado" onClick={() => manejarCambioRol(row)}>
            Rol
          </button>
          <button className="boton-estado" onClick={() => manejarCambioEstado(row)}>
            Estado
          </button>
        </div>
      );
    }
    return row[column];
  };


  const filtrosBasicos = [
    { nombre: "email", tipo: "text", etiqueta: "Filtrar por email",valorPorDefecto: ""},
    { nombre: "rol", tipo: "select", etiqueta: "Rol", opciones: ["Voluntario", "Security_Ops"],valorPorDefecto: "" },
    { nombre: "estado", tipo: "select", etiqueta: "Estado", opciones: ["activo", "suspendido"],valorPorDefecto: "" },
    { nombre: "ordenEstado", tipo: "select", etiqueta: "Orden Estado", opciones: ["asc", "desc"],valorPorDefecto: "desc" },
  ];

  const filtrosAvanzados = [
    { nombre: "id", tipo: "text", etiqueta: "ID del usuario" },
  ];

  const configFiltros = [...filtrosBasicos, ...filtrosAvanzados];

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

    const result = await ApiService.get("/auth/usuarios-simples", token, queryParams);

    return {
      datos: result.data,
      paginacion: result.pagination,
    };
  };

  return (
  <>
    {mostrarModalRol && (
  <Modal
    isOpen={mostrarModalRol}
    closeModal={() => {
      setMostrarModalRol(false);
      setUsuarioSeleccionado(null);
    }}>
        <div className="modal-contenido-blur tarjeta-cambio-rol">
          <div className="encabezado-usuario">
            <FaTools size={28} color="#007bff" />
            <h2>Cambio de Rol</h2>
          </div>

          <p><strong>ID del usuario:</strong> {usuarioSeleccionado?.id}</p>

          <div className="boton-rol-selector">
            <button className="boton-rol-modal" onClick={() => cambiarRolUsuario("Voluntario")}>
              🧑 Voluntario
            </button>
            <button className="boton-rol-modal" onClick={() => cambiarRolUsuario("Security_Ops")}>
              🛡️ Security_Ops
            </button>
            <button
              className="boton-cancelar"
              onClick={() => {
                setMostrarModalRol(false);
                setUsuarioSeleccionado(null);
              }}
            >
              <MdClose style={{ verticalAlign: "middle", marginRight: "0.4rem" }} />
              Cancelar
            </button>
          </div>
        </div>
      </Modal>
    )}

    {mostrarModalInfo && (
      <Modal
        isOpen={mostrarModalInfo}
        closeModal={() => {
          setMostrarModalInfo(false);
          setDatosUsuarioInfo(null);
        }}
>
        <div className="modal-contenido-blur tarjeta-usuario">
          <h2><FaUser style={{ marginRight: "0.5rem" }} /> Perfil del usuario</h2>

          <div className="grupo-datos">
            <p><FaUser className="icono" /><strong>Nombre:</strong> {datosUsuarioInfo?.name}</p>
            <p><FaIdBadge className="icono" /><strong>ID:</strong> {datosUsuarioInfo?.id}</p>
            <p><FaPhoneAlt className="icono" /><strong>Teléfono:</strong> {datosUsuarioInfo?.telf}</p>
            <p><FaTools className="icono" /><strong>Habilidades:</strong> {datosUsuarioInfo?.habilidades}</p>
          </div>

          <button className="boton-cancelar" onClick={() => {
            setMostrarModalInfo(false);
            setDatosUsuarioInfo(null);
          }}>
            <MdClose style={{ verticalAlign: "middle", marginRight: "0.4rem" }} />
            Cerrar
          </button>
        </div>
      </Modal>
    )}


    <div className="listado-glass">
      <ListadoConFiltros
        key={claveListado}
        titulo="Administrar usuarios"
        columnas={columnas}
        renderCell={renderCell}
        configFiltros={configFiltros}
        fetchDatos={fetchDatos}
      />
    </div>

  </>);
}
