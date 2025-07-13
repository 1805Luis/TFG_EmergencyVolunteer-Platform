using System.Text;
using System.Text.Json;
using AssigmentService.Context;
using AssigmentService.Data;
using AssigmentService.DTOs;
using AssigmentService.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionFactory _factory;

    public RabbitMqConsumer(IServiceProvider serviceProvider, IOptions<RabbitMQSettings> settings)
    {
        _serviceProvider = serviceProvider;
        var config = settings.Value;

        _factory = new ConnectionFactory()
        {
            HostName = config.HostName,
            UserName = config.UserName,
            Password = config.Password,
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        try
        {
            using var connection = await RetryCreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            Console.WriteLine("Conexion y Canal creado");

            const string exchangeName = "user_delete_broadcast";
            const string queueName = "user_delete_queue_assignment";
            const string taskDeleteQueue = "task_delete_queue";
            const string groupCreateQueue = "group_create_queue";
            const string groupDeleteQueue = "group_delete_queue";

            await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Fanout, durable: true);

            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

            await channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: "");

            await channel.QueueDeclareAsync(queue: taskDeleteQueue, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeclareAsync(queue: groupCreateQueue, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeclareAsync(queue: groupDeleteQueue, durable: true, exclusive: false, autoDelete: false);
           


            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine("\n======================================");
                Console.WriteLine("📥 NUEVO MENSAJE RECIBIDO - ASSIGMENT SERVICE");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine($"🧾 Cola de origen : {ea.RoutingKey}");
                Console.WriteLine($"📨 Contenido      :\n{message}");
                Console.WriteLine("======================================\n");

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDBContext>();
                    var producer = scope.ServiceProvider.GetRequiredService<RabbitMQProducer>();

                    if (ea.RoutingKey == taskDeleteQueue)
                    {
                        var data = JsonSerializer.Deserialize<TaskDeleteDTO>(message);
                        if (data == null || !data.id_task.Any()) return;

                        var eventos = new List<BitacoraEvent>();

                        foreach (var tareaId in data.id_task)
                        {
                            var gruposAsociados = await context.tareas_grupos
                                .Where(tg => tg.tarea_id == tareaId)
                                .ToListAsync();

                            var antes = new Dictionary<string, object>();

                            foreach (var grupo in gruposAsociados)
                            {
                                var miembros = await context.grupos_voluntarios
                                    .Where(gv => gv.grupo_id == grupo.grupo_id)
                                    .Select(gv => new { gv.voluntario_id, gv.rol })
                                    .ToListAsync();

                                antes[$"Grupo_{grupo.grupo_id}"] = miembros;
                            }

                            var evento = new BitacoraEvent
                            {
                                EntityType = "Tareas_Grupos",
                                EntityId = tareaId.ToString(),
                                Action = "Delete",
                                ActorId = data.id_Usuario,
                                Antes = antes,
                                Despues = new Dictionary<string, object>(),
                                Timestamp = DateTime.UtcNow
                            };

                            eventos.Add(evento);

                            // Eliminar asociaciones tarea-grupo
                            context.tareas_grupos.RemoveRange(gruposAsociados);

                            // Eliminar voluntarios asociados a los grupos
                            var voluntariosAsociados = await context.grupos_voluntarios
                                .Where(gv => gruposAsociados.Select(g => g.grupo_id).Contains(gv.grupo_id))
                                .ToListAsync();

                            foreach (var recurso in voluntariosAsociados)
                            {
                                var antesRecurso = new Dictionary<string, object>
                                {
                                    { "GrupoId", recurso.grupo_id },
                                    { "VoluntarioId", recurso.voluntario_id },
                                    { "Rol", recurso.rol.ToString() }
                                };

                                var eventoRecurso = new BitacoraEvent
                                {
                                    EntityType = "Grupo_Voluntario",
                                    EntityId = $"{recurso.grupo_id}_{recurso.voluntario_id}",
                                    Action = "Delete",
                                    ActorId = data.id_Usuario,
                                    Antes = antesRecurso,
                                    Despues = new Dictionary<string, object>(),
                                    Timestamp = DateTime.UtcNow
                                };

                                eventos.Add(eventoRecurso);
                            }

                            context.grupos_voluntarios.RemoveRange(voluntariosAsociados);
                            Console.WriteLine($"🗑 Eliminados {voluntariosAsociados.Count} registros de grupos_voluntarios");

                            var gruposAEliminar = gruposAsociados.Select(g => g.grupo_id).Distinct().ToList();

                            var deleteGroupDto = new GroupDeleteDTO
                            {
                                id_usuario = data.id_Usuario,
                                id_group = gruposAEliminar,
                                tipoEliminacion = TipoEliminacion.EliminarGrupos
                            };

                            await producer.SendMessageToQueueAsync("group_unsubscribe_queue", deleteGroupDto, "assigment_ServiceConnection");
                        }

                        await context.SaveChangesAsync();
                        await producer.SendMessageToQueueAsync("audit_log_queue", eventos, "audit_ServiceConnection");
                        Console.WriteLine($"🗑 Eliminadas asociaciones de tareas {string.Join(",", data.id_task)}");
                    }
                    else if (ea.RoutingKey == groupCreateQueue)
                    {
                        var data = JsonSerializer.Deserialize<AsignaciónDTO>(message);
                        if (data == null) return;

                        context.grupos_voluntarios.Add(new Grupos_Voluntarios
                        {
                            grupo_id = data.Id_grupo,
                            voluntario_id = data.Id_usuario,
                            rol = Rol.Lider
                        });

                        context.tareas_grupos.Add(new Tareas_Grupos
                        {
                            grupo_id = data.Id_grupo,
                            tarea_id = data.Id_tarea,
                            estado = Estado.EnProgreso,
                            fecha_completada = null
                        });

                        await context.SaveChangesAsync();

                        var estadoTarea = new TaskUpdateDTO
                        {
                            tarea_id = data.Id_tarea,
                            id_usuario = data.Id_usuario,
                            estado = "EnProgreso"
                        };

                        await producer.SendMessageToQueueAsync("task_update_queue", estadoTarea, "assigment_ServiceConnection");
                        Console.WriteLine($"✅ Asignación procesada para grupo {data.Id_grupo} y tarea {data.Id_tarea}");
                    }
                    else if (ea.RoutingKey == groupDeleteQueue)
                    {
                        var data = JsonSerializer.Deserialize<GroupDeleteDTO>(message);
                        if (data == null || !data.id_group.Any()) return;

                        var eventos = new List<BitacoraEvent>();

                        foreach (var groupId in data.id_group)
                        {
                            // Eliminar voluntarios del grupo
                            var voluntarios = await context.grupos_voluntarios
                                .Where(gv => gv.grupo_id == groupId)
                                .ToListAsync();

                            var antes = new Dictionary<string, object>
                            {
                                [$"Grupo_{groupId}"] = voluntarios.Select(gv => new { gv.voluntario_id, gv.rol }).ToList()
                            };

                            context.grupos_voluntarios.RemoveRange(voluntarios);

                            // Añadir a bitácora
                            eventos.Add(new BitacoraEvent
                            {
                                EntityType = "Grupo",
                                EntityId = groupId.ToString(),
                                Action = "Delete",
                                ActorId = data.id_usuario.Value,
                                Antes = antes,
                                Despues = new Dictionary<string, object>(),
                                Timestamp = DateTime.UtcNow
                            });

                            //Obtener relaciones tarea–grupo asociadas a este grupo
                            var tareasGrupoRelacionadas = await context.tareas_grupos
                                .Where(tg => tg.grupo_id == groupId)
                                .ToListAsync();

                            foreach (var tg in tareasGrupoRelacionadas)
                            {
                                // Ver si la tarea tiene otros grupos asignados (distintos al que se va a borrar)
                                bool tieneMasGrupos = await context.tareas_grupos
                                    .AnyAsync(rel => rel.tarea_id == tg.tarea_id && rel.grupo_id != groupId);

                                // Si NO tiene más → mandar mensaje para actualizar estado a SinEmpezar
                                if (!tieneMasGrupos)
                                {
                                    var estadoTarea = new TaskUpdateDTO
                                    {
                                        tarea_id = tg.tarea_id,
                                        id_usuario = data.id_usuario.Value,
                                        estado = "SinEmpezar"
                                    };

                                    await producer.SendMessageToQueueAsync("task_update_queue", estadoTarea, "assigment_ServiceConnection");
                                }

                                context.tareas_grupos.Remove(tg);
                            }
                        }

                        //Guardar cambios en base de datos
                        await context.SaveChangesAsync();

                        await producer.SendMessageToQueueAsync("audit_log_queue", eventos, "audit_ServiceConnection");

                        Console.WriteLine($"🗑 Eliminadas referencias de los grupos {string.Join(",", data.id_group)}");
                    }
                    else
                    {
                        var deleteDto = JsonSerializer.Deserialize<UserDeleteDTO>(message);
                        if (deleteDto != null && deleteDto.id_usuario != Guid.Empty)
                        {
                            Console.WriteLine("🗑 Procesando eliminación de usuario...");

                            // Obtener todos los grupos en los que participa el usuario que será eliminado
                            var gruposDelUsuario = await context.grupos_voluntarios
                                .Where(gv => gv.voluntario_id == deleteDto.id_usuario)
                                .ToListAsync();

                            var eventos = new List<BitacoraEvent>();

                            // Recorrer cada relación del usuario con grupos
                            foreach (var grupo in gruposDelUsuario)
                            {
                                // Contar cuántos voluntarios hay en el grupo actual
                                var totalVoluntarios = await context.grupos_voluntarios
                                    .CountAsync(gv => gv.grupo_id == grupo.grupo_id);

                                if (totalVoluntarios <= 1)
                                {
                                    // Si el usuario era el único miembro del grupo, se elimina todo el grupo
                                    Console.WriteLine($"Usuario {deleteDto.id_usuario} era el único voluntario en el grupo {grupo.grupo_id}, se procederá a eliminar el grupo completo.");

                                    var tareasAsociadas = await context.tareas_grupos
                                       .Where(tg => tg.grupo_id == grupo.grupo_id)
                                       .ToListAsync();

                                    foreach (var tareaGrupo in tareasAsociadas)
                                    {
                                        bool otrosGrupos = await context.tareas_grupos
                                            .AnyAsync(tg => tg.tarea_id == tareaGrupo.tarea_id && tg.grupo_id != grupo.grupo_id);

                                        if (!otrosGrupos)
                                        {
                                            var taskUpdate = new TaskUpdateDTO
                                            {
                                                tarea_id = tareaGrupo.tarea_id,
                                                id_usuario = deleteDto.id_usuario,
                                                estado = "SinEmpezar"
                                            };

                                            await producer.SendMessageToQueueAsync("task_update_queue", taskUpdate, "assigment_ServiceConnection");
                                            Console.WriteLine($"Estado de tarea {tareaGrupo.tarea_id} actualizado a 'SinEmpezar' porque no tiene otros grupos asignados.");
                                        }
                                    }

                                    var deleteGroupDto = new GroupDeleteDTO
                                    {
                                        id_usuario = deleteDto.id_usuario,
                                        id_group = new List<Guid> { grupo.grupo_id },
                                        tipoEliminacion = TipoEliminacion.EliminarGrupos
                                    };

                                    // Enviar mensaje para eliminar el grupo completo
                                    await producer.SendMessageToQueueAsync("group_unsubscribe_queue", deleteGroupDto, "assigment_ServiceConnection");
                                }
                                else
                                {
                                    // Eliminar solo la relación del voluntario con el grupo
                                    context.grupos_voluntarios.Remove(grupo);

                                    // Si el usuario eliminado era el líder del grupo
                                    if (grupo.rol == Rol.Lider)
                                    {
                                        // Verificar si ya hay otro líder en el grupo (evita duplicar el rol de líder)
                                        var yaHayLider = await context.grupos_voluntarios
                                            .AnyAsync(gv => gv.grupo_id == grupo.grupo_id && gv.rol == Rol.Lider && gv.voluntario_id != deleteDto.id_usuario);

                                        if (!yaHayLider)
                                        {
                                            // Obtener candidatos para nuevo líder (excluyendo al usuario eliminado)
                                            var candidatos = await context.grupos_voluntarios
                                                .Where(gv => gv.grupo_id == grupo.grupo_id && gv.voluntario_id != deleteDto.id_usuario)
                                                .ToListAsync();

                                            if (candidatos.Any())
                                            {
                                                // Elegir aleatoriamente un nuevo líder entre los candidatos
                                                var random = new Random();
                                                var nuevoLider = candidatos[random.Next(candidatos.Count)];

                                                var rolAnterior = nuevoLider.rol;
                                                nuevoLider.rol = Rol.Lider;

                                                // Registrar el cambio de rol en la bitácora
                                                eventos.Add(new BitacoraEvent
                                                {
                                                    EntityType = "Grupos_Voluntarios",
                                                    EntityId = $"{nuevoLider.grupo_id}_{nuevoLider.voluntario_id}",
                                                    Action = "Update",
                                                    ActorId = deleteDto.id_usuario,
                                                    Antes = new Dictionary<string, object> { { "rol", rolAnterior.ToString() } },
                                                    Despues = new Dictionary<string, object> { { "rol", Rol.Lider.ToString() } },
                                                    Timestamp = DateTime.UtcNow
                                                });

                                                Console.WriteLine($"Nuevo líder asignado en el grupo {grupo.grupo_id}: {nuevoLider.voluntario_id}");
                                            }
                                        }
                                        else
                                        {
                                            // Ya existe otro líder en el grupo, no se hace nada
                                            Console.WriteLine($"✅ Ya existe otro líder en el grupo {grupo.grupo_id}, no es necesario reasignar.");
                                        }
                                    }

                                    // Registrar la eliminación del voluntario del grupo en la bitácora
                                    var antes = new Dictionary<string, object>
                                    {
                                        { "grupo_id", grupo.grupo_id },
                                        { "rol", grupo.rol }
                                    };

                                    eventos.Add(new BitacoraEvent
                                    {
                                        EntityType = "Grupos_Voluntarios",
                                        EntityId = $"{grupo.grupo_id}_{deleteDto.id_usuario}",
                                        Action = "Delete",
                                        ActorId = deleteDto.id_usuario,
                                        Antes = antes,
                                        Despues = new Dictionary<string, object>(),
                                        Timestamp = DateTime.UtcNow
                                    });
                                }
                            }

                            // Guardar los cambios en la base de datos
                            await context.SaveChangesAsync();

                            // Enviar todos los eventos generados a la bitácora
                            if (eventos.Any())
                            {
                                await producer.SendMessageToQueueAsync("audit_log_queue", eventos, "audit_ServiceConnection");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Error: deleteDto es null o id inválido");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error procesando mensaje: {ex.Message}");
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);
            await channel.BasicConsumeAsync(queue: taskDeleteQueue, autoAck: false, consumer: consumer);
            await channel.BasicConsumeAsync(queue: groupCreateQueue, autoAck: false, consumer: consumer);
            await channel.BasicConsumeAsync(queue: groupDeleteQueue, autoAck: false, consumer: consumer);

            Console.WriteLine("🔄 Consumo iniciado en AssignmentService");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en RabbitMqConsumer: {ex}");
        }
    }

    private async Task<IConnection> RetryCreateConnectionAsync()
    {
        var tiemposEspera = new[] { 5, 10, 15, 20, 25, 30 }; // segundos
        int totalIntentos = tiemposEspera.Length;

        for (int attempt = 1; attempt <= totalIntentos; attempt++)
        {
            try
            {
                var connection = await _factory.CreateConnectionAsync("assign_ServiceConnection");
                Console.WriteLine($"✅ Conexión a RabbitMQ establecida en el intento {attempt}");
                return connection;
            }
            catch (BrokerUnreachableException ex)
            {
                Console.WriteLine($"❌ Intento {attempt}: error de conexión a RabbitMQ ({ex.Message})");

                if (attempt == totalIntentos)
                {
                    Console.WriteLine("⛔ Se alcanzó el máximo de intentos sin éxito.");
                    throw new Exception("No se pudo conectar a RabbitMQ tras múltiples intentos.");
                }

                int espera = tiemposEspera[attempt - 1];
                Console.WriteLine($"⏳ Esperando {espera} segundos antes del siguiente intento...");
                await Task.Delay(TimeSpan.FromSeconds(espera));
            }
        }

        throw new InvalidOperationException("Ruta de recuperación no alcanzada.");
    }

}
