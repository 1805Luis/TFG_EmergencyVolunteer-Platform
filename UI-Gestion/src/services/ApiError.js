// ApiError.js
export default class ApiError extends Error {
  constructor(message, errors = [], statusCode = null) {
    super(message);
    this.name = "ApiError";
    this.errors = errors;       
    this.statusCode = statusCode; 
  }
}
