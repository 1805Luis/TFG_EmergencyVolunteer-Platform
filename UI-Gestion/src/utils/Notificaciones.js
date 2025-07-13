// src/utils/Notificaciones.js
import { toast } from "react-toastify";

function Exito(mensaje) {
  toast.success(mensaje);
}

function Error(mensaje) {
  toast.error(mensaje);
}

function Info(mensaje) {
  toast.info(mensaje);
}

function Advertencia(mensaje) {
  toast.warn(mensaje);
}

export default {
  Exito,
  Error,
  Info,
  Advertencia
};