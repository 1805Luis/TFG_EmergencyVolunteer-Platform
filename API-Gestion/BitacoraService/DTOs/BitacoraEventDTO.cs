namespace BitacoraService.DTOs
{
    public class BitacoraEventDTO
    {
        public Guid EventId { get; set; }
        public string EntityType { get; set; }
        public string Action { get; set; }
        public Guid ActorId { get; set; }
        public string EntityId { get; set; }
        public Dictionary<string, object> Antes { get; set; }
        public Dictionary<string, object> Despues { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
