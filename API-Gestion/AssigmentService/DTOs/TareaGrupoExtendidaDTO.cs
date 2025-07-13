namespace AssigmentService.DTOs
{
    public class TareaGrupoExtendidaDTO
    {
        public Guid TareaId { get; set; }
        public string NombreTarea { get; set; }
        public string DescripcionTarea { get; set; }
        public string UbicacionTarea { get; set; }
        public string GravedadTarea { get; set; }

        public Guid  GrupoId { get; set; }
        public string NombreGrupo { get; set; }
        public string ZonaAsignada { get; set; }

        public string estado { get; set; }
        public DateTime? fecha_completada { get; set; }
    }

}
