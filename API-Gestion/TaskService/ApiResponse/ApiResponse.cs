namespace TaskService.ApiResponse
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public List<ApiError>? Errors { get; set; }
        public PaginationMetadata? Pagination { get; set; }

        public static ApiResponse<T> Ok(T data, PaginationMetadata? pagination = null) => new()
        {
            Success = true,
            Data = data,
            Pagination = pagination
        };

        public static ApiResponse<T> Fail(string code, string message) => new()
        {
            Success = false,
            Errors = new List<ApiError> { new(code, message) }
        };

    }
}
