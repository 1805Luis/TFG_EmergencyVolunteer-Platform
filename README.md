# Plataforma de Gestión de Voluntariado en Situaciones de Emergencia

![Estado del proyecto](https://img.shields.io/badge/estado-en_desarrollo-yellow)
![Licencia](https://img.shields.io/badge/licencia-reservada-red)

---

## Índice
- [Descripción](#descripción)
- [Motivación](#motivación)
- [Tecnologías utilizadas](#tecnologías-utilizadas)
- [Arquitectura del sistema](#arquitectura-del-sistema)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Cómo ejecutar el proyecto](#cómo-ejecutar-el-proyecto)
  - [Requisitos previos](#requisitos-previos)
  - [Levantar la arquitectura sin balanceador (acceso directo al API Gateway)](#levantar-la-arquitectura-sin-balanceador-acceso-directo-al-api-gateway)
  - [Levantar la arquitectura con balanceador (acceso directo al API Gateway)](#levantar-la-arquitectura-con-balanceador-acceso-directo-al-api-gateway)  
  - [Puertos principales](#puertos-principales)
  - [Notas rápidas de Docker Compose](#notas-rápidas-de-docker-compose)- [Uso básico](#uso-básico)
- [Funcionalidades clave](#funcionalidades-clave)
- [Autor](#autor)

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
  ![Diagrama de arquitectura](https://github.com/1805Luis/TFG/blob/main/img/Diagrama%20despliegue.png)
 ---

## Estructura del repositorio
- El sistema está dividido en microservicios independientes que se comunican a través de RabbitMQ y se exponen mediante un API Gateway que gestiona la autenticación, autorización y el enrutamiento.
  - **Auth Service**: valida credenciales, crea y emite JWT, y gestiona roles y permisos.
  - **User Service**: almacena y sirve los datos personales y perfiles de voluntarios.
  - **Group Service**: administra la organización de voluntarios en grupos y equipos.
  - **Emergency Service**: registra emergencias activas, mantiene detalles de ubicación y estado.
  - **Task Service**: define tareas, criterios y estados de ejecución.
  - **Assignment Service**: asigna tareas a voluntarios.
  - **Bitácora Service**: guarda eventos importantes y acciones para auditoría y seguimiento.
- nginx #  Configuración del balanceador de carga NGINX
- UI-Gestion # Frontend React del sistema
- docker-compose.con-balanceador.yml # Docker Compose con NGINX balanceador
- docker-compose.sin-balanceador.yml # Docker Compose sin NGINX (acceso directo)

```
├── 📁 API-Gestion
│   ├── 📁 ApiGateway
│   ├── 📁 AssigmentService
│   ├── 📁 AuthService
│   ├── 📁 BitacoraService
│   ├── 📁 EmergencyService
│   ├── 📁 GroupService
│   ├── 📁 TaskService
│   ├── 📁 UserService
├── 📁 UI-Gestion
├── 📁 img
├── 📁 nginx
├── 📝 README.md
├── ⚙️ docker-compose.con-balanceador.yml
└── ⚙️ docker-compose.sin-balanceador.yml
```

### Descripción general

- La carpeta **API-Gestion** contiene todos los microservicios que conforman la API REST, incluyendo el API Gateway configurado con Ocelot.
- La carpeta **nginx** incluye la configuración para el balanceador NGINX que distribuye el tráfico hacia el API Gateway y otros servicios si es necesario.
- La carpeta **UI-Gestion** contiene la interfaz web para administración y uso por parte de voluntarios.
- Los archivos **docker-compose.con-balanceador.yml** y **docker-compose.sin-balanceador.yml** permiten levantar la arquitectura completa con o sin el balanceador NGINX.

## Cómo ejecutar el proyecto
### Requisitos previos

- Docker y Docker Compose instalados

### Levantar la arquitectura sin balanceador (acceso directo al API Gateway)

```bash
docker-compose -f docker-compose.sin-balanceador.yml up --build
```

### Levantar la arquitectura con balanceador (acceso mediante NGINX)

```bash
docker-compose -f docker-compose.con-balanceador.yml up --build
```

## Puertos principales
- Frontend: `http://localhost:3000`
- API Gateway (sin balanceador): `http://localhost:8080`
- NGINX balanceador: `http://localhost:8080`
- RabbitMQ management: `http://localhost:15672`
- SQL Server de AuthService: `14333:1433`
- SQL Server de UserService: `14334:1433`
- SQL Server de TaskService: `14335:1433`
- SQL Server de GroupService: `14337:1433`
- SQL Server de AssignmentService: `14338:1433`
- PostgreSQL Bitácora: `5432:5432`
- PostGIS Emergency: `5433:5432`

## Notas rápidas de Docker Compose
- `docker-compose.sin-balanceador.yml`: arranca una sola instancia de API Gateway y el UI con acceso directo.
- `docker-compose.con-balanceador.yml`: usa NGINX como balanceador frente a tres instancias de API Gateway.
- RabbitMQ debe estar activo antes de los servicios que consumen colas.
- Cada microservicio usa su propia base de datos; revisa los puertos de SQL Server y PostgreSQL si necesitas conectarte desde fuera del contenedor.

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

Graduado del Grado en Tecnologías para la Sociedad de la Información

Universidad Politecnica de Madrid

[🔗 LinkedIn](https://www.linkedin.com/in/luis-zambrana-rodrigo)

