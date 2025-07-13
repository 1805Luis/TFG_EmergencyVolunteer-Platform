import { useState, useEffect } from "react";
import Modal from "./Modal";
import Tabla from "./Tabla";
import Paginador from "./Paginador";
import FiltrosGenericos from "./FiltrosGenericos";
import { usePagination } from "../hooks/usePagination";
import { CiFilter } from "react-icons/ci";
import "../styles/ListadoConFiltros.css";

export default function ListadoConFiltros({
  titulo = "Listado",
  columnas,
  renderCell,
  fetchDatos,
  configFiltros
}) {
  const valoresIniciales = {};
    configFiltros.forEach(f => {
      valoresIniciales[f.nombre] = f.valorPorDefecto ?? "";
  });

  const [valoresFiltros, setValoresFiltros] = useState(valoresIniciales);
  const { pagination, setPagination, actualizar } = usePagination();
  const [mostrarModal, setMostrarModal] = useState(false);
  const [datos, setDatos] = useState([]);

  const filtrosActivos = Object.values(valoresFiltros).filter(v => v).length;

  useEffect(() => {
    fetchDatos(valoresFiltros, pagination).then(res => {
      setDatos(res.datos);
      
      actualizar(res.paginacion);
    });
  }, [valoresFiltros, pagination.page, pagination.pageSize]);

  return (
    <div className="listado-container">
      <div className="encabezado">
        <h1>{titulo}</h1>
      </div>

      <Modal isOpen={mostrarModal} closeModal={() => setMostrarModal(false)} textoBoton="Cerrar">
        <h2>Filtros</h2>
        <FiltrosGenericos
          config={configFiltros}
          valoresIniciales={valoresFiltros}
          onCambio={(campo, valor) => setValoresFiltros(prev => ({ ...prev, [campo]: valor }))}
          onAplicar={(f) => {
            setValoresFiltros(f);
            setPagination(p => ({ ...p, page: 1 }));
            setMostrarModal(false);
          }}
        />
      </Modal>

        <div className="layout-superior">
            <div className="paginador-y-filtro">
                <Paginador
                page={pagination.page}
                pageSize={pagination.pageSize}
                totalPages={pagination.totalPages}
                onPageChange={num => setPagination(p => ({ ...p, page: num }))}
                onPageSizeChange={size => setPagination(p => ({ ...p, pageSize: size }))}
                />

                <div className="icono-filtros-wrapper" onClick={() => setMostrarModal(true)}>
                {filtrosActivos > 0 && <div className="contador-filtros">{filtrosActivos}</div>}
                <div className="icono-fondo">
                    <CiFilter className="icono-filtros" size={22} />
                </div>
                </div>
            </div>
        </div>


      <Tabla rows={datos} columns={columnas} renderCell={renderCell} />
       <div className="paginador-numerico">
            {Array.from({ length: pagination.totalPages }, (_, i) => i + 1).map((num) => (
                <button
                key={num}
                onClick={() => setPagination(p => ({ ...p, page: num }))}
                disabled={num === pagination.page}
                >
                {num}
                </button>
            ))}
        </div>
      <div className="tabla-resumen">
          <span>
            Mostrando elementos {(pagination.page - 1) * pagination.pageSize + 1}–
            {Math.min(pagination.page * pagination.pageSize, pagination.totalItems)} de {pagination.totalItems}
          </span>
      </div>

    </div>
  );
}
