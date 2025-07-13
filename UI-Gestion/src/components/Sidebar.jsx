import "../styles/Sidebar.css";
import {
  AiOutlineClose,
  AiOutlineMenu,
  AiOutlineHome,
  AiOutlineUser,
  AiOutlineFileText,
  AiOutlineDown
} from "react-icons/ai";
import { GrGroup } from "react-icons/gr";
import { CiLogout } from "react-icons/ci";
import { MdOutlineSecurity } from "react-icons/md";
import { FaUserEdit } from "react-icons/fa";

import { NavLink, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import { useState, useEffect } from "react";
import Notificar from "../utils/Notificaciones";



export default function Sidebar({ isExpanded, onToggle }) {
  const auth = useAuth();
  const navigate = useNavigate();
  const { getRol, logout } = auth;
  const rol = getRol();

  const [showSecurityMenu, setShowSecurityMenu] = useState(false);
  const location = useLocation();

  const isSecuritySubroute = securityLinks.some(link => link.to === location.pathname);

  const toggleSecurityMenu = () => setShowSecurityMenu(prev => !prev);

  useEffect(() => {
    setShowSecurityMenu(false); // cerrar submenú al navegar
  }, [location.pathname]);

  useEffect(() => {
    if (!isExpanded) setShowSecurityMenu(false); // cerrar submenú al colapsar
  }, [isExpanded]);

  const handleLogout = () => {
    logout();
    navigate("/", { replace: true }); 
    Notificar.Exito("Sesion cerrada");
  };

  return (
    <div className={`sidebar ${isExpanded ? "expanded" : "collapsed"}`}>
      <button className="Sidebarbutton" onClick={onToggle}>
        {isExpanded ? <AiOutlineClose size={20} /> : <AiOutlineMenu size={20} />}
        {isExpanded && <span className="btn-text">Cerrar</span>}
      </button>

      {linksArray.map(({ icon, label, to }) => (
        <NavLink
          to={to}
          key={label}
          className={({ isActive }) => `Links ${isActive ? "active" : ""}`}
          title={!isExpanded ? label : ""}
        >
          <div className={`LinkIcon ${!isExpanded ? "icon-collapsed" : ""}`}>
            {icon}
          </div>
          {isExpanded && <span className="LinkText">{label}</span>}
        </NavLink>
      ))}

      {rol === "Security_Ops" && (
        <>
          <div
            className={`Links ${showSecurityMenu || isSecuritySubroute ? "active" : ""}`}
            onClick={toggleSecurityMenu}
            title={!isExpanded ? "Seguridad" : ""}
            style={{ cursor: "pointer" }}
          >
            <div className={`LinkIcon ${!isExpanded ? "icon-collapsed" : ""}`}>
              <MdOutlineSecurity />
              {!isExpanded && (
                <AiOutlineDown
                  className={`arrow ${showSecurityMenu ? "rotate" : ""}`}
                  size={12}
                />
              )}
            </div>
            {isExpanded && (
              <span className="LinkTextWithArrow">
                Seguridad
                <AiOutlineDown
                  className={`arrow ${showSecurityMenu ? "rotate" : ""}`}
                  size={12}
                />
              </span>
            )}
          </div>

          <div className={`submenu ${showSecurityMenu ? "open" : ""}`}>
            {securityLinks.map(({ icon, label, to }) => (
              <NavLink
                to={to}
                key={label}
                className={({ isActive }) => `Links sublink ${isActive ? "active" : ""}`}
                title={!isExpanded ? label : ""}
              >
                <div className={`LinkIcon ${!isExpanded ? "icon-collapsed" : ""}`}>
                  {icon}
                </div>
                {isExpanded && <span className="LinkText">{label}</span>}
              </NavLink>
            ))}
          </div>
        </>
      )}

      {/* Botón salir al fondo */}
      <NavLink
        to="/"
        className="Links salir"
        title={!isExpanded ? "Salir" : ""}
        onClick={handleLogout}
      >
        <div className={`LinkIcon ${!isExpanded ? "icon-collapsed" : ""}`}>
          <CiLogout />
        </div>
        {isExpanded && <span className="LinkText">Salir</span>}
      </NavLink>
    </div>
  );
}

// Enlaces generales
const linksArray = [
  { label: "Home", icon: <AiOutlineHome />, to: "/dashboard" },
  { label: "Perfil", icon: <AiOutlineUser />, to: "/perfil" },
  { label: "Mis asignaciones", icon: <GrGroup />, to: "/asignaciones" }
];

// Submenú para rol Security_Ops
const securityLinks = [
  { label: "Administrar usuarios", icon: <FaUserEdit/> , to: "/seguridad/usuarios" },
  { label: "Ver registros", icon: <AiOutlineFileText />, to: "/seguridad/registros" }
];
