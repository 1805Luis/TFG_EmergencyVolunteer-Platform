# Plataforma de Gestión de Voluntariado en Situaciones de Emergencia

![Estado del proyecto](https://img.shields.io/badge/estado-en_desarrollo-yellow)
![Licencia](https://img.shields.io/badge/licencia-reservada-red)

---

## Descripción

Este proyecto es el Trabajo de Fin de Grado (TFG) para el Grado en Tecnologías para la Sociedad de la Información. Consiste en el desarrollo de una plataforma distribuida y escalable para gestionar voluntarios en situaciones de emergencia, facilitando la coordinación, asignación de tareas y trazabilidad mediante una arquitectura de microservicios.

---

## Motivación

La rapidez y coordinación en situaciones críticas como catástrofes naturales o emergencias sociales es vital para salvar vidas y optimizar recursos. Este sistema busca mejorar la gestión de personas voluntarias, permitiendo asignar tareas de forma eficiente basándose en habilidades, disponibilidad y ubicación geográfica.

---

## Tecnologías utilizadas

| Categoría             | Tecnologías                                  |
|-----------------------|----------------------------------------------|
| Lenguaje backend      | .NET 8 (C#)                                  |
| Frontend              | React + Vite                                 |
| API Gateway           | Ocelot                                       |
| Mensajería            | RabbitMQ                                     |
| Base de datos         | PostgreSQL y SQL Server                      |
| Contenedores          | Docker & Docker Compose                       |
| Seguridad             | JWT (JSON Web Tokens)                         |
| Balanceo de carga     | NGINX (opcional)                             |

---

## Arquitectura del sistema

El sistema está dividido en microservicios independientes que se comunican a través de RabbitMQ y se exponen mediante un API Gateway que gestiona la autenticación, autorización y el enrutamiento.

- **Auth Service**: Gestión de usuarios, autenticación y emisión de JWT.
- **User Service**: Datos detallados de voluntarios.
- **Group Service**: Organización en grupos.
- **Emergency Service**: Registro y gestión de emergencias.
- **Task Service**: Creación y gestión de tareas.
- **Assignment Service**: Asignación de tareas a voluntarios.
- **Bitácora Service**: Registro y auditoría de eventos.

 ---

## Estructura del repositorio
/
- API_gestion/ # Toda la API con microservicios y API Gateway
- nginx/ # Configuración del balanceador de carga NGINX
- UI-gestion/ # Frontend React del sistema
- docker-compose.con-balanceador.yml # Docker Compose con NGINX balanceador
- docker-compose.sin-balanceador.yml # Docker Compose sin NGINX (acceso directo)

### Descripción general

- La carpeta **API_gestion** contiene todos los microservicios que conforman la API REST, incluyendo el API Gateway configurado con Ocelot.
- La carpeta **nginx** incluye la configuración para el balanceador NGINX que distribuye el tráfico hacia el API Gateway y otros servicios si es necesario.
- La carpeta **UI-gestion** contiene la interfaz web para administración y uso por parte de voluntarios.
- Los archivos **docker-compose.con-balanceador.yml** y **docker-compose.sin-balanceador.yml** permiten levantar la arquitectura completa con o sin el balanceador NGINX.

## Cómo ejecutar el proyecto
### Requisitos previos

- Docker y Docker Compose instalados

### Levantar la arquitectura sin balanceador (acceso directo al API Gateway)

```bash
docker-compose -f docker-compose.sin-balanceador.yml up --build
```

### Levantar la arquitectura sin balanceador (acceso directo al API Gateway)

```bash
docker-compose -f docker-compose.con-balanceador.yml up --build
```

## Uso básico
- Accede al frontend en el navegador en: http://localhost:3000
- Regístrate o inicia sesión.
- Gestiona emergencias, voluntarios, tareas y asignaciones desde la interfaz.

## Funcionalidades clave
Registro y autenticación con JWT.

Gestión completa de usuarios y grupos.

Registro de emergencias con geolocalización.

Creación y asignación automática de tareas según parámetros.

Comunicación asíncrona mediante RabbitMQ.

Bitácora para auditoría de eventos y acciones.

Frontend intuitivo para usuarios y administradores.

## Autor
Luis Zambrana

Gradudado del Grado en Tecnologías para la Sociedad de la Información

Universidad Politecnica de Madrid

[🔗 LinkedIn](https://www.linkedin.com/in/luis-zambrana-rodrigo-6100b421b/)

