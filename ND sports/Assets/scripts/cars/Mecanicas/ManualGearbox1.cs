using UnityEngine;

// Caja manual con embrague para usar JUNTO a Prometeo Car Controller.
// No modifica Prometeo: solo toma el control del torque de los WheelColliders
// después de que Prometeo corre (en LateUpdate).
//
// Controles:
//   W            = acelerar
//   E (mantener) = embrague
//   E + U/J/I/K/O = 1ra / 2da / 3ra / 4ta / 5ta
//   E + L        = reversa
//   E + N        = neutro
//   Espacio      = freno de mano (lo sigue manejando Prometeo)
[DefaultExecutionOrder(1000)] // corre después de Prometeo
[RequireComponent(typeof(Rigidbody))]
public class ManualGearbox : MonoBehaviour
{
    // ---------------------------------------------------------------
    // 1. REFERENCIAS
    // ---------------------------------------------------------------
    [Header("Ruedas motrices")]
    [Tooltip("Los WheelColliders que reciben fuerza. Si lo dejas vacío usa todos los del auto.")]
    public WheelCollider[] drivenWheels;

    // ---------------------------------------------------------------
    // 2. TECLAS
    // ---------------------------------------------------------------
    [Header("Teclas")]
    public KeyCode clutchKey = KeyCode.E;
    public KeyCode gear1Key = KeyCode.U;
    public KeyCode gear2Key = KeyCode.J;
    public KeyCode gear3Key = KeyCode.I;
    public KeyCode gear4Key = KeyCode.K;
    public KeyCode gear5Key = KeyCode.O;
    public KeyCode reverseKey = KeyCode.L;
    public KeyCode neutralKey = KeyCode.N;

    // ---------------------------------------------------------------
    // 3. MOTOR Y CAJA
    // ---------------------------------------------------------------
    [Header("Motor")]
    public float engineTorque = 250f;     // torque máximo del motor (Nm)
    public float idleRpm = 900f;          // RPM en ralentí
    public float redlineRpm = 7000f;      // RPM máximas (corta el motor al llegar)

    // Forma del torque según las RPM (eje X: 0 a 1 = ralentí a línea roja).
    public AnimationCurve torqueCurve = new AnimationCurve(
        new Keyframe(0f, 0.5f),
        new Keyframe(0.55f, 1f),
        new Keyframe(1f, 0.7f));

    [Header("Caja")]
    // Relación de cada marcha: más alta = más fuerza y menos velocidad máxima
    public float[] forwardRatios = { 3.6f, 2.1f, 1.4f, 1.0f, 0.8f }; // 1ra a 5ta
    public float reverseRatio = 3.2f;
    public float finalDrive = 3.9f;       // relación final (diferencial)

    [Header("Otros")]
    public bool active = true;            // ponlo en false si nadie maneja
    public bool showHud = true;           // muestra marcha, RPM y km/h

    // ---------------------------------------------------------------
    // Variables internas
    // ---------------------------------------------------------------
    Rigidbody rb;
    int gear = 0;        // -1 = reversa, 0 = neutro, 1 a 5 = marchas
    float rpm;
    bool clutch;
    float speedKmh;
    float currentTorque;   // torque que mandamos a las ruedas
    bool releaseBrake;     // ¿anular el freno de Prometeo? (reversa)

    // Lectura desde otros scripts (por ejemplo el velocímetro)
    public int Gear => gear;               // -1 = reversa, 0 = neutro, 1 a 5
    public bool ClutchPressed => clutch;
    public float Rpm => rpm;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Si no asignaste ruedas, busca todos los WheelColliders hijos
        if (drivenWheels == null || drivenWheels.Length == 0)
            drivenWheels = GetComponentsInChildren<WheelCollider>();

        rpm = idleRpm;
    }

    // LateUpdate corre DESPUÉS del Update de Prometeo, así que mandamos nosotros.
    void LateUpdate()
    {
        if (!active || drivenWheels.Length == 0) return;

        clutch = Input.GetKey(clutchKey);
        speedKmh = rb.velocity.magnitude * 3.6f;

        HandleShifting();
        ApplyEngine();
    }

    // Cambiar de marcha: solo funciona con el embrague apretado
    void HandleShifting()
    {
        if (!clutch) return;

        if (Input.GetKeyDown(gear1Key)) gear = 1;
        else if (Input.GetKeyDown(gear2Key)) gear = 2;
        else if (Input.GetKeyDown(gear3Key)) gear = 3;
        else if (Input.GetKeyDown(gear4Key)) gear = 4;
        else if (Input.GetKeyDown(gear5Key)) gear = 5;
        else if (Input.GetKeyDown(reverseKey)) gear = -1;
        else if (Input.GetKeyDown(neutralKey)) gear = 0;
    }

    // Relación de la marcha actual (0 en neutro)
    float GetRatio()
    {
        if (gear == 0) return 0f;
        if (gear == -1) return reverseRatio;
        return forwardRatios[gear - 1];
    }

    void ApplyEngine()
    {
        bool throttle = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool brakeKey = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool handbrake = Input.GetKey(KeyCode.Space);

        float ratio = GetRatio();
        bool engaged = gear != 0 && !clutch; // ¿el motor está conectado a las ruedas?

        // RPM promedio de las ruedas (siempre positivo)
        float wheelRpm = 0f;
        foreach (WheelCollider w in drivenWheels) wheelRpm += Mathf.Abs(w.rpm);
        wheelRpm /= drivenWheels.Length;

        // RPM del motor
        float targetRpm;
        if (engaged)
            targetRpm = Mathf.Max(idleRpm, wheelRpm * ratio * finalDrive); // atado a las ruedas
        else
            targetRpm = throttle ? redlineRpm : idleRpm;                    // motor "libre"

        rpm = Mathf.Lerp(rpm, Mathf.Clamp(targetRpm, idleRpm, redlineRpm + 300f), 8f * Time.deltaTime);

        // Torque que llega a cada rueda
        float torque = 0f;
        if (engaged && throttle && rpm < redlineRpm)
        {
            float engineNm = engineTorque * torqueCurve.Evaluate(rpm / redlineRpm);
            float wheelNm = engineNm * ratio * finalDrive;
            torque = wheelNm / drivenWheels.Length;
            if (gear == -1) torque = -torque; // reversa empuja hacia atrás
        }

        currentTorque = torque;

        // Prometeo frena al auto si va hacia atrás y apretamos W.
        // En reversa eso estorba, así que anulamos ese freno.
        releaseBrake = gear == -1 && throttle && !brakeKey && !handbrake;

        ApplyToWheels();
    }

    // Se repite justo antes de cada paso de la física, para que nuestro torque
    // sea siempre el último y Prometeo no pueda mover el auto en neutro.
    void FixedUpdate()
    {
        if (!active || drivenWheels == null || drivenWheels.Length == 0) return;
        ApplyToWheels();
    }

    void ApplyToWheels()
    {
        foreach (WheelCollider w in drivenWheels)
        {
            w.motorTorque = currentTorque;
            if (releaseBrake) w.brakeTorque = 0f;
        }
    }

    // Texto en pantalla con marcha, RPM y velocidad
    void OnGUI()
    {
        if (!active || !showHud) return;

        string g = gear == 0 ? "N" : (gear == -1 ? "R" : gear.ToString());

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.normal.textColor = Color.white;

        string text = "Marcha: " + g + "   RPM: " + Mathf.RoundToInt(rpm) +
                      "   " + Mathf.RoundToInt(speedKmh) + " km/h" +
                      (clutch ? "   [EMBRAGUE]" : "");

        GUI.Label(new Rect(20f, Screen.height - 60f, 700f, 40f), text, style);
    }
}
