namespace GroupService.DTOs
{
    public class GroupDeleteDTO
    {
        public Guid? id_usuario { get; set; }
        public List<Guid> id_group { get; set; } = new();
        public TipoEliminacion? tipoEliminacion { get; set; }
    }
}
