import { useState } from "react";
import Step from "./Step";
import ProgressBar from "./ProgressBar";
import "../styles/MultiStepForm.css";
import ApiService from "../services/ApiService";
import ApiError from "../services/ApiError";
import Notificar from "../utils/Notificaciones";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import LegalInfoModal from "./LegalInfoModal";

const stepsLabels = ["Cuenta", "Información personal", "Perfil", "Términos"];

const stepsConfig = {
  1: [
    { label: "Correo electrónico", name: "correo", type: "text", required: true },
    { label: "Clave", name: "clave", type: "password", required: true },
  ],
  2: [
    { label: "Nombre completo", name: "nombre", required: true },
    { label: "Teléfono", name: "telf", type: "text", required: false },
  ],
  3: [
    {
      label: "Disponibilidad",
      type: "text",
      name: "disponibilidad",
      required: false,
      placeholder: "Ej: Lunes y Miércoles de 4pm a 7pm",
    },
    {
      label: "Habilidades",
      name: "habilidades",
      type: "text",
      required: false,
      placeholder: "Ej: Logistica, Sanidad, Seguridad, Coordinacion, etc",
    },
  ],
  4: [
    {
      label: "Acepto la Política de Privacidad",
      name: "aceptaPolitica",
      type: "checkbox",
      required: true,
    },
    {
      label: "Acepto los Términos y Condiciones de Servicio",
      name: "aceptaTerminos",
      type: "checkbox",
      required: true,
    },
  ],
};

const initialData = {
  correo: "",
  clave: "",
  nombre: "",
  telf: "",
  disponibilidad: "",
  habilidades: "",
  aceptaPolitica: false,
  aceptaTerminos: false,
};

export default function MultiStepForm() {
  const [currentStep, setCurrentStep] = useState(1);
  const [formData, setFormData] = useState(initialData);
  const [showModal, setShowModal] = useState(null); // null, "politica", "terminos"
  const [errors, setErrors] = useState({});

  const auth = useAuth();
  const gotTo = useNavigate();

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleBlur = (e) => {
    const { name, value, type, checked } = e.target;
    // Para checkbox obligatorios, validamos si está marcado o no
    const val = type === "checkbox" ? checked : value;
    const error = validateField(name, val);

    setErrors((prev) => ({
      ...prev,
      [name]: error,
    }));
  };

  const next = () =>
    setCurrentStep((prev) => Math.min(prev + 1, stepsLabels.length));
  const prev = () => setCurrentStep((prev) => Math.max(prev - 1, 1));

  function validateField(name, value) {
    let error = "";

    if (name === "correo") {
      const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!value) {
        error = "Correo es obligatorio";
      } else if (!emailPattern.test(value)) {
        error = "Correo inválido";
      }
    } else if ((name === "telf")&& value) {
       if (!/^[\d\s-]+$/.test(value)) {
        error = "Teléfono solo puede tener números, espacios o guiones";
      }
    } else if ((name === "clave" || name === "nombre") && !value)
    {
      error = "Este campo es obligatorio";
    } else if ((name === "aceptaPolitica" || name === "aceptaTerminos") && !value ) 
    {
      error = "Debes aceptar este campo";
    } 

    return error;
  }

  async function handleRegister(e) {
    e.preventDefault();

    // Validar todos los campos antes de enviar
    const newErrors = {};
    for (const fieldName of Object.keys(formData)) {
      const error = validateField(fieldName, formData[fieldName]);
      if (error) {
        newErrors[fieldName] = error;
      }
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      Notificar.Advertencia("Por favor, corrige los errores antes de enviar.");
      return;
    }

    if (!formData.aceptaPolitica || !formData.aceptaTerminos) {
      alert("Debes aceptar la Política de Privacidad y los Términos y Condiciones");
      return;
    }

    try {
      const { correo, clave, nombre, telf, disponibilidad, habilidades } = formData;

      const result = await ApiService.post("/auth/register", {
        correo,
        clave,
        nombre,
        telf,
        disponibilidad,
        habilidades,
      });

      if (result.data.token && result.data.refreshToken) {
        auth.saveUser(result.data);
        Notificar.Exito("Registro exitoso");
        gotTo("/dashboard");
      }
    } catch (error) {
      if (error instanceof ApiError) {
        Notificar.Error("Error API: " + error.message);
      } else {
        Notificar.Error("Error API: " + error.message);
      }
    }
  }

  // Para habilitar botón enviar: debe aceptar términos y no haber errores en campos obligatorios visibles (opcional)
  const isFormValid =
    formData.aceptaPolitica &&
    formData.aceptaTerminos &&
    Object.values(errors).every((err) => !err);

  return (
    <div className="form-container">
      <ProgressBar step={currentStep} steps={stepsLabels} />

      <form onSubmit={handleRegister} noValidate>
        <h2 className="step-title">{stepsLabels[currentStep - 1]}</h2>

        {/* Avisos con enlaces al modal solo en paso 4 */}
        {currentStep === 4 && (
          <>
            <p className="privacy-text">
              Puedes leer nuestra{" "}
              <span className="modal-link" onClick={() => setShowModal("politica")}>
                Política de Privacidad
              </span>
              .
            </p>
            <p className="privacy-text">
              Puedes leer nuestros{" "}
              <span className="modal-link" onClick={() => setShowModal("terminos")}>
                Términos y Condiciones de Servicio
              </span>
              .
            </p>
          </>
        )}

        <Step
          fields={stepsConfig[currentStep]}
          data={formData}
          onChange={handleChange}
          onBlur={handleBlur}
          errors={errors}
        />

        <div className="button-group">
          {currentStep > 1 && (
            <button type="button" className="btn-prev" onClick={prev}>
              Anterior
            </button>
          )}

          {currentStep < stepsLabels.length ? (
            <button type="button" className="btn-next" onClick={next}>
              Siguiente
            </button>
          ) : (
            <button
              type="submit"
              className={`btn-submit ${isFormValid ? "enabled" : "disabled"}`}
              disabled={!isFormValid}
            >
              Enviar
            </button>
          )}
        </div>
      </form>
      <LegalInfoModal
        tipo={showModal}
        isOpen={showModal !== null}
        onClose={() => setShowModal(null)}
      />
    </div>
  );
}
