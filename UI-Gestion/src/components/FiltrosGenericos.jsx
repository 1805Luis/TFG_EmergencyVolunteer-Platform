import "../styles/FiltrosGenericos.css";
import { useState } from "react";

export default function FiltrosGenericos({ config = [], valoresIniciales = {}, onCambio, onAplicar }) {
  const [valoresLocales, setValoresLocales] = useState(valoresIniciales);

  const actualizarCampo = (nombre, valor) => {
    const nuevosValores = { ...valoresLocales, [nombre]: valor };
    setValoresLocales(nuevosValores);
    onCambio && onCambio(nombre, valor);
  };

  return (
    <form className="filtros-genericos" onSubmit={(e) => {
      e.preventDefault();
      onAplicar && onAplicar(valoresLocales);
    }}>
      {config.map(({ nombre, tipo = "text", etiqueta, opciones = [], min, max }) => {
        const valorActual = valoresLocales[nombre] || "";

        return (
          <div className="campo-filtro" key={nombre}>
            <label htmlFor={nombre}>{etiqueta || nombre}</label>
            {tipo === "select" ? (
              <select
                id={nombre}
                value={valorActual}
                onChange={(e) => actualizarCampo(nombre, e.target.value)}
              >
                <option value="">--</option>
                {opciones.map(op => (
                  <option key={op} value={op}>{op}</option>
                ))}
              </select>
            ) : (
              <input
                id={nombre}
                type={tipo}
                value={valorActual}
                onChange={(e) => actualizarCampo(nombre, e.target.value)}
                min={min}
                max={max}
              />
            )}
          </div>
        );
      })}
    </form>
  );
}
