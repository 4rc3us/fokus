# Fokus para Windows - Backlog de Producto

Este backlog detalla todos los requerimientos y épicas para la construcción de **Fokus para Windows**, con paridad funcional total respecto a [focus-plasmoid](https://gitlab.com/divinae/focus-plasmoid) de KDE Plasma 6 y diseño moderno Windows 11 Fluent.

---

## 📌 Épica 1: Motor del Temporizador y Estado (Core Engine)

### US-1.1: Máquina de Estados Clásica Pomodoro
- **Descripción**: Permitir temporizadores con tiempos configurables de enfoque (ej. 25 min), descanso corto (5 min) y descanso largo (20 min) tras N ciclos (ej. 4 sesiones).
- **Criterios de Aceptación**:
  - Transición automática o manual entre estados (Enfoque -> Descanso corto/largo).
  - Presets disponibles: 25/5/20, 50/10/30, 90/20/30 y modo Personalizado.
  - Omitir descansos configurados en 0 minutos sin bloquear la secuencia.
  - Indicador numérico de ciclo actual (ej. sesión 2 de 4).

### US-1.2: Modo Flowmodoro
- **Descripción**: Permitir trabajar sin límite de tiempo (cuenta progresiva ascendente) y ganar tiempo de descanso en base a un divisor configurable.
- **Criterios de Aceptación**:
  - Cuenta ascendente desde 00:00:00.
  - Cálculo automático del descanso = `Tiempo de flujo / divisor` (por defecto 1/5).
  - Recordatorio sutil periódico configurable (por defecto cada 30m) para evitar olvidar el temporizador activo.

### US-1.3: Control Interactivo y Rueda del Ratón
- **Descripción**: Ajustar el tiempo restante de forma rápida mediante la rueda del ratón o botones.
- **Criterios de Aceptación**:
  - Scroll del ratón sobre el icono del tray o el dial agrega/resta minutos (+/- 1 min por click estándar de rueda, delta 120).
  - Funciones de Iniciar, Pausar, Reanudar, Detener/Reiniciar y Saltar sesión.
  - Botón "Posponer" (`Postpone`) para añadir tiempo adicional de descanso cuando se necesite.

### US-1.4: Resiliencia ante Reinicios y Suspensión del Sistema
- **Descripción**: Garantizar que el temporizador sobreviva a reinicios de la aplicación y detecte suspensiones del equipo.
- **Criterios de Aceptación**:
  - Almacenar la fecha límite (`deadline`) en tiempo real para sesiones activas.
  - Restaurar el tiempo correcto al reabrir la app.
  - Si el desfase entre comprobaciones excede el límite de tolerancia (`> 5 min`), identificar que el sistema estuvo suspendido/apagado y evitar contabilizar ese tiempo como concentración real.

### US-1.5: Gestión de Tiempo Extra en Descanso (Overrun)
- **Descripción**: Si un descanso finaliza y el usuario no regresa de inmediato, el contador continúa en valores negativos.
- **Criterios de Aceptación**:
  - Contador muestra valores negativos (ej. `-01:45`).
  - Pulso visual continuo en la interfaz y repetición configurable del sonido de alerta cada N segundos (por defecto 60s).

---

## 📌 Épica 2: Interfaz en la Bandeja del Sistema (System Tray)

### US-2.1: Renderizado Dinámico de Iconos en Tiempo Real
- **Descripción**: Mostrar el progreso del temporizador directamente en el icono de la bandeja del sistema.
- **Criterios de Aceptación**:
  - 3 estilos seleccionables:
    1. Marco en forma de tomate con relleno proporcional.
    2. Anillo circular delgado con arco de progreso.
    3. Cuña circular rellena con borde sutil.
  - Opciones de texto en el tray: oculto, visible al lado del icono o sustituir el icono por el tiempo en ejecución.
  - Destello visual en el icono al completarse un ciclo para usuarios que usan modo silencio.

### US-2.2: Menú Contextual (Click Derecho)
- **Descripción**: Ofrecer acceso rápido a las acciones más comunes sin necesidad de abrir la ventana completa.
- **Criterios de Aceptación**:
  - Selector de Presets de duración (25/5/20, 50/10/30, 90/20/30).
  - Interruptor directo para alternar modo Flowmodoro.
  - Lista rápida de tareas pendientes para cambio inmediato de contexto.
  - Acceso directo a Configuración y opción de Salir.

### US-2.3: Tooltip Informativo y Eventos de Click
- **Descripción**: Informar del estado al pasar el ratón y abrir la ventana al hacer click.
- **Criterios de Aceptación**:
  - Tooltip enriquecido mostrando: Modo, Estado (Enfoque / Descanso), Tiempo restante, Tarea actual y Ciclo.
  - Click izquierdo abre/cierra de forma fluida el Flyout de control anclado al área de notificación.

---

## 📌 Épica 3: Flyout Principal y Dial de Control (WPF Fluent)

### US-3.1: Dial Central de Tiempo con Estilo Fluent
- **Descripción**: Dial circular con diseño moderno Glassmorphism / Mica para Windows 11.
- **Criterios de Aceptación**:
  - Anillo de progreso animado y suave con colores temáticos (rojo/tomate para enfoque, verde/azul para descanso).
  - Tipografía clara para los dígitos, con opción de dígitos en negrita y fuente personalizada.
  - Puntos indicadores de ciclo de sesiones (`session dots`).
  - Insignia con el nombre de la tarea en curso.

### US-3.2: Navegación por Pestañas
- **Descripción**: Navegación integrada en el Flyout para Temporizador, Tareas, Estadísticas y Configuración.
- **Criterios de Aceptación**:
  - Selector de vista ágil y con transiciones limpias.
  - Integración nativa con el tema oscuro/claro de Windows 11.

---

## 📌 Épica 4: Gestor de Tareas Integrado (Task List)

### US-4.1: Gestión Básica de Tareas (CRUD)
- **Descripción**: Administrar las tareas pendientes directamente desde la aplicación.
- **Criterios de Aceptación**:
  - Agregar tarea nueva rápidamente.
  - Marcar como completada mediante casilla de verificación.
  - Edición de nombre in-situ (doble click o botón de edición).
  - Reordenar tareas arriba/abajo o arrastrando.

### US-4.2: Asignación Activa y Atribución Fraccionada
- **Descripción**: Vincular el temporizador a una tarea activa y gestionar cambios durante la sesión.
- **Criterios de Aceptación**:
  - Seleccionar qué tarea se está trabajando actualmente (o "Sin tarea").
  - Al cambiar de tarea a mitad de una sesión, el tiempo transcurrido hasta el cambio se asigna a la tarea anterior y el tiempo restante se asigna a la nueva.
  - El renombrado de una tarea actualiza transparentemente las sesiones registradas en el historial.

### US-4.3: Archivado Suave y Restauración
- **Descripción**: Proteger los datos históricos cuando una tarea ya no se necesita.
- **Criterios de Aceptación**:
  - Al eliminar una tarea, se archiva en lugar de destruirse para que los gráficos históricos sigan mostrando su nombre real.
  - Panel o vista de tareas archivadas con opción de restaurarlas.
  - Botón "Limpiar completadas" para archivar en lote las tareas marcadas.

---

## 📌 Épica 5: Estadísticas e Historial de Productividad

### US-5.1: Historial Rodante de 30 Días
- **Descripción**: Almacenar y consolidar las sesiones completadas durante los últimos 30 días.
- **Criterios de Aceptación**:
  - Registro de cada sesión con fecha, hora, duración planeada, duración real, tipo de sesión y tarea.
  - Umbral mínimo configurable (por defecto 5 min): las sesiones de enfoque interrumpidas antes del umbral se descartan para evitar ruido estadístico.
  - Depuración automática de entradas mayores a 30 días.

### US-5.2: Reloj de 24 Horas / Línea de Tiempo Diaria
- **Descripción**: Visualizar cómo se distribuyó el día de trabajo en un reloj/timeline continuo.
- **Criterios de Aceptación**:
  - Representación de los bloques de sesión situados en su hora exacta de ocurrencia.
  - Colores diferenciados por tarea, bloques de descanso atenuados y huecos visuales para tiempos inactivos.

### US-5.3: Gráfico de Barras Apiladas por Período
- **Descripción**: Comparar el tiempo invertido en los períodos: Hoy, 7 días, 14 días y 30 días.
- **Criterios de Aceptación**:
  - Barras apiladas por día mostrando el tiempo de cada tarea + tiempo de descansos.
  - Línea de referencia horizontal con la jornada laboral objetivo (ej. 8 horas).
  - Paleta armónica determinista para tareas basada en el hash de su título.
  - Indicadores clave: Tiempo de enfoque, tiempo de descansos, tiempo ocioso (`idle`) y porcentaje de objetivo laboral alcanzado.

---

## 📌 Épica 6: Overlay de Descanso a Pantalla Completa (Multi-Monitor)

### US-6.1: Despliegue en Todos los Monitores
- **Descripción**: Bloqueo visual agradable que cubra todas las pantallas conectadas al comenzar un descanso.
- **Criterios de Aceptación**:
  - Detección dinámica y cobertura de todas las pantallas activas (`Screen.AllScreens`).
  - Fondo con desenfoque (Blur / Mica / Acrylic) y opacidad graduable (0% a 100%).
  - Selector de tema propio para el overlay (Claro u Oscuro independiente del sistema operativo).

### US-6.2: Controles y Acciones del Overlay
- **Descripción**: Proporcionar acciones claras durante el descanso.
- **Criterios de Aceptación**:
  - Dial central con tiempo de descanso restante y animación de pulso al agotarse.
  - Botones opcionales configurables: Posponer (+1 min), Saltar descanso, Cerrar overlay (mantiene el descanso en fondo) y Terminar sesión.
  - Opción de mostrar los botones solo al pasar el puntero del ratón (`hover`).
  - Tecla `Escape` cierra el overlay si el botón Cerrar está habilitado.

---

## 📌 Épica 7: Sonidos, Notificaciones y Automatizaciones

### US-7.1: Notificaciones y Chimes por Evento
- **Descripción**: Configuración granular de alertas para los 4 eventos clave:
  1. Inicio de Enfoque.
  2. Fin de Enfoque.
  3. Inicio de Descanso.
  4. Fin de Descanso.
- **Criterios de Aceptación**:
  - Notificaciones interactivas tipo Toast en Windows con botones de acción rápida.
  - Selector de archivos de sonido personalizados (.wav, .mp3, .ogg) e inclusión de sonidos predeterminados elegantes.
  - Control de volumen global (0 a 100%).

### US-7.2: Sonido de Tic-Tac en Recta Final
- **Descripción**: Advertir con sonido de tic-tac cuando una sesión de enfoque está por culminar.
- **Criterios de Aceptación**:
  - Activable/desactivable, con duración configurable (ej. últimos 10 segundos).
  - Rampa de volumen ascendente a medida que se acerca el segundo cero.
  - No aplica a descansos ni a sesiones de flowmodoro.

### US-7.3: Integración de Medios y No Molestar
- **Descripción**: Automatizar la pausa de música y silenciar distracciones.
- **Criterios de Aceptación**:
  - Pausar reproductores multimedia de Windows al entrar en descanso (Windows Media Transport Controls / tecla virtual Play/Pause).
  - Integración con Asistente de Concentración (Focus Assist) de Windows.

### US-7.4: Ejecución de Scripts Externos
- **Descripción**: Ejecutar comandos o scripts personalizados del usuario (.bat, .cmd, .ps1, .exe) en los 5 disparadores:
  - Al iniciar enfoque.
  - Al terminar enfoque.
  - Al iniciar descanso.
  - Al terminar descanso.
  - Al detener el temporizador.

---

## 📌 Épica 8: Configuración, Calidad y Despliegue

### US-8.1: Panel de Preferencias Exhaustivo
- **Descripción**: Interfaz gráfica para configurar todos los parámetros equivalentes a `main.xml`.
- **Criterios de Aceptación**:
  - Agrupación ordenada en pestañas: Temporizador, Notificaciones/Sonido, Overlay, Scripts, Apariencia y General.
  - Opción de inicio automático con Windows (Registro `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`).

### US-8.2: Suite de Pruebas Automatizadas
- **Descripción**: Garantizar la fiabilidad del motor con pruebas unitarias en `Fokus.Tests`.
- **Criterios de Aceptación**:
  - Pruebas equivalentes a `tst_timer.qml` y `tst_tasks.qml`.
  - Cobertura de cálculos de flowmodoro, transiciones de estados, tolerancia a suspensión, filtrado de tareas e intervalos de historial.
