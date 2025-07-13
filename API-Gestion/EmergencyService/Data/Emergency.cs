using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace EmergencyService.Data
{
    public class Emergency
    {
        public Guid Id { get; set; }
        public string tipo_emergencia { get; set; }
        public string zona_afectada { get; set; }
        public Polygon AreaAfectada { get; set; }
        public string descripcion { get; set; }
        public Estado estado { get; set; }
        public Gravedad gravedad { get; set; }
        public DateTime fechaInicio { get; set; }
        public DateTime? fechaFin  { get; set; }

        [NotMapped]
        public List<List<double>> AreaAfectadaCoordenadas
        {
            get
            {
                if (AreaAfectada == null)
                    return new();

                var coords = AreaAfectada.Coordinates
                    .Select(c => new List<double> { c.X, c.Y }) // X=long, Y=lat
                    .ToList();

                // Eliminar la última si es igual a la primera (polígono cerrado)
                if (coords.Count > 1 &&
                    coords.First()[0] == coords.Last()[0] &&
                    coords.First()[1] == coords.Last()[1])
                {
                    coords.RemoveAt(coords.Count - 1);
                }

                return coords;
            }
        }

    }
}
