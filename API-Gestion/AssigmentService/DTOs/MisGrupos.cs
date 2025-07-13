namespace AssigmentService.DTOs
{
    public class MisGrupos
    {
        public Guid Grupo_Id { get; set; }

        public string NombreGrupo { get; set; }
        public string Rol { get; set; }

        public Guid? Tarea_Id { get; set; }
        public string EstadoTarea { get; set; }
    }
}
