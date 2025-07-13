import "../styles/Step.css";

export default function Step({ fields, data, onChange, onBlur, errors }) {
  return (
    <>
      {fields.map(
        ({
          label,
          name,
          type = "text",
          placeholder = "",
          required = false,
          options = [],
          className = "",
        }) => {
          const key = `field-${name}`;

          // Checkbox múltiple con opciones
          if (type === "checkbox" && Array.isArray(options) && options.length > 0) {
            return (
              <div className={`form-group ${className}`} key={key}>
                <label>{label}</label>
                <div className="checkbox-group">
                  {options.map((option) => (
                    <label key={option} className="checkbox-option">
                      <input
                        type="checkbox"
                        name={name}
                        value={option}
                        checked={data[name]?.includes(option)}
                        onChange={(e) => {
                          const isChecked = e.target.checked;
                          const value = e.target.value;
                          const current = data[name] || [];

                          const updated = isChecked
                            ? [...current, value]
                            : current.filter((v) => v !== value);

                          onChange({ target: { name, value: updated } });
                        }}
                      />
                      {option}
                    </label>
                  ))}
                </div>
              </div>
            );
          }

          // Checkbox único (booleano) usando checkbox-option también
          if (type === "checkbox") {
            return (
              <div className={`form-group ${className}`} key={key}>
                <label className="checkbox-option" htmlFor={name}>
                  <input
                    type="checkbox"
                    id={name}
                    name={name}
                    checked={!!data[name]}
                    onChange={onChange}
                    onBlur={onBlur}
                    required={required}
                  />
                  {label}
                </label>
                {errors && errors[name] && (
                  <p className="error-text">{errors[name]}</p>
                )}
              </div>
            );
          }

          // Otros tipos de inputs (text, email, etc.)
          return (
            <div className={`form-group ${className}`} key={key}>
              <label htmlFor={name}>{label}</label>
              <input
                type={type}
                id={name}
                name={name}
                value={data[name] || ""}
                onChange={onChange}
                onBlur={onBlur}
                placeholder={placeholder}
                required={required}
                className="input-field"
              />
              {errors && errors[name] && (
                <p className="error-text">{errors[name]}</p>
              )}
            </div>
          );
        }
      )}
    </>
  );
}
