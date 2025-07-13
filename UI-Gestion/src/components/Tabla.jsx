import React, { useState, useEffect } from "react";
import "../styles/Tabla.css";

export default function Tabla({ rows = [], columns = [], rowsPerPage = 10, filterText = "", renderCell }) {
  const [currentPage, setCurrentPage] = useState(1);

  // Filtrar filas por texto
  const filteredRows = Array.isArray(rows)
    ? rows.filter((row) =>
        Object.values(row).some((value) =>
          String(value).toLowerCase().includes(filterText.toLowerCase())
        )
      )
    : [];

  // Calcular paginación
  const totalPages = Math.ceil(filteredRows.length / rowsPerPage);
  const paginatedRows = filteredRows.slice(
    (currentPage - 1) * rowsPerPage,
    currentPage * rowsPerPage
  );

  const handlePageClick = (pageNum) => {
    setCurrentPage(pageNum);
  };

  useEffect(() => {
    setCurrentPage(1); // Resetear página al cambiar filtro
  }, [filterText]);

  return (
    <>
      <table>
        <thead>
          <tr>
            {columns.map((col) => (
              <th key={col}>{col}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {paginatedRows.map((row) => (
            <tr key={row.id}>
              {columns.map((col) => (
                <td key={col}>
                  {renderCell ? renderCell(row, col) : row[col]}
                </td>
              ))}
            </tr>
          ))}
          {paginatedRows.length === 0 && (
            <tr>
              <td colSpan={columns.length} style={{ textAlign: "center", padding: "1rem" }}>
                No hay datos que mostrar
              </td>
            </tr>
          )}
        </tbody>
      </table>

      {/* Paginador */}
      {totalPages > 1 && (
        <div className="pagination">
          {Array.from({ length: totalPages }, (_, i) => i + 1).map((pageNum) => (
            <button
              key={pageNum}
              onClick={() => handlePageClick(pageNum)}
              className={pageNum === currentPage ? "active" : ""}
              aria-label={`Ir a la página ${pageNum}`}
            >
              {pageNum}
            </button>
          ))}
        </div>
      )}
    </>
  );
}
