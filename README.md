# Recolección Multiplayer - Entregable Evaluativo

Este proyecto consiste en un videojuego 3D multijugador básico desarrollado en Unity para la materia **Programación en Entornos Virtuales 2** de la Tecnicatura Universitaria en Desarrollo y Producción de Videojuegos.

## 👤 Datos del Alumno
* **Nombre:** Octavio Arnaudo
* **Cohorte:** 2025
* **Comisión:** 1B

## 🎯 Objetivo y Loop General del Juego
El juego cumple con el loop general requerido: iniciar partida, jugar con un sistema de puntuación sincronizado por red, finalizar por tiempo o victoria inmediata, y reiniciar o salir de la aplicación sin presentar bugs bloqueantes.

* **Mecánica Principal:** Los jugadores deben recolectar objetos esparcidos por el mapa que poseen la etiqueta (`Tag`) `"Star"`. Al tomarlos, se activa una indicación visual (aureola) sobre el avatar. Los objetos deben ser trasladados y entregados en la zona central del mapa (`Tag` `"Zone"`) para sumar puntos.
* **Condición de Fin de Partida:** El juego concluye cuando el temporizador de la interfaz llega a `00:00` (actualmente configurado en **1 minuto**) o cuando un equipo logra la victoria inmediata por supremacía de puntos.

## 🚀 Características del Producto Mínimo Logrado (MVP)

1. **Multijugador en Red (Unity Netcode for GameObjects):**
   * Soporte integrado para un mínimo de 2 jugadores en arquitectura Host/Cliente.
   * Conexión directa por dirección IP utilizando `UnityTransport`.
   * Sincronización de variables de red (`NetworkVariable`) para puntaje, ID de equipo e indicador de estrella.
   * Gestión optimizada de recolectables de escena mediante `Despawn(false)` y `ClientRpc` para ocultar visuales sin destruir objetos de jerarquía.
   * Robustez en la gestión de sockets para evitar errores de "Address already in use" mediante validaciones de estado en el `NetworkManager`.

2. **Sistema de Entrada (New Input System):**
   * Control total mediante el *New Input System* (`UnityEngine.InputSystem`).
   * Navegación inteligente con la tecla **Esc**:
     * **1ra pulsación:** Muestra el Scoreboard actualizado en tiempo real con el líder actual.
     * **2da pulsación:** Transiciona al Menú Principal de opciones.
     * **3ra pulsación:** Cierra los menús y devuelve el control al juego (bloqueando el cursor).
   * Prevención de errores de emparejamiento de dispositivos mediante la desactivación controlada del componente `PlayerInput` en clones remotos.

3. **Interfaz de Usuario (UI) y Flujo de Navegación:**
   * **Menú Principal:** Datos del alumno y botones de Host/Cliente/Salir.
   * **Scoreboard Dinámico:** Reutiliza los mensajes de victoria para mostrar quién va liderando la partida en cualquier momento. Al finalizar el tiempo, se muestra permanentemente bloqueando el uso de Esc.
   * **Autocompletado de IP:** Obtiene la IPv4 local de la máquina para facilitar conexiones en red local, permitiendo edición manual para otros destinos.
   * **HUD de Partida:** Cronómetro sincronizado de 1 minuto y marcadores de equipo.

4. **Entorno Gráfico:**
   * Escenario modular con formas geométricas básicas.
   * Diferenciación visual automática: Jugador Par = Azul, Jugador Impar = Rojo.
   * Sistema de aureola visual sincronizado para indicar la posesión de una estrella.

## 📂 Enlaces de Entrega

* **Repositorio en GitHub:** [https://github.com/OctavioArnaudo/EntregableEvaluativo](https://github.com/OctavioArnaudo/EntregableEvaluativo)
* **Build Jugable (Archivo Comprimido):** [Google Drive Link](https://drive.google.com/drive/folders/1mTZRj-PFmwEBHOC-6OTgiyPfqJDYZboQ?usp=sharing)
