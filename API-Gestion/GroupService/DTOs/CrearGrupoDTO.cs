namespace GroupService.DTOs
{
    public class CrearGrupoDTO
    {
        public string nombre { get; set; }
        public string zona_asignada { get; set; }
        public List<CrearRecursoDTO>? Recursos { get; set; }
    }
}
