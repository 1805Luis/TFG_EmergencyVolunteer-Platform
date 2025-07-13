import "../styles/Paginador.css";

export default function Paginador({
  page,
  pageSize,
  totalPages,
  onPageChange,
  onPageSizeChange,
  minSize = 1,
  maxSize = 50,
}) {
  const handlePageInput = (e) => {
    let val = Number(e.target.value);
    if (val < 1) val = 1;
    else if (val > totalPages) val = totalPages;
    onPageChange(val);
  };

  const handlePageSizeInput = (e) => {
    let val = Number(e.target.value);
    if (val < minSize) val = minSize;
    else if (val > maxSize) val = maxSize;
    onPageSizeChange(val);
  };

  return (
    <div className="paginador">
      <div className="campo">
        <label htmlFor="pagina">Página</label>
        <input
          id="pagina"
          type="number"
          min="1"
          max={totalPages || 1}
          value={page}
          onChange={handlePageInput}
        />
      </div>

      <div className="campo">
        <label htmlFor="tamano">Tamaño página</label>
        <input
          id="tamano"
          type="number"
          min={minSize}
          max={maxSize}
          value={pageSize}
          onChange={handlePageSizeInput}
        />
      </div>
    </div>
  );
}
