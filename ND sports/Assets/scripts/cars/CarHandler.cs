using UnityEngine;

// Controlador de auto con WheelColliders, tracción trasera (RWD).
// Estilo mixto: agarre alto para rally + freno de mano y "power slide" para drift.
[RequireComponent(typeof(Rigidbody))]
public class CarHandler : MonoBehaviour
{
    // ---------------------------------------------------------------
    // 1. REFERENCIAS (se asignan en el Inspector)
    // ---------------------------------------------------------------
    [Header("Referencias")]
    [SerializeField] Rigidbody rb;

    // Los 4 WheelCollider (la física). Deben ser hijos del auto, NO de la malla de la rueda.
    [SerializeField] WheelCollider frontLeft;
    [SerializeField] WheelCollider frontRight;
    [SerializeField] WheelCollider rearLeft;
    [SerializeField] WheelCollider rearRight;

    // Las 4 mallas visuales de las ruedas (solo se mueven para que se vean girar).
    [SerializeField] Transform frontLeftMesh;
    [SerializeField] Transform frontRightMesh;
    [SerializeField] Transform rearLeftMesh;
    [SerializeField] Transform rearRightMesh;

    // ---------------------------------------------------------------
    // 2. MOTOR Y FRENOS
    // ---------------------------------------------------------------
    [Header("Motor y frenos")]
    [SerializeField] float motorTorque = 1200f;       // fuerza del motor por rueda trasera
    [SerializeField] float brakeTorque = 3500f;       // fuerza del freno normal
    [SerializeField] float handbrakeTorque = 8000f;   // freno de mano (solo ruedas traseras)
    [SerializeField] float maxSpeedKmh = 180f;        // velocidad máxima hacia adelante
    [SerializeField] float maxReverseSpeedKmh = 40f;  // velocidad máxima en reversa

    // ---------------------------------------------------------------
    // 3. DIRECCIÓN
    // ---------------------------------------------------------------
    [Header("Dirección")]
    [SerializeField] float maxSteerAngle = 32f;       // ángulo máximo a baja velocidad
    [SerializeField] float minSteerAngle = 8f;        // ángulo máximo a alta velocidad
    [SerializeField] float steerFullSpeedKmh = 120f;  // a esta velocidad se usa minSteerAngle
    [SerializeField] float steerSmoothing = 8f;       // qué tan rápido gira el volante
    [SerializeField, Range(0f, 1f)] float counterSteerAssist = 0.6f; // ayuda a contravolantear en drift

    // ---------------------------------------------------------------
    // 4. AGARRE (aquí se define el estilo rally/drift)
    // ---------------------------------------------------------------
    [Header("Agarre")]
    [SerializeField] float frontGrip = 2.0f;          // agarre lateral delantero
    [SerializeField] float rearGrip = 1.7f;           // agarre lateral trasero normal
    [SerializeField] float rearDriftGrip = 0.7f;      // agarre trasero con freno de mano (drift)
    [SerializeField] float throttleSlide = 0.25f;     // pérdida de agarre al acelerar girando (power slide)
    [SerializeField] float gripLossSpeed = 12f;       // qué tan rápido se pierde agarre
    [SerializeField] float gripRecoverSpeed = 2f;     // qué tan lento se recupera (drift más largo = valor bajo)

    // Multiplicador de superficie: 1 = asfalto, ~0.7 = tierra, ~0.5 = nieve/hielo.
    // Otro script (por ejemplo un detector de terreno) puede cambiarlo en tiempo real.
    public float surfaceGrip = 1f;

    // ---------------------------------------------------------------
    // 5. ESTABILIDAD
    // ---------------------------------------------------------------
    [Header("Estabilidad")]
    [SerializeField] Vector3 centerOfMassOffset = new Vector3(0f, -0.3f, 0f); // baja el centro de masa para no volcar
    [SerializeField] float downforce = 30f;           // presión aerodinámica (más velocidad = más pegado al suelo)

    // ---------------------------------------------------------------
    // Variables internas
    // ---------------------------------------------------------------
    // Lo activa CarEnterExit cuando el jugador sube al auto.
    // Si es false, el auto ignora el teclado y se queda frenado.
    public bool isDriving = false;

    float throttle;          // -1 (reversa/freno) a 1 (acelerar)
    float steerInput;        // -1 (izquierda) a 1 (derecha)
    bool handbrake;          // ¿freno de mano presionado?
    float currentSteer;      // ángulo actual de las ruedas delanteras
    float currentRearGrip;   // agarre trasero actual (cambia suavemente)

    // ---------------------------------------------------------------
    // 6. INICIO
    // ---------------------------------------------------------------
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        // Baja el centro de masa respecto al calculado automáticamente.
        rb.centerOfMass += centerOfMassOffset;

        currentRearGrip = rearGrip;
    }

    // ---------------------------------------------------------------
    // 7. ENTRADA (la llama InputHandler cada frame)
    // ---------------------------------------------------------------
    public void SetInput(Vector2 input, bool handbrakeInput)
    {
        // Nadie maneja: ignora el teclado
        if (!isDriving)
        {
            steerInput = 0f;
            throttle = 0f;
            handbrake = false;
            return;
        }

        steerInput = Mathf.Clamp(input.x, -1f, 1f);
        throttle = Mathf.Clamp(input.y, -1f, 1f);
        handbrake = handbrakeInput;
    }

    // ---------------------------------------------------------------
    // 8. FÍSICA (todo lo de fuerzas va en FixedUpdate)
    // ---------------------------------------------------------------
    void FixedUpdate()
    {
        float speedKmh = rb.velocity.magnitude * 3.6f;                 // velocidad total en km/h
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward); // + adelante, - reversa

        ApplyMotorAndBrakes(speedKmh, forwardSpeed);
        ApplySteering(speedKmh, forwardSpeed);
        ApplyGrip(speedKmh);
        ApplyDownforce();
    }

    // Motor, freno y reversa
    void ApplyMotorAndBrakes(float speedKmh, float forwardSpeed)
    {
        // Auto estacionado: sin motor y con las 4 ruedas frenadas
        if (!isDriving)
        {
            rearLeft.motorTorque = 0f;
            rearRight.motorTorque = 0f;
            frontLeft.brakeTorque = brakeTorque;
            frontRight.brakeTorque = brakeTorque;
            rearLeft.brakeTorque = brakeTorque;
            rearRight.brakeTorque = brakeTorque;
            return;
        }

        float torque = 0f;
        float brake = 0f;

        if (throttle > 0.01f)
        {
            // Si voy en reversa y presiono adelante: freno primero.
            if (forwardSpeed < -1f) brake = brakeTorque;
            else torque = throttle * motorTorque;
        }
        else if (throttle < -0.01f)
        {
            // Si voy hacia adelante y presiono atrás: freno. Si ya casi paré: reversa.
            if (forwardSpeed > 1f) brake = brakeTorque;
            else torque = throttle * motorTorque * 0.5f;
        }
        else
        {
            brake = 100f; // freno motor suave cuando no se toca nada
        }

        // Límites de velocidad
        if (forwardSpeed > 0f && speedKmh > maxSpeedKmh) torque = 0f;
        if (forwardSpeed < 0f && speedKmh > maxReverseSpeedKmh) torque = 0f;

        // Tracción trasera: el motor solo mueve las ruedas traseras
        rearLeft.motorTorque = torque;
        rearRight.motorTorque = torque;

        // Freno en las 4 ruedas
        frontLeft.brakeTorque = brake;
        frontRight.brakeTorque = brake;

        // Atrás: el freno de mano reemplaza al freno normal
        float rearBrake = handbrake ? handbrakeTorque : brake;
        rearLeft.brakeTorque = rearBrake;
        rearRight.brakeTorque = rearBrake;
    }

    // Dirección sensible a la velocidad + ayuda para contravolantear
    void ApplySteering(float speedKmh, float forwardSpeed)
    {
        // A más velocidad, menos ángulo (evita giros bruscos a 150 km/h)
        float t = Mathf.Clamp01(speedKmh / steerFullSpeedKmh);
        float maxAngle = Mathf.Lerp(maxSteerAngle, minSteerAngle, t);
        float target = steerInput * maxAngle;

        // Contravolante automático: si el auto va "de lado", las ruedas apuntan hacia donde viaja.
        if (speedKmh > 15f && forwardSpeed > 0f)
        {
            // Ángulo entre hacia dónde apunta el auto y hacia dónde se mueve realmente
            float slipAngle = Vector3.SignedAngle(transform.forward, rb.velocity, Vector3.up);
            if (Mathf.Abs(slipAngle) > 8f)
                target += Mathf.Clamp(slipAngle, -30f, 30f) * counterSteerAssist;
        }

        target = Mathf.Clamp(target, -maxSteerAngle, maxSteerAngle);

        // Suaviza el giro del volante
        currentSteer = Mathf.Lerp(currentSteer, target, steerSmoothing * Time.fixedDeltaTime);

        frontLeft.steerAngle = currentSteer;
        frontRight.steerAngle = currentSteer;
    }

    // Agarre lateral: es lo que permite o impide derrapar
    void ApplyGrip(float speedKmh)
    {
        float target = rearGrip;

        if (handbrake)
        {
            // Freno de mano: la parte trasera pierde agarre -> drift
            target = rearDriftGrip;
        }
        else if (speedKmh > 30f && throttle > 0.5f)
        {
            // Acelerar a fondo mientras giras: la cola se desliza un poco (power slide)
            target = rearGrip * (1f - Mathf.Abs(steerInput) * throttleSlide);
        }

        // Perder agarre es rápido, recuperarlo es lento (así el drift dura)
        float speed = target < currentRearGrip ? gripLossSpeed : gripRecoverSpeed;
        currentRearGrip = Mathf.MoveTowards(currentRearGrip, target, speed * Time.fixedDeltaTime);

        SetSidewaysStiffness(frontLeft, frontGrip * surfaceGrip);
        SetSidewaysStiffness(frontRight, frontGrip * surfaceGrip);
        SetSidewaysStiffness(rearLeft, currentRearGrip * surfaceGrip);
        SetSidewaysStiffness(rearRight, currentRearGrip * surfaceGrip);
    }

    // Cambia la rigidez de la fricción lateral de una rueda
    void SetSidewaysStiffness(WheelCollider wheel, float stiffness)
    {
        WheelFrictionCurve curve = wheel.sidewaysFriction; // las curvas son structs: se copian,
        curve.stiffness = stiffness;                        // se modifican
        wheel.sidewaysFriction = curve;                     // y se reasignan
    }

    // Empuja el auto hacia el suelo según la velocidad
    void ApplyDownforce()
    {
        rb.AddForce(-transform.up * downforce * rb.velocity.magnitude);
    }

    // ---------------------------------------------------------------
    // 9. VISUAL (en Update para que se vea fluido)
    // ---------------------------------------------------------------
    void Update()
    {
        UpdateWheelMesh(frontLeft, frontLeftMesh);
        UpdateWheelMesh(frontRight, frontRightMesh);
        UpdateWheelMesh(rearLeft, rearLeftMesh);
        UpdateWheelMesh(rearRight, rearRightMesh);
    }

    // Copia posición y rotación del WheelCollider a la malla de la rueda
    void UpdateWheelMesh(WheelCollider col, Transform mesh)
    {
        if (mesh == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.SetPositionAndRotation(pos, rot);
    }
}