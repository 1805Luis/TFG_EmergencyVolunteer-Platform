namespace TaskService.DTOs
{
    public class TaskUpdateDTO
    {
        public Guid tarea_id {  get; set; }
        public Guid id_usuario { get; set; }
        public string estado { get; set; }  
    }
}
