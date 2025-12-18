# 🏥 Gestor Clínico - Sistema Integral de Gestión Médica

![Badge .NET](https://img.shields.io/badge/.NET-Core_6.0%2F7.0-purple) ![Badge Status](https://img.shields.io/badge/Estado-Finalizado-success) ![Badge ORT](https://img.shields.io/badge/Instituci%C3%B3n-ORT_Argentina-blue)

## 📖 Introducción

**Gestor Clínico** es una solución web integral diseñada para la administración, operatividad y digitalización de procesos en instituciones médicas. El sistema centraliza el flujo completo de atención sanitaria, abarcando desde la gestión administrativa de agendas y la asignación inteligente de turnos, hasta la atención dinámica en consultorios y guardias, finalizando con el registro detallado en historias clínicas electrónicas.

A diferencia de los sistemas tradicionales, **Gestor Clínico** pone un énfasis especial en la **integridad y persistencia de los datos**. Se ha implementado un patrón de **Borrado Lógico (Soft Delete)** transversal a todas las entidades críticas. Esto garantiza que la información nunca se pierda definitivamente, permitiendo la recuperación de registros eliminados accidentalmente y manteniendo una coherencia histórica total en la base de datos para futuras auditorías.

Este proyecto fue desarrollado como trabajo final para la materia **Prácticas en Nuevas Tecnologías 1** de la carrera **Analista de Sistemas** en **ORT Argentina**. El objetivo principal fue construir una arquitectura **MVC** robusta, escalable y segura en **.NET**, aplicando principios SOLID, inyección de dependencias y un estricto manejo de estados para modelar la complejidad del negocio de la salud.

---

## 🚀 Funcionalidades del Sistema

El sistema está dividido en módulos basados en roles de usuario (Identity), asegurando que cada actor tenga acceso a las herramientas específicas de su función.

### 👨‍⚕️ Módulo Médicos
* **Atención de Consultorio:** Dashboard diario con los pacientes agendados. Permite visualizar el estado del paciente (En espera, Atendido).
* **Historia Clínica Electrónica:** Carga de evolución médica (diagnóstico, tratamiento, observaciones) vinculada al paciente.
* **Recetas y Órdenes Médicas:** Generación dinámica de recetas y órdenes de estudios durante la consulta.
* **Dashboard Personal:** Estadísticas de turnos del día, próximos pacientes y accesos rápidos.

### 🧑‍🦱 Módulo Pacientes
* **Autogestión de Turnos:** Buscador inteligente de turnos disponibles con filtros por especialidad y médico. Reserva inmediata.
* **Portal del Paciente:** Visualización de próximos turnos y estado de los mismos.
* **Historial Médico:** Acceso de lectura a sus propias recetas y órdenes médicas generadas en consultas anteriores.

### 🏥 Módulo Guardia / Emergencias
* **Cola de Espera (Triage):** Gestión de pacientes en sala de espera de guardia.
* **Atención de Urgencia:** Flujo rápido de atención sin turno previo, con generación inmediata de evolución clínica.
* **Ingreso de Pacientes:** Búsqueda rápida por DNI para ingreso a la cola de guardia.

### 🛠️ Módulo Administrativo
* **Gestión de Entidades:** ABM (Alta, Baja, Modificación) de Médicos, Pacientes y Administrativos.
* **Gestión de Usuarios:** Control de accesos, roles y reactivación de usuarios eliminados lógicamente.
* **Gestión de Agenda:** Herramienta visual para generar turnos masivos configurando rangos de fechas, horarios y duración de la consulta.
* **Reportes y Estadísticas:** Dashboard global con métricas de ocupación, cantidad de pacientes atendidos por centro médico y tiempos promedio de espera.
* **Cancelación de Agendas:** Herramientas para cancelar turnos o días completos, notificando y liberando recursos.

---

## 🛠️ Tecnologías Utilizadas

* **Backend:** ASP.NET Core MVC (C#).
* **ORM:** Entity Framework Core (Code First).
* **Base de Datos:** SQL Server.
* **Seguridad:** ASP.NET Core Identity (Manejo de Roles y Usuarios).
* **Frontend:** Razor Views, HTML5, CSS3, Bootstrap 5.
* **Herramientas:** Visual Studio 2022.

---

## 💻 Instalación y Puesta en Marcha

Sigue estos pasos para ejecutar el proyecto en tu entorno local:

### 1. Prerrequisitos
* Tener instalado **Visual Studio 2022** (o superior) con la carga de trabajo de ASP.NET y desarrollo web.
* Tener instalado **SQL Server** (LocalDB o Express).
* .NET SDK compatible.

### 2. Clonar el Repositorio
```bash
git clone [https://github.com/LautaroArtecona/GestorClinico-MVC.git](https://github.com/LautaroArtecona/GestorClinico-MVC.git)
cd GestorClinico
```
### 3. Configurar Base de Datos
Abre el archivo `appsettings.json` y asegúrate de que la cadena de conexión `ClinicaDBContext` apunte a tu instancia local de SQL Server
```json
"ConnectionStrings": {
  "ClinicaDBContext": "Server=.;Database=GestorClinicoDB;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```
### 4. Ejecutar Migraciones
Abre la consola del Administrador de Paquetes (Package Manager Console) en Visual Studio y ejecuta:
```PowerShell
Update-Database
```
Esto creará la base de datos y todas las tablas necesarias.

### 5. Carga de Datos Críticos (IMPORTANTE)
Para que el sistema funcione correctamente, es obligatorio que la tabla Estados contenga los siguientes 5 registros exactos. El sistema depende de estos nombres para la lógica de turnos.

Ejecuta el siguiente script SQL en tu base de datos recién creada:
```SQL
INSERT INTO Estados (Nombre) VALUES ('En Espera');
INSERT INTO Estados (Nombre) VALUES ('Atendido');
INSERT INTO Estados (Nombre) VALUES ('Cancelado');
INSERT INTO Estados (Nombre) VALUES ('Libre');
INSERT INTO Estados (Nombre) VALUES ('Asignado');
```
### 6. Ejecutar
Presiona F5 o el botón de Play en Visual Studio. El sistema creará automáticamente los roles necesarios (Admin, Medico, Paciente) cuando intentes registrar el primer usuario de cada tipo.

## 📸 Capturas de Pantalla

| Inicio | Portal Médico | Portal Admin | Portal Paciente |
|:---:|:---:|:---:|
| ![Inicio](Screenshots/inicio.png) | ![Medico](Screenshots/portal-medico.png) | ![Admin](Screenshots/portal-admin.png) | ![Paciente](Screenshots/portal-paciente.png) |
