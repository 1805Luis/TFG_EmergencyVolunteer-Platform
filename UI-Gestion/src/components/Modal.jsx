import "../styles/Modal.css";

export default function Modal({ isOpen, closeModal, children, textoBoton }) {
  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={closeModal}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        {children}
        {textoBoton && (
          <button className="modal-close-button" onClick={closeModal}>
            {textoBoton}
          </button>
        )}
      </div>
    </div>
  );
}
