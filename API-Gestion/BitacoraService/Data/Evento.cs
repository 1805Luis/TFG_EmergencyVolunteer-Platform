namespace BitacoraService.Data
{
    public class Evento
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; }
        public Action_Evento Action { get; set; }
        public Guid ActorId { get; set; }
        public DateTime Timestamp { get; set; }
        public Payload Payload { get; set; }
        public string? Descripcion { get; set; }
    }
}
