using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Net;
using System.Net.Sockets;

public class UIManager : MonoBehaviour
{
    [Header("Configuración de Red")]
    [Tooltip("Arrastra aquí el Prefab del Jugador para que se asigne automáticamente.")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Navegación de Menús")]
    [SerializeField] private GameObject panelMenuPrincipal;
    [SerializeField] private GameObject panelMenuSecundario;
    [SerializeField] private TMP_InputField ipInputField;

    [Header("HUD de Juego")]
    [SerializeField] private TMP_Text textoAzul;
    [SerializeField] private TMP_Text textoRojo;
    [SerializeField] private TMP_Text textoTimer;

    [Header("Fin de Partida")]
    [SerializeField] private GameObject panelFinPartida;
    [SerializeField] private TMP_Text textoGanador;

    private void Start()
    {
        if (panelMenuPrincipal != null) panelMenuPrincipal.SetActive(true);
        if (panelMenuSecundario != null) panelMenuSecundario.SetActive(false);
        if (panelFinPartida != null) panelFinPartida.SetActive(false);
    }

    private void Update()
    {
        // Usar el New Input System para detectar la tecla Escape
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Caso 1: Estamos jugando (ningún panel principal/fin activo) -> Mostrar Scoreboard (panelFinPartida)
            if (!panelFinPartida.activeSelf && !panelMenuPrincipal.activeSelf && !panelMenuSecundario.activeSelf)
            {
                if (panelFinPartida != null)
                {
                    panelFinPartida.SetActive(true);
                    ActualizarTextoScoreboard();
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            // Caso 2: El Scoreboard está abierto
            else if (panelFinPartida.activeSelf)
            {
                // Si la partida terminó de verdad, el Esc no hace nada (obligamos a elegir opción)
                if (GameManager.Instance != null && GameManager.Instance.juegoTerminado.Value) return;

                // Si no ha terminado, pasamos al Menú Principal
                panelFinPartida.SetActive(false);
                if (panelMenuPrincipal != null) panelMenuPrincipal.SetActive(true);
            }
            // Caso 3: El Menú Principal está abierto -> Volver al juego
            else if (panelMenuPrincipal.activeSelf)
            {
                OcultarTodosLosMenus();
                if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
            // Caso 4: Menú secundario (IP) -> Volver al Principal
            else if (panelMenuSecundario.activeSelf)
            {
                RegresarAlMenuPrincipal();
            }
        }

        if (NetworkManager.Singleton == null || (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer))
            return;

        if (GameManager.Instance == null) return;
        ActualizarHUD();
    }

    private void ActualizarTextoScoreboard()
    {
        if (GameManager.Instance == null || textoGanador == null) return;

        if (GameManager.Instance.juegoTerminado.Value)
        {
            TeamId ganador = GameManager.Instance.ganadorSincronizado.Value;
            textoGanador.text = (ganador == TeamId.Blue) ? "¡EQUIPO AZUL!" : (ganador == TeamId.Red) ? "¡EQUIPO ROJO!" : "¡EMPATE!";
        }
        else
        {
            var p = GameManager.Instance.CalcularPuntajes();
            if (p.azul > p.rojo) textoGanador.text = "¡EQUIPO AZUL!";
            else if (p.rojo > p.azul) textoGanador.text = "¡EQUIPO ROJO!";
            else textoGanador.text = "¡EMPATE!";
        }
    }

    public void IrAlMenuSecundario()
    {
        panelMenuPrincipal?.SetActive(false);
        panelMenuSecundario?.SetActive(true);
        if (ipInputField != null && string.IsNullOrEmpty(ipInputField.text)) ipInputField.text = GetLocalIPAddress();
    }

    public void RegresarAlMenuPrincipal()
    {
        panelMenuPrincipal?.SetActive(true);
        panelMenuSecundario?.SetActive(false);
    }

    public void IniciarHost()
    {
        if (NetworkManager.Singleton == null) return;

        // Si ya estamos en una sesión (Host o Cliente), no intentamos iniciar de nuevo.
        // Esto evita el error de "Socket already in use" al pulsar el botón repetidamente.
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[RED] Ya hay una sesión activa. Ocultando menús...");
            OcultarTodosLosMenus();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }

        ConfigurarPrefab();

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            // El Host escucha en 127.0.0.1 para sí mismo y 0.0.0.0 para aceptar conexiones externas
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
        }

        if (NetworkManager.Singleton.StartHost())
        {
            if (ipInputField != null) ipInputField.text = GetLocalIPAddress();
            OcultarTodosLosMenus();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("[RED] Host iniciado con éxito.");
        }
        else
        {
            Debug.LogError("[RED] No se pudo iniciar el Host. Asegúrate de que no haya otro juego abierto usando el puerto 7777.");
        }
    }

    public void IniciarCliente()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();

        ConfigurarPrefab();

        string rawInput = (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text)) ? ipInputField.text.Trim() : GetLocalIPAddress();
        string ip = rawInput;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        ushort port = (transport != null) ? transport.ConnectionData.Port : (ushort)7777;

        if (rawInput.Contains(":"))
        {
            string[] parts = rawInput.Split(':');
            ip = parts[0];
            if (parts.Length > 1 && ushort.TryParse(parts[1], out ushort parsedPort))
            {
                port = parsedPort;
            }
        }

        if (transport != null)
        {
            transport.SetConnectionData(ip, port);
        }

        if (NetworkManager.Singleton.StartClient())
        {
            OcultarTodosLosMenus();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log($"[RED] Intentando conectar a: {ip}:{port}");
        }
        else
        {
            Debug.LogError("[RED] No se pudo iniciar el Cliente.");
        }
    }

    private void ConfigurarPrefab()
    {
        if (NetworkManager.Singleton == null) return;

        if (playerPrefab != null)
        {
            bool exists = false;
            foreach (var p in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
            {
                if (p.Prefab == playerPrefab) { exists = true; break; }
            }
            if (!exists) NetworkManager.Singleton.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = playerPrefab });
            NetworkManager.Singleton.NetworkConfig.PlayerPrefab = playerPrefab;
        }
    }

    private void OcultarTodosLosMenus()
    {
        panelMenuPrincipal?.SetActive(false);
        panelMenuSecundario?.SetActive(false);
    }

    private void ActualizarHUD()
    {
        float t = Mathf.Max(0, GameManager.Instance.tiempoSincronizado.Value);
        if (textoTimer != null) textoTimer.text = string.Format("{0:0}:{1:00}", Mathf.FloorToInt(t / 60), Mathf.FloorToInt(t % 60));

        var puntajes = GameManager.Instance.CalcularPuntajes();
        if (textoAzul != null) textoAzul.text = $"EQUIPO AZUL: {puntajes.azul}";
        if (textoRojo != null) textoRojo.text = $"EQUIPO ROJO: {puntajes.rojo}";

        // Si el panel de fin/score está activo, mantenemos el texto actualizado
        if (panelFinPartida != null && panelFinPartida.activeSelf) ActualizarTextoScoreboard();

        if (GameManager.Instance.juegoTerminado.Value) MostrarFinPartida();
    }

    private void MostrarFinPartida()
    {
        if (panelFinPartida != null)
        {
            if (!panelFinPartida.activeSelf)
            {
                panelFinPartida.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            ActualizarTextoScoreboard();
        }
    }

    public void JugarDeNuevo() { NetworkManager.Singleton?.Shutdown(); SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void SalirDelJuego() { NetworkManager.Singleton?.Shutdown(); Application.Quit();
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private string GetLocalIPAddress()
    {
        try {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
        } catch { }
        return "127.0.0.1";
    }
}
