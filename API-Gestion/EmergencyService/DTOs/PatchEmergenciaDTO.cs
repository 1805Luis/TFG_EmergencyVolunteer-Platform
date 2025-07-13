using NetTopologySuite.Geometries;

namespace EmergencyService.DTOs
{
    public class PatchEmergenciaDTO
    {
        public string? tipo_emergencia { get; set; }
        public string? zona_afectada { get; set; }
        public string? descripcion { get; set; }
        public string? gravedad { get; set; }
        public string? estado { get; set; }
        public List<List<double>>? Coordenadas { get; set; }

    }
}
