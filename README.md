# Recolección Multiplayer - Entregable Evaluativo

Este proyecto consiste en un videojuego 3D multijugador básico desarrollado en Unity para la materia **Programación en Entornos Virtuales 2** de la Tecnicatura Universitaria en Desarrollo y Producción de Videojuegos.

## 👤 Datos del Alumno
* **Nombre:** Octavio Arnaudo
* **Cohorte:** 2025
* **Comisión:** 1B

## 🎯 Objetivo y Loop General del Juego
El juego cumple con el loop general requerido: iniciar partida, jugar con un sistema de puntuación sincronizado por red, finalizar por tiempo o victoria inmediata, y reiniciar o salir de la aplicación sin presentar bugs bloqueantes.

* **Mecánica Principal:** Los jugadores deben recolectar objetos esparcidos por el mapa que poseen la etiqueta (`Tag`) `"Star"`. Al tomarlos, se activa una indicación visual (aureola) sobre el avatar. Los objetos deben ser trasladados y entregados en la zona central del mapa (`Tag` `"Zone"`) para sumar puntos.
* **Condición de Fin de Partida:** El juego concluye cuando el temporizador de la interfaz llega a `00:00` o cuando un equipo logra la victoria inmediata por supremacía de puntos ("mitad más uno" de las estrellas totales disponibles en el mapa).

## 🚀 Características del Producto Mínimo Logrado (MVP)

1. **Multijugador en Red (Unity Netcode for GameObjects):**
   * Soporte integrado para un mínimo de 2 jugadores en arquitectura Host/Cliente.
   * Conexión directa por dirección IP utilizando `UnityTransport`.
   * Sincronización de variables de red (`NetworkVariable`) públicas para el seguimiento del puntaje e ID de equipo de cada jugador conectado.
   * Gestión remota segura del ciclo de vida de las entidades en red a través de comandos RPC unificados (`[Rpc(SendTo.Server)]`).

2. **Sistema de Entrada (New Input System):**
   * Control y lectura directa de periféricos mediante las API modernas del *New Input System* (`Keyboard.current` / `Gamepad.current`).
   * Procesamiento optimizado del desplazamiento del avatar en un entorno 3D mediante físicas aplicadas en el ciclo `FixedUpdate`.

3. **Interfaz de Usuario (UI) y Flujo de Navegación:**
   * **Menú Principal:** Pantalla de inicio con los datos reglamentarios del alumno visibles. Contiene botones dedicados para iniciar como *Host*, transicionar a la pantalla de configuración de *Cliente* o *Salir* del juego.
   * **Autocompletado de IP:** Si el jugador decide ser *Host*, el sistema obtiene de forma nativa la IPv4 local de la máquina (`System.Net`) y la escribe por defecto en el `InputField` como referencia visual.
   * **HUD de Partida:** Muestra en tiempo real el cronómetro regresivo de 3 minutos formateado y el marcador de puntos por equipos formateado estrictamente en **dos cifras** (`00`).
   * **Pantalla de Fin de Partida:** Despliega un panel con el ganador definitivo (`EQUIPO AZUL` / `EQUIPO ROJO` / `EMPATE`) basándose en la paridad del `OwnerClientId` del jugador de red, acompañado de botones interactivos para reiniciar la escena local limpiando sockets de red o salir de la aplicación.

4. **Entorno Gráfico:**
   * Escenario modular construido mediante formas geométricas básicas en 3D (bloques, esferas, cilindros).
   * Diferenciación visual automática del color del material de los avatares según su equipo asignado por ID de red (Par = Azul, Impar = Rojo).

## 📂 Enlaces de Entrega

* **Repositorio en GitHub:** [https://github.com/OctavioArnaudo/EntregableEvaluativo](https://github.com/OctavioArnaudo/EntregableEvaluativo)
* **Build Jugable (Archivo Comprimido):** [Google Drive Link](https://drive.google.com/drive/folders/1mTZRj-PFmwEBHOC-6OTgiyPfqJDYZboQ?usp=sharing)
