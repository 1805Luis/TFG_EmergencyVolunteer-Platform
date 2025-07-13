namespace BitacoraService.Data
{
    public class Payload
    {
        public string Id { get; set; }
        public Dictionary<string, object>? Antes { get; set; }
        public Dictionary<string, object>? Despues { get; set; }
    }
}
