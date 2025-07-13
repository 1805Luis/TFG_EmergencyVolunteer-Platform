namespace AssigmentService.DTOs
{
    public class RecursoDTO
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; }
        public Guid VoluntarioId { get; set; }
    }

    public class GrupoDTO
    {
        public Guid Id { get; set; }
        public string nombre { get; set; }
        public string zonaAsignada { get; set; }
        public List<RecursoDTO> Recursos { get; set; }
    }
}
