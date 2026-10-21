using UnityEngine;
using TMPro;

// Velocímetro: gira la aguja según la velocidad y muestra la marcha activa.
// Va en el Canvas (por ejemplo en el objeto del velocímetro).
public class SpeedometerUI : MonoBehaviour
{
    // ---------------------------------------------------------------
    // 1. REFERENCIAS
    // ---------------------------------------------------------------
    [Header("Auto")]
    public Rigidbody carRigidbody;     // Rigidbody del auto (de ahí se lee la velocidad)
    public ManualGearbox gearbox;      // Caja manual (de ahí se lee la marcha)

    [Header("UI")]
    public RectTransform needle;       // Imagen de la aguja
    public TMP_Text gearText;          // Texto dentro del recuadro de la marcha
    public TMP_Text speedText;         // Opcional: velocidad en número

    // ---------------------------------------------------------------
    // 2. CALIBRACIÓN (ajústalo hasta que la aguja coincida con tu imagen)
    // ---------------------------------------------------------------
    [Header("Calibración")]
    public float maxSpeedKmh = 240f;   // el último número de tu velocímetro
    public float zeroAngle = 135f;     // ángulo de la aguja en 0 km/h
    public float maxAngle = -135f;     // ángulo de la aguja en la velocidad máxima
    public float needleSmoothTime = 0.1f; // más alto = aguja más lenta y suave

    [Header("Colores de la marcha")]
    public Color gearColor = Color.white;
    public Color clutchColor = Color.yellow; // color cuando el embrague está apretado

    // ---------------------------------------------------------------
    // Variables internas
    // ---------------------------------------------------------------
    float shownSpeed;      // velocidad suavizada que muestra la aguja
    float speedVelocity;   // variable interna de SmoothDamp

    void Start()
    {
        // Si no asignaste el Rigidbody, lo toma de la caja
        if (carRigidbody == null && gearbox != null)
            carRigidbody = gearbox.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (carRigidbody == null) return;

        // Velocidad real en km/h
        float speed = carRigidbody.velocity.magnitude * 3.6f;

        // Suaviza el movimiento de la aguja (como una aguja real)
        shownSpeed = Mathf.SmoothDamp(shownSpeed, speed, ref speedVelocity, needleSmoothTime);

        // Convierte la velocidad en un ángulo entre zeroAngle y maxAngle
        float t = Mathf.Clamp01(shownSpeed / maxSpeedKmh);
        float angle = Mathf.Lerp(zeroAngle, maxAngle, t);
        if (needle != null)
            needle.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (speedText != null)
            speedText.text = Mathf.RoundToInt(shownSpeed).ToString();

        // Marcha activa
        if (gearbox != null && gearText != null)
        {
            int g = gearbox.Gear;
            gearText.text = g == 0 ? "N" : (g == -1 ? "R" : g.ToString());
            gearText.color = gearbox.ClutchPressed ? clutchColor : gearColor;
        }
    }
}
