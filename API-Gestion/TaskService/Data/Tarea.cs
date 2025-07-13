namespace TaskService.Data
{
    public class Tarea
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; }
        public string Ubicacion {  get; set; }
        public string Descripcion { get; set; }
        public Guid emergencia_id { get; set; }
        public Estado estado { get; set; }
        public Gravedad gravedad { get; set; }
        public string HabilidadesMin {  get; set; }


    } 
}
