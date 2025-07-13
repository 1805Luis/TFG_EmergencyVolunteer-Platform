// handleApiResponse.js
export function handleApiResponse(response, data) {
  if (response.ok && data.success) {
    return { success: true, data: data.data, pagination: data.pagination };
  } else {
    return { success: false, errors: data.errors  || [{ errors: data.Message || "Error desconocido" }] };
  }
}
