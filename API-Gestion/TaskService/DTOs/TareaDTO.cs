namespace TaskService.DTOs
{
    public class TareaDTO
    {
        public Guid? Id { get; set; }
        public string? Nombre { get; set; } = string.Empty;
        public string? Ubicacion { get; set; } = string.Empty;
        public string? Descripcion { get; set; } = string.Empty;
        public string? gravedad { get; set; } = string.Empty;
        public string? estado { get; set; } = string.Empty;
        public Guid? emergencia_id { get; set; }
        public string? HabilidadesMin { get; set; }
    }
}
