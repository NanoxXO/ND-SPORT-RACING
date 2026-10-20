using UnityEngine;

public class CarEnterExit : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject player;        // La Capsule (con su camara adentro)
    public GameObject carCamera;     // Camara del auto (hija del auto, desactivada al inicio)
    public CarHandler car;           // El CarHandler del auto
    public Transform exitPoint;      // Opcional: lugar donde aparece el jugador al bajar

    [Header("Config")]
    public KeyCode interactKey = KeyCode.E;
    public float enterDistance = 3f;
    public Vector3 fallbackExitOffset = new Vector3(2f, 0.5f, 0f); // al lado del auto

    private bool inCar = false;

    void Awake()
    {
        if (car == null) car = GetComponent<CarHandler>();
    }

    void Start()
    {
        SetDriving(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(interactKey)) return;

        if (inCar)
        {
            ExitCar();
        }
        else
        {
            float dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist <= enterDistance)
                EnterCar();
        }
    }

    void EnterCar()
    {
        inCar = true;
        player.SetActive(false);   // oculta al jugador y su camara
        SetDriving(true);
    }

    void ExitCar()
    {
        inCar = false;
        SetDriving(false);

        Vector3 pos = exitPoint != null
            ? exitPoint.position
            : transform.TransformPoint(fallbackExitOffset);

        // El CharacterController hay que apagarlo para poder teletransportar
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = pos;
        player.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (cc != null) cc.enabled = true;

        player.SetActive(true);
    }

    void SetDriving(bool driving)
    {
        if (car != null) car.isDriving = driving;
        if (carCamera != null) carCamera.SetActive(driving);
    }
}