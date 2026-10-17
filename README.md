# Fokus para Windows 🍅⏱️

Una alternativa nativa para Windows (Windows 11 / 10) inspirada en el temporizador pomodoro y flowmodoro [focus-plasmoid](https://gitlab.com/divinae/focus-plasmoid) de KDE Plasma 6.

Diseñada para vivir en la **Bandeja del Sistema (System Tray)** con diseño moderno **Windows 11 Fluent / Glassmorphism**, modo oscuro, paridad funcional del 100% y rendimiento nativo en **.NET 10**.

---

## ✨ Características Principales

### 🍅 1. Dos Modos de Trabajo
- **Pomodoro Clásico**:
  - Tiempo de enfoque (ej. 25m), descansos cortos (5m) y descanso largo (20m) tras un ciclo de sesiones (ej. 4).
  - Presets rápidos accesibles desde el menú contextual del tray: `25 / 5 / 20`, `50 / 10 / 30`, `90 / 20 / 30` y personalizado.
  - Omisión inteligente de descansos configurados en 0 minutos.
- **Flowmodoro**:
  - Trabaja en estado de flujo sin cuenta regresiva (cuenta progresiva hacia arriba).
  - El descanso se calcula automáticamente como una fracción del tiempo de flujo (por defecto `1/5`).
  - Recordatorio sutil periódico configurable (por defecto cada 30m) para evitar olvidos.

### 🔔 2. Icono Dinámico en la Bandeja del Sistema (System Tray)
- **3 estilos gráficos** seleccionables:
  1. *Silueta de Tomate* con llenado progresivo.
  2. *Anillo circular delgado* con arco de progreso.
  3. *Cuña / queso circular* con contorno sutil.
- Contador de tiempo en el tooltip o swap directo del icono por dígitos al correr.
- Parpadeo visual suave al finalizar cada sesión (ideal para modo silencio).
- **Rueda del ratón (Scroll wheel)** sobre el icono para sumar o restar minutos (+/- 1 min).
- Click izquierdo abre el panel de control desplegable anclado sobre la barra de tareas.
- Click derecho con menú contextual completo (presets, toggle flowmodoro, iniciar/pausar, configuración, salir).

### 🖥️ 3. Overlay de Descanso a Pantalla Completa (Multi-Monitor)
- Bloqueo visual agradable que cubre **todos los monitores conectados**.
- Fondo con desenfoque / acrílico y opacidad graduable (0% a 100%).
- Esquema de color independiente (Tema Claro u Oscuro).
- Dial central con tiempo de descanso restante y animación suave de pulso en tiempo extra.
- Botones de acción opcionales (o revelados solo al pasar el cursor):
  - **Posponer (+1 min)**
  - **Saltar descanso**
  - **Minimizar / Cerrar** (o tecla `Esc`)
  - **Finalizar por hoy**

### 📋 4. Gestor de Tareas Integrado (To-Do List)
- Agregar, marcar como completadas y reordenar tareas arriba/abajo.
- Selección de tarea activa vinculada al temporizador en caliente.
- **Atribución fraccionada**: Si cambias de tarea a mitad de sesión, el tiempo ya trabajado se atribuye a la tarea anterior y el resto a la nueva.
- **Archivado suave**: Al eliminar una tarea, se archiva internamente para que los informes históricos conserven su nombre real.

### 📊 5. Estadísticas de Productividad
- **Reloj de 24 Horas / Línea de Tiempo del Día**: Muestra el día como un reloj continuo con cada sesión en su hora exacta, colores pastel por tarea, descansos atenuados y espacios vacíos para tiempos inactivos.
- **Gráfico de Barras Apiladas por Período**: Vista para Hoy, 7 días, 14 días y 30 días con línea de referencia para la jornada laboral objetivo (ej. 8 horas).
- Métricas consolidadas: Tiempo total enfocado, tiempo en descansos, tiempo ocioso y desglose ordenado por tarea con paleta determinista.
- Historial rodante automático de 30 días almacenado localmente en `%LOCALAPPDATA%\Fokus\`.

### 🔊 6. Sonidos, Alertas y Automatizaciones
- Eventos independientes: Inicio/Fin de Enfoque e Inicio/Fin de Descanso.
- Sonido de tic-tac en los últimos segundos de concentración con rampa de volumen ascendente.
- Alerta repetitiva durante tiempo extra en descansos (`overrun repeat`).
- **Pausa automática de música**: Envía señal de pausa a reproductores multimedia de Windows al comenzar un descanso.
- **Ejecución de scripts externos**: Lanza scripts `.bat`, `.ps1` o `.exe` al iniciar/terminar enfoque, descanso o detener.
- **Inicio con Windows**: Opción en configuración para iniciar automáticamente con el sistema.

---

## 🛠️ Tecnologías Utilizadas

- **.NET 10** (`net10.0-windows`)
- **WPF (Windows Presentation Foundation)**
- **WPF-UI 4.3** (Lepo) - Fluent System Icons, Mica/Acrylic, Dark Theme.
- **H.NotifyIcon 2.4** - Gestión moderna del área de notificación de Windows y eventos de rueda del ratón.
- **System.Drawing.Common** - Renderizado GDI+ en tiempo real de iconos dinámicos ICO.
- **xUnit** - Suite de pruebas unitarias automatizadas.

---

## 🚀 Compilación y Ejecución

### Prerrequisitos
- Windows 10 (versión 1809+) o Windows 11.
- .NET 10 SDK (o .NET 8/10 SDK).

### Ejecutar la aplicación
```bash
dotnet run --project src/Fokus.UI/Fokus.UI.csproj
```

### Ejecutar las pruebas unitarias
```bash
dotnet test
```

### Publicar ejecutable optimizado
```bash
dotnet publish src/Fokus.UI/Fokus.UI.csproj -c Release -r win-x64 --self-contained false -o ./publish
```

---

## 📄 Licencia y Créditos
Inspirado en el trabajo de Jakub Lipiński en [focus-plasmoid](https://gitlab.com/divinae/focus-plasmoid).
