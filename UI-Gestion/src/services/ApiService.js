import { handleApiResponse } from "./handleApiResponse";
import ApiError from "./ApiError";


const API_URL = import.meta.env.VITE_API_URL;

async function request(method, path, body = null, token  = null, queryParams = null) {

  try {
    // 1. Construir cabecera
    const headers = {
      "Content-Type": "application/json",
    };

    if (token) {
      headers["Authorization"] = `Bearer ${token}`;
    }

    // 2. Preparar configuración base
    const options = {
      method,
      headers,
    };

    // 3. Añadir body solo si el método lo admite
    if (["POST", "PUT", "PATCH"].includes(method) && body) {
      options.body = JSON.stringify(body);
    }

    // 4. Construir URL final
    let url = `${API_URL}${path}`;
    if (queryParams) {
      const queryString = new URLSearchParams(queryParams).toString();
      url += `?${queryString}`;
    }


    // 5. Hacer la petición
    const response = await fetch(url, options);

    const data = await response.json();

    const result = handleApiResponse(response, data);

    if (!result.success) {
      const mensajesErrores = Array.isArray(result.errors)
        ? result.errors.map(e => e.message).join(", ")
        : typeof result.errors === "string"
          ? result.errors
          : "Error desconocido";

      throw new ApiError(
        mensajesErrores,
        result.errors,
        response.status
      );
    }

    console.log(result)
    return result;

  } catch (error) {
    console.error("Error en API:", error);
    throw error;
  }
}

export default {
  get: (path, token = null, queryParams = null) => request("GET", path, null, token, queryParams),
  post: (path, body, token = null, queryParams = null) => request("POST", path, body, token, null),
  patch: (path, body, token = null, queryParams = null) => request("PATCH", path, body, token, null),
  delete: (path, token = null, queryParams = null) => request("DELETE", path, null, token, null),
};

