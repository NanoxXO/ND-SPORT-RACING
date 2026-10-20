using UnityEngine;

public class CameraToggle : MonoBehaviour
{
    [Header("Referencias (si las dejas vacias, se buscan solas)")]
    public Transform playerBody;      // Objeto que gira en Y (la Capsule)
    public Transform cameraPivot;     // Empty a la altura de los ojos
    public Camera cam;                // La camara (hija de cameraPivot)
    public Renderer[] bodyRenderers;  // Mallas del personaje (para ocultarlas en 1a persona)

    [Header("Config")]
    public KeyCode toggleKey = KeyCode.V;
    public float sensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Tercera persona")]
    public float thirdPersonDistance = 3.5f;
    public Vector3 thirdPersonOffset = new Vector3(0.4f, 0.2f, 0f);
    public float transitionSpeed = 12f;
    public LayerMask collisionMask = ~0;   // desmarca la capa Player
    public float collisionPadding = 0.05f;

    private bool isFirstPerson = true;
    private float pitch;

    void Awake()
    {
        AutoAssignReferences();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetBodyVisible(!isFirstPerson);
    }

    void AutoAssignReferences()
    {
        if (playerBody == null)
            playerBody = transform;

        if (cam == null)
            cam = GetComponentInChildren<Camera>();

        if (cameraPivot == null && cam != null && cam.transform.parent != null
            && cam.transform.parent != transform)
            cameraPivot = cam.transform.parent;

        if (bodyRenderers == null || bodyRenderers.Length == 0
            || System.Array.Exists(bodyRenderers, r => r == null))
            bodyRenderers = playerBody.GetComponentsInChildren<Renderer>();

        if (cam == null || cameraPivot == null)
            Debug.LogError("CameraToggle: falta la camara o el pivote. Revisa Capsule > camaraPivote > MainCamera.");
    }

    void Update()
    {
        if (cam == null || cameraPivot == null) return;

        if (Input.GetKeyDown(toggleKey))
        {
            isFirstPerson = !isFirstPerson;
            SetBodyVisible(!isFirstPerson);
        }

        // Escape libera el mouse, click lo vuelve a capturar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        playerBody.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void LateUpdate()
    {
        if (cam == null || cameraPivot == null) return;

        Vector3 desiredLocal = isFirstPerson
            ? Vector3.zero
            : thirdPersonOffset + new Vector3(0f, 0f, -thirdPersonDistance);

        // Colision de camara en 3a persona
        if (!isFirstPerson)
        {
            Vector3 origin = cameraPivot.position;
            Vector3 desiredWorld = cameraPivot.TransformPoint(desiredLocal);

            if (Physics.Linecast(origin, desiredWorld, out RaycastHit hit, collisionMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 safeWorld = hit.point + hit.normal * collisionPadding;
                desiredLocal = cameraPivot.InverseTransformPoint(safeWorld);
            }
        }

        cam.transform.localPosition = Vector3.Lerp(
            cam.transform.localPosition, desiredLocal, Time.deltaTime * transitionSpeed);
        cam.transform.localRotation = Quaternion.identity;
    }

    void SetBodyVisible(bool visible)
    {
        if (bodyRenderers == null) return;

        var mode = visible
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        foreach (var r in bodyRenderers)
        {
            if (r == null) continue;
            r.shadowCastingMode = mode;
        }
    }
}