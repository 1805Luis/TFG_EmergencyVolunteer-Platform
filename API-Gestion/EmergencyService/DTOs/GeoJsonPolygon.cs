namespace EmergencyService.DTOs
{
    public class GeoJsonPolygon
    {
        public string Type { get; set; } = "Polygon";
        public List<List<double[]>> Coordinates { get; set; } = new();
    }
}
