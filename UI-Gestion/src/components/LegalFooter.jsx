import { useState } from "react";
import LegalInfoModal from "./LegalInfoModal";
import "../styles/LegalFooter.css";

export default function LegalFooter() {
  const [tipoModal, setTipoModal] = useState(null);

  return (
    <footer className="legal-footer">
      <div className="footer-links">
        <button onClick={() => setTipoModal("terminos")}>Términos y Condiciones</button>
        <button onClick={() => setTipoModal("politica")}>Política de Privacidad</button>
      </div>

      <div className="footer-info">
        <span>Información legal y corporativa</span>
        <span>© 2025 TuEmpresa</span>
        <span>Versión: v2.3.7</span>
        <span>Aviso legal: Esta plataforma gestiona datos sensibles conforme al RGPD.</span>
      </div>

      <LegalInfoModal
        tipo={tipoModal}
        isOpen={tipoModal !== null}
        onClose={() => setTipoModal(null)}
      />
    </footer>
  );
}
