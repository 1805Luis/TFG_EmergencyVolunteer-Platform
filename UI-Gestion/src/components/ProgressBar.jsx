import "../styles/ProgressBar.css";

export default function ProgressBar({ step = 1, steps = [] }) {
  if (!steps.length) return null; // Si no hay pasos, no mostrar nada

  return (
    <div className="progress-bar">
      {steps.map((label, i) => {
        const number = i + 1;
        const active = step === number;
        const completed = step > number;

        return (
          <div
            key={label}
            className={`progress-indicator ${completed ? "completed" : ""} ${
              active ? "active" : ""
            }`}
          >
            <span className="progress-label">{label}</span>
          </div>
        );
      })}
    </div>
  );
}
