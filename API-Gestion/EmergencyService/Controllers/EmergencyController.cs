using System.Security.Claims;
using EmergencyService.ApiResponse;
using EmergencyService.Data;
using EmergencyService.DTOs;
using EmergencyService.Interfaz;
using EmergencyService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;


namespace EmergencyService.Controllers
{
    [Route("emergencias")]
    [ApiController]
    public class EmergencyController : ControllerBase
    {
        private readonly IAppDBContext _context;
        private readonly RabbitMQProducer _rabbitMQProducer;

        public EmergencyController(IAppDBContext appDBContext, RabbitMQProducer rabbitMQProducer)
        {
            _context = appDBContext;
            _rabbitMQProducer = rabbitMQProducer;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(
            Guid? id = null,
            string? tipo_emergencia = null,
            string? zona_afectada = null,
            string? descripcion = null,
            string? estado = null,
            string? gravedad = null,
            bool activa = true,
            double? lat = null,
            double? lon = null,
            double? distanciaMetros = null
        )
        {
            List<Emergency> emergencias;

            if (string.IsNullOrWhiteSpace(estado))
                estado = Estado.EnProgreso.ToString();


            if (lat.HasValue && lon.HasValue && distanciaMetros.HasValue && distanciaMetros.Value > 0)
            {
                emergencias = await _context.emergencies
                    .FromSqlInterpolated($@"
                SELECT * FROM ""emergencies""
                WHERE ST_DWithin( 
                    ST_Transform(""AreaAfectada"", 3857),
                    ST_Transform(ST_SetSRID(ST_MakePoint({lon}, {lat}), 4326), 3857),
                    {distanciaMetros})
            ")
                    .ToListAsync();

                // Aquí se filtra la lista ya obtenida en memoria

                if (id.HasValue)
                    emergencias = emergencias.Where(e => e.Id == id.Value).ToList();

                if (!string.IsNullOrWhiteSpace(tipo_emergencia))
                    emergencias = emergencias.Where(e => e.tipo_emergencia.ToLower().Contains(tipo_emergencia.ToLower())).ToList();

                if (!string.IsNullOrWhiteSpace(zona_afectada))
                    emergencias = emergencias.Where(e => e.zona_afectada.ToLower().Contains(zona_afectada.ToLower())).ToList();

                if (!string.IsNullOrWhiteSpace(descripcion))
                    emergencias = emergencias.Where(e => e.descripcion.ToLower().Contains(descripcion.ToLower())).ToList();

                if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<Estado>(estado, true, out var estadoEnum))
                    emergencias = emergencias.Where(e => e.estado == estadoEnum).ToList();

                if (!string.IsNullOrWhiteSpace(gravedad) && Enum.TryParse<Gravedad>(gravedad, true, out var gravedadEnum))
                    emergencias = emergencias.Where(e => e.gravedad == gravedadEnum).ToList();

                emergencias = activa
                    ? emergencias.Where(e => e.fechaFin == null).ToList()
                    : emergencias.Where(e => e.fechaFin != null).ToList();
            }
            else
            {
                var query = _context.emergencies.AsQueryable();

                if (id.HasValue)
                    query = query.Where(e => e.Id == id.Value);

                if (!string.IsNullOrWhiteSpace(tipo_emergencia))
                    query = query.Where(e => e.tipo_emergencia.ToLower().Contains(tipo_emergencia.ToLower()));

                if (!string.IsNullOrWhiteSpace(zona_afectada))
                    query = query.Where(e => e.zona_afectada.ToLower().Contains(zona_afectada.ToLower()));

                if (!string.IsNullOrWhiteSpace(descripcion))
                    query = query.Where(e => e.descripcion.ToLower().Contains(descripcion.ToLower()));

                if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<Estado>(estado, true, out var estadoEnum))
                    query = query.Where(e => e.estado == estadoEnum);

                if (!string.IsNullOrWhiteSpace(gravedad) && Enum.TryParse<Gravedad>(gravedad, true, out var gravedadEnum))
                    query = query.Where(e => e.gravedad == gravedadEnum);

                query = activa ? query.Where(e => e.fechaFin == null) : query.Where(e => e.fechaFin != null);

                if (lat.HasValue && lon.HasValue)
                {
                    var puntoFiltro = new NetTopologySuite.Geometries.Point(lon.Value, lat.Value) { SRID = 4326 };
                    query = query.Where(e => e.AreaAfectada != null && e.AreaAfectada.Contains(puntoFiltro));
                }

                emergencias = await query.ToListAsync();
            }

            if (!emergencias.Any())
                return NotFound(ApiResponse<string>.Fail("NotFound", "No se encontraron emergencias con los parámetros proporcionados."));

            var wktWriter = new NetTopologySuite.IO.WKTWriter();

            var emergenciasDto = emergencias.Select(e => new EmergencyResponseDTO
            {
                Id = e.Id,
                tipo_emergencia = e.tipo_emergencia,
                zona_afectada = e.zona_afectada,
                descripcion = e.descripcion,
                estado = e.estado.ToString(),
                gravedad = e.gravedad.ToString(),
                fechaInicio = e.fechaInicio,
                fechaFin = e.fechaFin,
                AreaAfectadaWKT = e.AreaAfectada != null ? wktWriter.Write(e.AreaAfectada) : null,
                AreaAfectadaGeoJson = e.AreaAfectada != null ? ConvertToGeoJsonPolygon(e.AreaAfectada) : null,

            }).ToList();

            return Ok(ApiResponse<List<EmergencyResponseDTO>>.Ok(emergenciasDto));
        }





        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(CrearEmergenciaDTO dto)
        {
            if (!Enum.TryParse<Gravedad>(dto.gravedad, true, out var gravedadParsed))
                return BadRequest(ApiResponse<string>.Fail("Validation", "Gravedad no válida."));

            Polygon polygon;
            try
            {
                polygon = ConstruirPoligonoDesdeCoordenadas(dto.Coordenadas);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.Fail("Validation", $"Error al construir el polígono: {ex.Message}"));
            }

            polygon.SRID = 4326;

            var emergencia = new Emergency
            {
                Id = Guid.NewGuid(),
                tipo_emergencia = dto.tipo_emergencia,
                zona_afectada = dto.zona_afectada,
                descripcion = dto.descripcion,
                estado = Estado.EnProgreso,
                gravedad = gravedadParsed,
                fechaInicio = DateTime.UtcNow,
                AreaAfectada = polygon
            };

            await _context.emergencies.AddAsync(emergencia);
            await _context.SaveChangesAsync();

            var wktWriter = new NetTopologySuite.IO.WKTWriter();

            var despues = new Dictionary<string, object>
            {
                { "tipo_emergencia", emergencia.tipo_emergencia },
                { "zona_afectada", emergencia.zona_afectada },
                { "descripcion", emergencia.descripcion },
                { "Estado", emergencia.estado.ToString() },
                { "Gravedad", emergencia.gravedad.ToString() },
                { "fechaInicio", emergencia.fechaInicio },
                { "AreaAfectada", wktWriter.Write(emergencia.AreaAfectada) }
            };

            var evento = new BitacoraEvent
            {
                EntityType = "Emergencia",
                EntityId = emergencia.Id.ToString(),
                Action = "Create",
                ActorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                Antes = new Dictionary<string, object>(),
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            if (dto.Tareas != null && dto.Tareas.Any())
            {
                var tareasToCreate = dto.Tareas.Select(t => new TareaRabbitDTO
                {
                    Nombre = t.Nombre,
                    Ubicacion = t.Ubicacion,
                    Descripcion = t.Descripcion,
                    gravedad = t.gravedad,
                    HabilidadesMin = t.HabilidadesMin,
                    emergencia_id = emergencia.Id
                }).ToList();

                var dataToSend = new CrearTareasDesdeEmergenciaDTO
                {
                    idUsuario = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                    tareas = tareasToCreate
                };

                await _rabbitMQProducer.SendMessageToQueueAsync("task_create_queue", dataToSend, "emergency_ServiceConnection");
            }

            return Ok(ApiResponse<string>.Ok("Emergencia creada exitosamente."));
        }





        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, PatchEmergenciaDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Token inválido o usuario no identificado."));

            var emergencia = await _context.emergencies.FindAsync(id);
            if (emergencia == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Emergencia no encontrada."));

            var userId = Guid.Parse(userIdClaim.Value);

            bool seActualizoAlgo = false;
            var antes = new Dictionary<string, object>();
            var despues = new Dictionary<string, object>();
            var wktWriter = new NetTopologySuite.IO.WKTWriter();

            if (!string.IsNullOrWhiteSpace(dto.tipo_emergencia) && dto.tipo_emergencia != emergencia.tipo_emergencia)
            {
                antes["tipo_emergencia"] = emergencia.tipo_emergencia;
                despues["tipo_emergencia"] = dto.tipo_emergencia;
                emergencia.tipo_emergencia = dto.tipo_emergencia;
                seActualizoAlgo = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.zona_afectada) && dto.zona_afectada != emergencia.zona_afectada)
            {
                antes["zona_afectada"] = emergencia.zona_afectada;
                despues["zona_afectada"] = dto.zona_afectada;
                emergencia.zona_afectada = dto.zona_afectada;
                seActualizoAlgo = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.descripcion) && dto.descripcion != emergencia.descripcion)
            {
                antes["descripcion"] = emergencia.descripcion;
                despues["descripcion"] = dto.descripcion;
                emergencia.descripcion = dto.descripcion;
                seActualizoAlgo = true;
            }

            if (!string.IsNullOrEmpty(dto.gravedad) && Enum.TryParse<Gravedad>(dto.gravedad, true, out var gravedad) && emergencia.gravedad != gravedad)
            {
                antes["Gravedad"] = emergencia.gravedad.ToString();
                despues["Gravedad"] = gravedad.ToString();
                emergencia.gravedad = gravedad;
                seActualizoAlgo = true;
            }

            if (!string.IsNullOrEmpty(dto.estado) && Enum.TryParse<Estado>(dto.estado, true, out var estado) && emergencia.estado != estado)
            {
                antes["Estado"] = emergencia.estado.ToString();
                despues["Estado"] = estado.ToString();
                emergencia.estado = estado;
                seActualizoAlgo = true;

                if (estado == Estado.Completada)
                {
                    antes["fechaFin"] = emergencia.fechaFin?.ToString("o") ?? "null";
                    emergencia.fechaFin = DateTime.UtcNow;
                    despues["fechaFin"] = emergencia.fechaFin?.ToString("o");
                }
                else if (estado == Estado.EnProgreso && emergencia.fechaFin != null)
                {
                    antes["fechaFin"] = emergencia.fechaFin?.ToString("o");
                    emergencia.fechaFin = null;
                    despues["fechaFin"] = "null";
                }
            }

            if (dto.Coordenadas != null && dto.Coordenadas.Any())
            {
                try
                {
                    var nuevoPoligono = ConstruirPoligonoDesdeCoordenadas(dto.Coordenadas);
                    antes["AreaAfectada"] = emergencia.AreaAfectada != null ? wktWriter.Write(emergencia.AreaAfectada) : "null";
                    emergencia.AreaAfectada = nuevoPoligono;
                    despues["AreaAfectada"] = wktWriter.Write(nuevoPoligono);
                    seActualizoAlgo = true;
                }
                catch (Exception ex)
                {
                    return BadRequest(ApiResponse<string>.Fail("Validation", $"Error al actualizar el área afectada: {ex.Message}"));
                }
            }

            if (!seActualizoAlgo)
                return BadRequest(ApiResponse<string>.Fail("NoChanges", "No se realizó ningún cambio."));

            await _context.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Emergencia",
                EntityId = id.ToString(),
                Action = "Update",
                ActorId = userId,
                Antes = antes,
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Emergencia actualizada correctamente."));
        }



        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var emergencia = await _context.emergencies.FindAsync(id);
            if (emergencia == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Emergencia no encontrada."));

            var antes = new Dictionary<string, object>
            {
                { "tipo_emergencia", emergencia.tipo_emergencia },
                { "zona_afectada", emergencia.zona_afectada },
                { "descripcion", emergencia.descripcion },
                { "Estado", emergencia.estado.ToString() },
                { "Gravedad", emergencia.gravedad.ToString() },
                { "fechaInicio", emergencia.fechaInicio },
                { "fechaFin", emergencia.fechaFin }
            };

            _context.emergencies.Remove(emergencia);
            await _context.SaveChangesAsync();

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var dataToSend = new BorrarTareasPorEmergenciaDTO
            {
                id_usuario = userId,
                id_emergencia = id
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("emergency_delete_queue", dataToSend, "task_ServiceConnection");

            var evento = new BitacoraEvent
            {
                EntityType = "Emergencia",
                EntityId = id.ToString(),
                Action = "Delete",
                ActorId = userId,
                Antes = antes,
                Despues = new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Emergencia eliminada con éxito."));
        }

        private Polygon ConstruirPoligonoDesdeCoordenadas(List<List<double>> coordenadas)
        {
            if (coordenadas == null || coordenadas.Count < 4)
                throw new ArgumentException("Se necesitan al menos 4 coordenadas para formar un polígono cerrado.");

            var coords = coordenadas.Select(c => new Coordinate(c[0], c[1])).ToList();

            if (!coords.First().Equals2D(coords.Last()))
            {
                coords.Add(coords.First());
            }

            var ring = new LinearRing(coords.ToArray()) { SRID = 4326 };
            return new Polygon(ring) { SRID = 4326 };
        }

        private GeoJsonPolygon ConvertToGeoJsonPolygon(NetTopologySuite.Geometries.Geometry geometry)
        {
            var polygon = geometry as NetTopologySuite.Geometries.Polygon;
            if (polygon == null)
                return null;

            var shell = polygon.ExteriorRing;
            var coordinates = shell.Coordinates
                .Select(c => new double[] { c.X, c.Y }) // X=lon, Y=lat
                .ToList();

            // Asegurar que el anillo esté cerrado
            if (coordinates.Count > 0 && !coordinates[0].SequenceEqual(coordinates[^1]))
                coordinates.Add(coordinates[0]);

            return new GeoJsonPolygon
            {
                Coordinates = new List<List<double[]>> { coordinates }
            };
        }

    }
}
