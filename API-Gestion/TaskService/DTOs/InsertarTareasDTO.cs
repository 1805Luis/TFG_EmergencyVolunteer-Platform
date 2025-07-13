namespace TaskService.DTOs
{
    public class InsertarTareasDTO
    {
        public Guid idUsuario { get; set; }
        public List<CrearTareaRabbitDTO> tareas { get; set; }
    }
}
