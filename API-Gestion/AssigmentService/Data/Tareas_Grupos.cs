namespace AssigmentService.Data
{
    public class Tareas_Grupos
    {
        public Guid tarea_id { get; set; }
        public Guid grupo_id { get; set; }
        public Estado estado { get; set; }
        public DateTime? fecha_completada { get; set; }
    }
}
