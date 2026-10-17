# Fokus para Windows - Backlog de Producto

Este backlog detalla todos los requerimientos y épicas para la construcción de **Fokus para Windows**, con paridad funcional total respecto a [focus-plasmoid](https://gitlab.com/divinae/focus-plasmoid) de KDE Plasma 6 y diseño moderno Windows 11 Fluent.

---

## 📊 Panel de Estado General de Épicas

| Épica | Descripción | Historias | Estado | Cobertura / Archivos Principales |
|---|---|:---:|:---:|---|
| **Épica 1** | Motor del Temporizador y Estado (Core Engine) | 5 / 5 | ✅ Completado | `TimerEngine.cs`, `TimerLogic.cs`, `TimerLogicTests.cs` (79 tests) |
| **Épica 2** | Interfaz en la Bandeja del Sistema (System Tray) | 3 / 3 | ✅ Completado | `TrayManager.cs`, `TrayIconGenerator.cs`, `H.NotifyIcon` |
| **Épica 3** | Flyout Principal y Dial de Control (WPF Fluent) | 2 / 2 | ✅ Completado | `FlyoutWindow.xaml`, `TimerDial.xaml`, `Wpf.Ui` |
| **Épica 4** | Gestor de Tareas Integrado (Task List) | 3 / 3 | ✅ Completado | `TaskManager.cs`, `TaskLogic.cs`, `TaskLogicTests.cs` |
| **Épica 5** | Estadísticas e Historial de Productividad | 3 / 3 | ✅ Completado | `HistoryManager.cs`, `DayClockRing.xaml`, `StackedBarChart.xaml` |
| **Épica 6** | Overlay de Descanso a Pantalla Completa | 2 / 2 | ✅ Completado | `BreakOverlayWindow.xaml`, Multi-monitor (`VirtualScreen`) |
| **Épica 7** | Sonidos, Notificaciones y Automatizaciones | 4 / 4 | ✅ Completado | `SoundService.cs`, `MediaControlService.cs`, `ScriptService.cs` |
| **Épica 8** | Configuración, Calidad y Despliegue | 2 / 2 | ✅ Completado | `StartupService.cs`, `Fokus.Tests` (84 tests passing) |

**Progreso Total**: 24 / 24 Historias de Usuario completadas (**100%**).

---

## 📌 Épica 1: Motor del Temporizador y Estado (Core Engine)

### [x] US-1.1: Máquina de Estados Clásica Pomodoro
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Services/TimerEngine.cs`, `src/Fokus.Core/Utils/TimerLogic.cs`, `tests/Fokus.Tests/TimerLogicTests.cs`
- **Descripción**: Permitir temporizadores con tiempos configurables de enfoque (ej. 25 min), descanso corto (5 min) y descanso largo (20 min) tras N ciclos (ej. 4 sesiones).
- **Criterios de Aceptación**:
  - [x] Transición automática o manual entre estados (Enfoque -> Descanso corto/largo).
  - [x] Presets disponibles: 25/5/20, 50/10/30, 90/20/30 y modo Personalizado.
  - [x] Omitir descansos configurados en 0 minutos sin bloquear la secuencia.
  - [x] Indicador numérico de ciclo actual (ej. sesión 2 de 4).

### [x] US-1.2: Modo Flowmodoro
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Services/TimerEngine.cs`, `src/Fokus.Core/Utils/TimerLogic.cs`, `tests/Fokus.Tests/TimerEngineTests.cs`
- **Descripción**: Permitir trabajar sin límite de tiempo (cuenta progresiva ascendente) y ganar tiempo de descanso en base a un divisor configurable.
- **Criterios de Aceptación**:
  - [x] Cuenta ascendente desde 00:00:00.
  - [x] Cálculo automático del descanso = `Tiempo de flujo / divisor` (por defecto 1/5).
  - [x] Recordatorio sutil periódico configurable (por defecto cada 30m) para evitar olvidar el temporizador activo.

### [x] US-1.3: Control Interactivo y Rueda del Ratón
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.UI/Tray/TrayManager.cs` (`OnTrayMouseWheel`), `src/Fokus.Core/Services/TimerEngine.cs` (`ShiftCounter`, `Postpone`, `Skip`)
- **Descripción**: Ajustar el tiempo restante de forma rápida mediante la rueda del ratón o botones.
- **Criterios de Aceptación**:
  - [x] Scroll del ratón sobre el icono del tray o el dial agrega/resta minutos (+/- 1 min por click estándar de rueda, delta 120).
  - [x] Funciones de Iniciar, Pausar, Reanudar, Detener/Reiniciar y Saltar sesión.
  - [x] Botón "Posponer" (`Postpone`) para añadir tiempo adicional de descanso cuando se necesite.

### [x] US-1.4: Resiliencia ante Reinicios y Suspensión del Sistema
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Services/StorageService.cs`, `src/Fokus.Core/Services/TimerEngine.cs`, `src/Fokus.Core/Utils/TimerLogic.cs` (`IsTimeGap`, `SessionIsStale`)
- **Descripción**: Garantizar que el temporizador sobreviva a reinicios de la aplicación y detecte suspensiones del equipo.
- **Criterios de Aceptación**:
  - [x] Almacenar la fecha límite (`deadline`) en tiempo real para sesiones activas.
  - [x] Restaurar el tiempo correcto al reabrir la app.
  - [x] Si el desfase entre comprobaciones excede el límite de tolerancia (`> 5 min`), identificar que el sistema estuvo suspendido/apagado y evitar contabilizar ese tiempo como concentración real.

### [x] US-1.5: Gestión de Tiempo Extra en Descanso (Overrun)
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Services/TimerEngine.cs` (`BreakOvertime`, `OverrunAlert`), `src/Fokus.UI/Controls/TimerDial.xaml.cs`
- **Descripción**: Si un descanso finaliza y el usuario no regresa de inmediato, el contador continúa en valores negativos.
- **Criterios de Aceptación**:
  - [x] Contador muestra valores negativos (ej. `-01:45`).
  - [x] Pulso visual continuo en la interfaz y repetición configurable del sonido de alerta cada N segundos (por defecto 60s).

---

## 📌 Épica 2: Interfaz en la Bandeja del Sistema (System Tray)

### [x] US-2.1: Renderizado Dinámico de Iconos en Tiempo Real
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.UI/Tray/TrayIconGenerator.cs` (renderizado GDI+ a ICO de 32x32)
- **Descripción**: Mostrar el progreso del temporizador directamente en el icono de la bandeja del sistema.
- **Criterios de Aceptación**:
  - [x] 3 estilos seleccionables:
    1. Marco en forma de tomate con relleno proporcional.
    2. Anillo circular delgado con arco de progreso.
    3. Cuña circular rellena con borde sutil.
  - [x] Opciones de texto en el tray: oculto, visible al lado del icono o sustituir el icono por el tiempo en ejecución.
  - [x] Destello visual en el icono al completarse un ciclo para usuarios que usan modo silencio.

### [x] US-2.2: Menú Contextual (Click Derecho)
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.UI/Tray/TrayManager.cs` (`BuildContextMenu`)
- **Descripción**: Ofrecer acceso rápido a las acciones más comunes sin necesidad de abrir la ventana completa.
- **Criterios de Aceptación**:
  - [x] Selector de Presets de duración (25/5/20, 50/10/30, 90/20/30).
  - [x] Interruptor directo para alternar modo Flowmodoro.
  - [x] Iniciar, pausar, saltar sesión y detener.
  - [x] Acceso directo a Configuración y opción de Salir.

### [x] US-2.3: Tooltip Informativo y Eventos de Click
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.UI/Tray/TrayManager.cs` (`UpdateTrayVisuals`, `ShowFlyout`)
- **Descripción**: Informar del estado al pasar el ratón y abrir la ventana al hacer click.
- **Criterios de Aceptación**:
  - [x] Tooltip enriquecido mostrando: Modo, Estado (Enfoque / Descanso), Tiempo restante, Tarea actual y Ciclo.
  - [x] Click izquierdo abre/cierra de forma fluida el Flyout de control anclado al área de notificación.

---

## 📌 Épica 3: Flyout Principal y Dial de Control (WPF Fluent)

### [x] US-3.1: Dial Central de Tiempo con Estilo Fluent
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.UI/Controls/TimerDial.xaml`, `src/Fokus.UI/Controls/TimerDial.xaml.cs`
- **Descripción**: Dial circular con diseño moderno Glassmorphism / Mica para Windows 11.
- **Criterios de Aceptación**:
  - [x] Anillo de progreso animado y suave con colores temáticos (rojo/tomate para enfoque, verde/azul para descanso).
  - [x] Tipografía clara para los dígitos, con opción de dígitos en negrita y fuente personalizada.
  - [x] Puntos indicadores de ciclo de sesiones (`session dots`).
  - [x] Insignia con el nombre de la tarea en curso.

### [x] US-3.2: Navegación por Pestañas
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.UI/Views/FlyoutWindow.xaml`, `src/Fokus.UI/Views/FlyoutWindow.xaml.cs`
- **Descripción**: Navegación integrada en el Flyout para Temporizador, Tareas, Estadísticas y Configuración.
- **Criterios de Aceptación**:
  - [x] Selector de vista ágil y con transiciones limpias.
  - [x] Integración nativa con el tema oscuro/claro de Windows 11 con `WPF-UI`.

---

## 📌 Épica 4: Gestor de Tareas Integrado (Task List)

### [x] US-4.1: Gestión Básica de Tareas (CRUD)
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Utils/TaskLogic.cs`, `src/Fokus.Core/Services/TaskManager.cs`, `src/Fokus.UI/Views/FlyoutWindow.xaml.cs`
- **Descripción**: Administrar las tareas pendientes directamente desde la aplicación.
- **Criterios de Aceptación**:
  - [x] Agregar tarea nueva rápidamente con Enter o botón.
  - [x] Marcar como completada mediante casilla de verificación.
  - [x] Edición de nombre in-situ y asignación rápida como tarea activa al hacer click.
  - [x] Reordenar tareas arriba/abajo con botones dedicados.

### [x] US-4.2: Asignación Activa y Atribución Fraccionada
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Services/TimerEngine.cs` (`SetTask`), `src/Fokus.Core/Utils/TaskLogic.cs`
- **Descripción**: Vincular el temporizador a una tarea activa y gestionar cambios durante la sesión.
- **Criterios de Aceptación**:
  - [x] Seleccionar qué tarea se está trabajando actualmente (o "Sin tarea").
  - [x] Al cambiar de tarea a mitad de una sesión, el tiempo transcurrido hasta el cambio se asigna a la tarea anterior y el tiempo restante se asigna a la nueva.
  - [x] El renombrado de una tarea actualiza transparentemente las sesiones registradas en el historial.

### [x] US-4.3: Archivado Suave y Restauración
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Utils/TaskLogic.cs` (`RemoveTask`, `RemoveDone`, `RestoreTask`), `tests/Fokus.Tests/TaskLogicTests.cs`
- **Descripción**: Proteger los datos históricos cuando una tarea ya no se necesita.
- **Criterios de Aceptación**:
  - [x] Al eliminar una tarea, se archiva en lugar de destruirse para que los gráficos históricos sigan mostrando su nombre real.
  - [x] Si se vuelve a agregar una tarea con el mismo nombre que una archivada, se restaura automáticamente.
  - [x] Botón "Limpiar completadas" para archivar en lote las tareas marcadas.

---

## 📌 Épica 5: Estadísticas e Historial de Productividad

### [x] US-5.1: Historial Rodante de 30 Días
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Services/HistoryManager.cs`, `src/Fokus.Core/Utils/TimerLogic.cs` (`PruneHistory`, `CountsAsSession`)
- **Descripción**: Almacenar y consolidar las sesiones completadas durante los últimos 30 días.
- **Criterios de Aceptación**:
  - [x] Registro de cada sesión con fecha, hora, duración planeada, duración real, tipo de sesión y tarea.
  - [x] Umbral mínimo configurable (por defecto 5 min): las sesiones de enfoque interrumpidas antes del umbral se descartan para evitar ruido estadístico.
  - [x] Depuración automática de entradas mayores a 30 días.

### [x] US-5.2: Reloj de 24 Horas / Línea de Tiempo Diaria
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.UI/Controls/DayClockRing.xaml`, `src/Fokus.UI/Controls/DayClockRing.xaml.cs`, `src/Fokus.Core/Utils/TimerLogic.cs` (`DayTimeline`)
- **Descripción**: Visualizar cómo se distribuyó el día de trabajo en un reloj/timeline continuo.
- **Criterios de Aceptación**:
  - [x] Representación de los bloques de sesión situados en su hora exacta de ocurrencia en un anillo de 24h.
  - [x] Colores diferenciados por tarea, bloques de descanso atenuados y huecos visuales para tiempos inactivos.

### [x] US-5.3: Gráfico de Barras Apiladas por Período
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.UI/Controls/StackedBarChart.xaml`, `src/Fokus.UI/Controls/StackedBarChart.xaml.cs`, `src/Fokus.Core/Utils/TimerLogic.cs` (`FocusPerDay`, `FocusByTask`)
- **Descripción**: Comparar el tiempo invertido en los períodos: Hoy, 7 días, 14 días y 30 días.
- **Criterios de Aceptación**:
  - [x] Barras apiladas por día mostrando el tiempo de cada tarea + tiempo de descansos.
  - [x] Línea de referencia horizontal con la jornada laboral objetivo (ej. 8 horas).
  - [x] Paleta armónica determinista para tareas basada en el hash de su título (`ColorHelper.cs`).
  - [x] Indicadores clave: Tiempo de enfoque, tiempo de descansos, tiempo ocioso (`idle`) y desglose ordenado por tarea.

---

## 📌 Épica 6: Overlay de Descanso a Pantalla Completa (Multi-Monitor)

### [x] US-6.1: Despliegue en Todos los Monitores
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.UI/Views/BreakOverlayWindow.xaml`, `src/Fokus.UI/Tray/TrayManager.cs` (`ShowBreakOverlays`)
- **Descripción**: Bloqueo visual agradable que cubra todas las pantallas conectadas al comenzar un descanso.
- **Criterios de Aceptación**:
  - [x] Detección dinámica y cobertura de todo el espacio virtual de pantallas (`SystemParameters.VirtualScreen`).
  - [x] Fondo con desenfoque / oscurecimiento y opacidad graduable (0% a 100%).
  - [x] Selector de tema propio para el overlay (Claro u Oscuro independiente del sistema operativo).

### [x] US-6.2: Controles y Acciones del Overlay
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.UI/Views/BreakOverlayWindow.xaml.cs`
- **Descripción**: Proporcionar acciones claras durante el descanso.
- **Criterios de Aceptación**:
  - [x] Dial central con tiempo de descanso restante y animación de pulso al agotarse.
  - [x] Botones opcionales configurables: Posponer (+1 min), Saltar descanso, Minimizar overlay (mantiene el descanso en fondo) y Finalizar hoy.
  - [x] Opción de mostrar los botones solo al pasar el puntero del ratón (`hover`).
  - [x] Tecla `Escape` cierra el overlay si el botón Minimizar/Cerrar está habilitado.

---

## 📌 Épica 7: Sonidos, Notificaciones y Automatizaciones

### [x] US-7.1: Notificaciones y Chimes por Evento
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Services/SystemServices.cs` (`SoundService`), `src/Fokus.UI/Tray/TrayManager.cs` (`ShowNotification`)
- **Descripción**: Configuración granular de alertas para los 4 eventos clave:
  1. Inicio de Enfoque.
  2. Fin de Enfoque.
  3. Inicio de Descanso.
  4. Fin de Descanso.
- **Criterios de Aceptación**:
  - [x] Notificaciones de Windows tipo globo/toast informativo.
  - [x] Selector de archivos de sonido personalizados (.wav) e inclusión de sonidos predeterminados elegantes.
  - [x] Control de volumen global (0 a 100%).

### [x] US-7.2: Sonido de Tic-Tac en Recta Final
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Utils/TimerLogic.cs` (`ShouldTick`, `TickVolume`), `src/Fokus.Core/Services/TimerEngine.cs` (`TickingAlert`), `src/Fokus.Core/Services/SystemServices.cs` (`PlayTick`)
- **Descripción**: Advertir con sonido de tic-tac cuando una sesión de enfoque está por culminar.
- **Criterios de Aceptación**:
  - [x] Activable/desactivable, con duración configurable (ej. últimos 10 segundos).
  - [x] Rampa de volumen ascendente a medida que se acerca el segundo cero (`TickVolume`).
  - [x] No aplica a descansos ni a sesiones de flowmodoro.

### [x] US-7.3: Integración de Medios y No Molestar
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Services/SystemServices.cs` (`MediaControlService` vía `VK_MEDIA_PLAY_PAUSE`), `src/Fokus.UI/Tray/TrayManager.cs`
- **Descripción**: Automatizar la pausa de música y silenciar distracciones.
- **Criterios de Aceptación**:
  - [x] Pausar reproductores multimedia de Windows al entrar en descanso enviando tecla virtual multimedia Play/Pause.

### [x] US-7.4: Ejecución de Scripts Externos
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.Core/Services/SystemServices.cs` (`ScriptService`), `src/Fokus.UI/Tray/TrayManager.cs`
- **Descripción**: Ejecutar comandos o scripts personalizados del usuario (.bat, .cmd, .ps1, .exe) en los disparadores clave:
  - Al iniciar enfoque.
  - Al terminar enfoque.
  - Al iniciar descanso.
  - Al terminar descanso.
  - Al detener el temporizador.
- **Criterios de Aceptación**:
  - [x] Ejecución asíncrona no bloqueante de procesos en segundo plano.

---

## 📌 Épica 8: Configuración, Calidad y Despliegue

### [x] US-8.1: Panel de Preferencias Exhaustivo
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `src/Fokus.UI/Views/FlyoutWindow.xaml` (Tab Ajustes), `src/Fokus.Core/Services/SystemServices.cs` (`StartupService`)
- **Descripción**: Interfaz gráfica para configurar todos los parámetros equivalentes a `main.xml`.
- **Criterios de Aceptación**:
  - [x] Agrupación ordenada en secciones: Temporizador, Overlay, Sonidos/Notificaciones y Apariencia/Sistema.
  - [x] Opción de inicio automático con Windows (Registro `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`).

### [x] US-8.2: Suite de Pruebas Automatizadas
- **Estado**: ✅ Implementado y Validado
- **Archivos**: `tests/Fokus.Tests/TimerLogicTests.cs`, `tests/Fokus.Tests/TaskLogicTests.cs`, `tests/Fokus.Tests/TimerEngineTests.cs`
- **Descripción**: Garantizar la fiabilidad del motor con pruebas unitarias en `Fokus.Tests`.
- **Criterios de Aceptación**:
  - [x] Pruebas equivalentes a `tst_timer.qml` y `tst_tasks.qml`.
  - [x] Cobertura de cálculos de flowmodoro, transiciones de estados, tolerancia a suspensión, filtrado de tareas e intervalos de historial (**84 pruebas unitarias pasando al 100%**).
