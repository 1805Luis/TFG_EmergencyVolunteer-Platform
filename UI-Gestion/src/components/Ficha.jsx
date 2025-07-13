import "../styles/Ficha.css"

export default function Ficha({ children, gravedad, onClick}) {
  return (
    <div
      className={`ficha ${gravedad ? `gravedad-${gravedad}` : ""}`}
      onClick={onClick} // ← Aseguramos que el click funcione
    >
      {children}
    </div>
  )
}
