import "../styles/Tarjeta.css";

export default function Tarjeta({
  titulo,
  campos = [],
  girada,
  onGirar,
  onAceptar,
  onCancelar,
  tipoFondo
}) {
  return (
    <div
      className={`tarjeta ${girada ? "girada" : ""}`}
      onClick={(e) => {
        const tag = e.target.tagName;
        if (tag !== "INPUT" && tag !== "TEXTAREA" && tag !== "BUTTON") {
          onGirar();
        }
      }}
    >
      <div className={`tarjeta-front fondo-con-imagen ${tipoFondo}`}>
        <div className="glass-titulo">
          <h3>{titulo}</h3>
        </div>
      </div>

      <div className="tarjeta-back">
        <div className="contenido-campos">
          {campos.map(({ label, valor, tipo = "text", onChange }, i) => (
            <div className="campo-editable" key={i}>
              <label><strong>{label}:</strong></label>

              {onChange ? (
                tipo === "textarea" ? (
                  <textarea
                    value={valor || ""}
                    onChange={(e) => onChange(e.target.value)}
                  />
                ) : (
                  <input
                    type={tipo}
                    value={valor || ""}
                    onChange={(e) => onChange(e.target.value)}
                  />
                )
              ) : (
                <p className="campo-no-editable">{valor || "—"}</p>
              )}
            </div>
          ))}
        </div>

        <div className="botones-accion">
          <button
            onClick={(e) => {
              e.stopPropagation();
              onCancelar();
            }}
          >
            Cancelar
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              onAceptar();
            }}
          >
            Aceptar
          </button>
        </div>
      </div>
    </div>
  );
}
