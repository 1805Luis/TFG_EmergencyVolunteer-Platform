import Modal from "./Modal";

export default function LegalInfoModal({ tipo, isOpen, onClose }) {
    const correoContacto = "admin@voluntarios.com";
    const responsable = "Administrador ";
  return (
    <Modal isOpen={isOpen} closeModal={onClose} textoBoton="Cerrar">
      {tipo === "politica" && (
        <div>
          <h2>Política de Privacidad</h2>
          <h3>Última actualización: 03/07/2025</h3>

          <p><strong>En la plataforma de gestión de voluntarios para situaciones de emergencia</strong> (en adelante, "la Plataforma"), nos tomamos muy en serio la protección de tu privacidad y tus datos personales.</p>

          <h4>1. Responsable del tratamiento</h4>
          <p>
            Responsable: {responsable}<br />
            Correo de contacto: {correoContacto}
          </p>

          <h4>2. Datos personales que recopilamos</h4>
          <ul>
            <li>Nombre y apellidos</li>
            <li>Correo electrónico</li>
            <li>Número de teléfono</li>
            <li>Disponibilidad</li>
            <li>Habilidades</li>
            <li>Estado y rol dentro de la plataforma</li>
            <li>Datos de acceso</li>
          </ul>

          <h4>3. Finalidades del tratamiento</h4>
          <ul>
            <li>Gestionar tu registro</li>
            <li>Facilitar tu participación</li>
            <li>Comunicaciones operativas</li>
            <li>Mejoras de la plataforma</li>
          </ul>

          <h4>4. Legitimación</h4>
          <ul>
            <li>Intereses vitales</li>
            <li>Obligación legal</li>
            <li>Consentimiento</li>
            <li>Interés legítimo</li>
          </ul>

          <h4>5. Conservación</h4>
          <p>Mantendremos tus datos mientras seas usuario/a o hasta que pidas su eliminación.</p>

          <h4>6. Cesión de datos</h4>
          <ul>
            <li>Protección Civil, Cruz Roja, etc.</li>
            <li>Cuando haya obligación legal</li>
          </ul>

          <h4>7. Derechos</h4>
          <ul>
            <li>Acceso, rectificación, supresión, limitación, oposición, portabilidad</li>
          </ul>

          <p>Solicita estos derechos escribiendo a {correoContacto}.</p>

          <h4>8. Seguridad</h4>
          <ul>
            <li>Hash seguro para contraseñas</li>
            <li>Control de acceso por roles</li>
          </ul>

          <h4>9. Cambios</h4>
          <p>Te notificaremos si esta política cambia.</p>
        </div>
      )}

      {tipo === "terminos" && (
        <div>
          <h1>Términos y Condiciones de Servicio</h1>
          <h3>Última actualización: 03/07/2025</h3>

          <p><strong>Bienvenido/a a nuestra plataforma de gestión de voluntarios para situaciones de emergencia.</strong></p>

          <h4>1. Objeto</h4>
          <p>Facilitar organización y respuesta en emergencias.</p>

          <h4>2. Registro</h4>
          <p>Información veraz, credenciales seguras.</p>

          <h4>3. Uso de la Plataforma</h4>
          <p>Solo para fines de voluntariado. No usos ilegales.</p>

          <h4>4. Datos personales</h4>
          <p>Se tratan según RGPD y Política de Privacidad.</p>

          <h4>5. Propiedad Intelectual</h4>
          <p>Todo el contenido es propiedad de la Plataforma.</p>

          <h4>6. Responsabilidad</h4>
          <p>No garantizamos disponibilidad ni nos responsabilizamos por acciones de los usuarios.</p>

          <h4>7. Modificaciones</h4>
          <p>Se notificarán cambios en los Términos.</p>

          <h4>8. Legislación aplicable</h4>
          <p>Leyes de España, juzgados de Madrid.</p>
        </div>
      )}
    </Modal>
  );
}
