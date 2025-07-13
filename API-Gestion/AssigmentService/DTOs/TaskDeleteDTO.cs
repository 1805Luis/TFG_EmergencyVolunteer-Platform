namespace AssigmentService.DTOs
{
    public class TaskDeleteDTO
    {
        public Guid id_Usuario { get; set; }
        public List<Guid> id_task { get; set; } = new();
    }
}
