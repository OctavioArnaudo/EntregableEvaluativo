using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : NetworkBehaviour
{
    [Header("Ajustes de Movimiento (Planos)")]
    [SerializeField] private float velocidad = 5f;
    [SerializeField] private float sensibilidad = 0.5f;

    [Header("Ajustes de Cámara (Eje Y Bloqueado)")]
    [SerializeField] private float alturaFijaCamara = 2.0f;
    [SerializeField] private float distanciaTraseraCamara = -4.0f;

    [Header("Referencias")]
    [SerializeField] private GameObject aureolaVisual;

    private Transform camaraTransform;
    private Rigidbody rb;
    private Vector2 moveInput;
    private float rotY, rotX;

    [Header("Sincronización")]
    public NetworkVariable<int> puntaje = new NetworkVariable<int>(0);
    public NetworkVariable<int> idEquipo = new NetworkVariable<int>(-1);
    public NetworkVariable<bool> tieneEstrella = new NetworkVariable<bool>(false);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Bloqueo físico absoluto: no rota en X ni Z
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        rb.isKinematic = true;

        if (TryGetComponent<PlayerInput>(out var input))
            input.enabled = false;
    }

    private void Start()
    {
        if (aureolaVisual != null) aureolaVisual.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            rb.isKinematic = false;
            if (TryGetComponent<PlayerInput>(out var input)) input.enabled = true;
            if (camaraTransform == null) camaraTransform = Camera.main.transform;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log($"[CLIENTE] He spawneado correctamente. Controlando jugador ID: {OwnerClientId}");
        }
        else
        {
            rb.isKinematic = true;
        }

        if (IsServer)
        {
            idEquipo.Value = (int)(OwnerClientId % 2);
        }

        tieneEstrella.OnValueChanged += (oldVal, newVal) => { if (aureolaVisual != null) aureolaVisual.SetActive(newVal); };
        idEquipo.OnValueChanged += (oldVal, newVal) => ActualizarVisuales();

        ActualizarVisuales();
        if (aureolaVisual != null) aureolaVisual.SetActive(tieneEstrella.Value);
    }

    private void ActualizarVisuales()
    {
        if (idEquipo.Value < 0) return;
        Color color = (idEquipo.Value == 0) ? Color.blue : Color.red;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (aureolaVisual != null && r.gameObject == aureolaVisual) continue;
            r.material.color = color;
        }
    }

    public void OnMove(InputValue v) { if (IsOwner) moveInput = v.Get<Vector2>(); }

    public void OnLook(InputValue v)
    {
        if (!IsOwner || Cursor.lockState != CursorLockMode.Locked) return;
        Vector2 look = v.Get<Vector2>() * sensibilidad;

        // Acumulamos rotación
        rotY += look.x;
        rotX = Mathf.Clamp(rotX - look.y, -30, 60);
    }

    private void FixedUpdate()
    {
        if (!IsOwner || Cursor.lockState != CursorLockMode.Locked) return;

        // 2. ROTACIÓN DEL CUERPO: Solo sobre el eje Y (Giro horizontal)
        // Se establece explícitamente (0, rotY, 0). Jamás habrá inclinación.
        rb.MoveRotation(Quaternion.Euler(0, rotY, 0));

        // 3. MOVIMIENTO PLANO: Calculamos dirección y FORZAMOS eje Y a cero
        Vector3 forwardXZ = transform.forward;
        forwardXZ.y = 0;
        forwardXZ.Normalize();

        Vector3 rightXZ = transform.right;
        rightXZ.y = 0;
        rightXZ.Normalize();

        Vector3 moveDir = (forwardXZ * moveInput.y + rightXZ * moveInput.x);

        // 4. APLICACIÓN DE VELOCIDAD: Solo sobreescribimos X y Z.
        // El eje Y de la velocidad se mantiene igual al actual (gravedad controlada por Unity)
        rb.linearVelocity = new Vector3(moveDir.x * velocidad, rb.linearVelocity.y, moveDir.z * velocidad);
    }

    private void LateUpdate()
    {
        if (!IsOwner || camaraTransform == null || camaraTransform == transform) return;

        // 5. POSICIÓN DE CÁMARA INMUNE AL MOVIMIENTO VERTICAL DEL MOUSE:
        // Calculamos la órbita horizontal usando solo rotY
        Vector3 offsetHorizontal = Quaternion.Euler(0, rotY, 0) * new Vector3(0, 0, distanciaTraseraCamara);

        // La altura (Y) de la cámara es SIEMPRE la del jugador + alturaFijaCamara
        // Nada del mouse puede cambiar la posición vertical de la cámara.
        camaraTransform.position = transform.position + offsetHorizontal + Vector3.up * alturaFijaCamara;

        // 6. ROTACIÓN DE CÁMARA: La cámara puede rotar para mirar arriba/abajo (rotX)
        // pero su POSICIÓN ya quedó bloqueada arriba.
        camaraTransform.rotation = Quaternion.Euler(rotX, rotY, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        if (other.CompareTag("Star") && !tieneEstrella.Value)
        {
            if (other.TryGetComponent<NetworkObject>(out var netObj))
                RecogerEstrellaServerRpc(netObj.NetworkObjectId);
        }
        else if (other.CompareTag("Zone") && tieneEstrella.Value)
        {
            EntregarEstrellaServerRpc();
        }
    }

    [ServerRpc]
    private void RecogerEstrellaServerRpc(ulong starId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(starId, out var star))
        {
            tieneEstrella.Value = true;
            // Avisar a todos los clientes que oculten la estrella antes de despawnear
            OcultarEstrellaClientRpc(starId);

            // Despawn(false) para objetos de escena evita la advertencia y errores de jerarquía
            star.Despawn(false);
            star.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    private void OcultarEstrellaClientRpc(ulong starId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(starId, out var star))
        {
            star.gameObject.SetActive(false);
        }
    }

    [ServerRpc]
    private void EntregarEstrellaServerRpc()
    {
        tieneEstrella.Value = false;
        puntaje.Value++;
    }
}
