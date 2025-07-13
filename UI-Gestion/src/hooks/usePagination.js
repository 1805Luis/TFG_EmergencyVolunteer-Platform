import { useState } from "react";

export function usePagination(initialPage = 1, initialSize = 10) {
  const [pagination, setPagination] = useState({
    page: initialPage,
    pageSize: initialSize,
    totalItems: 0,
    totalPages: 0,
  });

  const actualizar = (data) => {
    setPagination(prev => ({
      ...prev,
      page: data.page,
      totalItems: data.totalItems,
      totalPages: data.totalPages,
    }));
  };

  return { pagination, setPagination, actualizar };
}
