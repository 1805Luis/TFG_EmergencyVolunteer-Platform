namespace GroupService.Data
{
    public class Grupo
    {
        public Guid Id { get; set; }
        public string nombre { get; set; }
        public string zona_asignada { get; set; }
        public ICollection<Recursos> Recursos { get; set; }
    }
}
