using UnityEngine;
using TMPro;

// Muestra la velocidad del auto en km/h como número
public class Velocimetro : MonoBehaviour
{
    public Rigidbody rb;                  // si queda vacío, usa el del mismo objeto
    public TextMeshProUGUI textoKmh;      // número en pantalla

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (rb == null || textoKmh == null) return;

        // velocity está en m/s, x3.6 lo pasa a km/h
        float kmh = rb.velocity.magnitude * 3.6f;
        textoKmh.text = Mathf.RoundToInt(kmh).ToString();
    }
}