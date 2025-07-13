namespace BitacoraService.ApiResponse
{
    public class ApiError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public ApiError(string code, string message) { Code = code; Message = message; }
    }
}
