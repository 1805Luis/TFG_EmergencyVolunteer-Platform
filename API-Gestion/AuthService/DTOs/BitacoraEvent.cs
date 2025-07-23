namespace AuthService.DTOs
{
    public class BitacoraEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string EntityType { get; set; } // Nombre de la entidad afectada
        public string Action { get; set; }    // Acción realizada sobre la entidad Create, Update, Delete
        public Guid ActorId { get; set; } // Identificador único del actor que realiza la acción
        public string EntityId { get; set; } // Identificador único de la entidad afectada por la acción.
        public Dictionary<string, object> Antes { get; set; } // Diccionario con los campos y valores previos al cambio
        public Dictionary<string, object> Despues { get; set; } // Diccionario con los campos y valores posteriores al cambio
        public DateTime Timestamp { get; set; } // Marca temporal UTC en la que se realizó la acción.
    }
}
