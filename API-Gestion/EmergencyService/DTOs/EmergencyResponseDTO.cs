namespace EmergencyService.DTOs
{
    public class EmergencyResponseDTO
    {
        public Guid Id { get; set; }
        public string tipo_emergencia { get; set; }
        public string zona_afectada { get; set; }
        public string descripcion { get; set; }
        public string estado { get; set; }
        public string gravedad { get; set; }
        public DateTime fechaInicio { get; set; }
        public DateTime? fechaFin { get; set; }
        public string AreaAfectadaWKT { get; set; }
        public GeoJsonPolygon? AreaAfectadaGeoJson { get; set; }
    }

}
