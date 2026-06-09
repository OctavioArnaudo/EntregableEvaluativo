using Unity.Netcode;
using UnityEngine;

public enum TeamId
{
    None = 0,
    Blue = 1,
    Red = 2,
    Tie = 3
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuración de Partida")]
    [SerializeField] private float tiempoRestante = 60f;
    [SerializeField] private int puntosParaVictoriaInmediata = 26;

    // Variables sincronizadas para que todos los clientes vean lo mismo
    public NetworkVariable<float> tiempoSincronizado = new NetworkVariable<float>(60f);
    public NetworkVariable<bool> juegoTerminado = new NetworkVariable<bool>(false);
    public NetworkVariable<TeamId> ganadorSincronizado = new NetworkVariable<TeamId>(TeamId.None);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            tiempoSincronizado.Value = tiempoRestante;
            juegoTerminado.Value = false;
            ganadorSincronizado.Value = TeamId.None;
        }
    }

    private void Update()
    {
        if (!IsServer || juegoTerminado.Value) return;

        // Lógica del cronómetro en el servidor
        if (tiempoSincronizado.Value > 0)
        {
            tiempoSincronizado.Value -= Time.deltaTime;
        }
        else
        {
            FinalizarPartidaPorTiempo();
        }

        // Comprobación de victoria inmediata
        VerificarVictoriaInmediata();
    }

    private void VerificarVictoriaInmediata()
    {
        var totales = CalcularPuntajes();
        if (totales.azul >= puntosParaVictoriaInmediata) FinalizarPartida(TeamId.Blue);
        else if (totales.rojo >= puntosParaVictoriaInmediata) FinalizarPartida(TeamId.Red);
    }

    private void FinalizarPartidaPorTiempo()
    {
        var totales = CalcularPuntajes();
        TeamId ganador = TeamId.Tie;
        if (totales.azul > totales.rojo) ganador = TeamId.Blue;
        else if (totales.rojo > totales.azul) ganador = TeamId.Red;

        FinalizarPartida(ganador);
    }

    private void FinalizarPartida(TeamId ganador)
    {
        juegoTerminado.Value = true;
        ganadorSincronizado.Value = ganador;
    }

    public (int azul, int rojo) CalcularPuntajes()
    {
        int azul = 0, rojo = 0;
        PlayerController[] jugadores = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in jugadores)
        {
            if (p.idEquipo.Value == 0) azul += p.puntaje.Value;
            else rojo += p.puntaje.Value;
        }
        return (azul, rojo);
    }
}
