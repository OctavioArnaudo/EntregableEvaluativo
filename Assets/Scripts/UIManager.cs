using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        if (NetworkManager.Singleton == null || (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer))
            return;

        if (GameManager.Instance == null) return;
        ActualizarHUD();
    }

    public void IrAlMenuSecundario()
    {
        panelMenuPrincipal?.SetActive(false);
        panelMenuSecundario?.SetActive(true);
        if (ipInputField != null && string.IsNullOrEmpty(ipInputField.text)) ipInputField.text = "192.168.0.197";
    }

    public void RegresarAlMenuPrincipal()
    {
        panelMenuPrincipal?.SetActive(true);
        panelMenuSecundario?.SetActive(false);
    }

    public void IniciarHost()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.Shutdown();

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
        }

        if (NetworkManager.Singleton.StartHost())
        {
            if (ipInputField != null) ipInputField.text = GetLocalIPAddress();
            OcultarTodosLosMenus();
            Debug.Log("[RED] Host iniciado con éxito en IP: " + GetLocalIPAddress());
        }
    }

    public void IniciarCliente()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.Shutdown();

        ConfigurarPrefab();

        string rawInput = (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text)) ? ipInputField.text.Trim() : "127.0.0.1";
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

        // Suscribirse a eventos de conexión
        NetworkManager.Singleton.OnClientConnectedCallback += (id) => {
            Debug.Log($"[RED] ¡CONECTADO! ID: {id}");
            Instantiate(playerPrefab, playerPrefab.transform.position, playerPrefab.transform.rotation);
        };

        if (NetworkManager.Singleton.StartClient())
        {
            OcultarTodosLosMenus();
            Debug.Log($"[RED] Intentando conectar al Host en: {ip}:{port}");
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

        if (GameManager.Instance.juegoTerminado.Value) MostrarFinPartida(GameManager.Instance.ganadorSincronizado.Value);
    }

    private void MostrarFinPartida(TeamId ganador)
    {
        if (panelFinPartida != null && !panelFinPartida.activeSelf)
        {
            panelFinPartida.SetActive(true);
            string m = (ganador == TeamId.Blue) ? "¡EQUIPO AZUL!" : (ganador == TeamId.Red) ? "¡EQUIPO ROJO!" : "¡EMPATE!";
            if (textoGanador != null) textoGanador.text = m;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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
