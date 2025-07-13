namespace GroupService.DTOs
{
    public class GrupoConRecursosDTO
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; }
        public string ZonaAsignada { get; set; }
        public List<RecursoSimpleDTO> Recursos { get; set; }
    }
}
