namespace AssigmentService.DTOs
{
    public class TareaDTO
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; }
        public string Ubicacion { get; set; }
        public string Descripcion { get; set; }
        public Guid EmergenciaId { get; set; }
        public string estado { get; set; }   
        public string gravedad { get; set; } // Como string para facilitar lectura en frontend
        public string HabilidadesMin { get; set; }
    }
}
