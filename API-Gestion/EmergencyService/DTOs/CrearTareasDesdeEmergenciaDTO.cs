namespace EmergencyService.DTOs
{
    public class CrearTareasDesdeEmergenciaDTO
    {
        public Guid idUsuario { get; set; }
        public List<TareaRabbitDTO> tareas { get; set; }
    }
}
