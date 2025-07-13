namespace EmergencyService.DTOs
{
    public class TareaRabbitDTO
    {
        public string Nombre { get; set; }
        public string Ubicacion { get; set; }
        public string Descripcion { get; set; }
        public string gravedad { get; set; }
        public string HabilidadesMin { get; set; }
        public Guid emergencia_id { get; set; }
    }
}
