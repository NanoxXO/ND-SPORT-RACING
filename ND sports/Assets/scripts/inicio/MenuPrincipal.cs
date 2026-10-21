using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

// Menú principal de ND-Sport Racing.
// INICIAR carga el juego, SALIR lo cierra, GARAGE y CONFIGURACIÓN muestran "Próximamente".
public class MenuPrincipal : MonoBehaviour
{
    [Header("Escena del juego (debe estar en Build Settings)")]
    public string escenaJuego = "SampleScene";

    [Header("Cartel de Próximamente")]
    public GameObject cartelProximamente;
    public TextMeshProUGUI textoCartel;
    public float segundosVisible = 2f;

    Coroutine rutinaCartel;

    void Start()
    {
        // Deja el menú usable al volver desde el juego o la pausa
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cartelProximamente != null)
            cartelProximamente.SetActive(false);
    }

    public void Iniciar()
    {
        SceneManager.LoadScene(escenaJuego);
    }

    public void AbrirGarage()
    {
        MostrarCartel("GARAGE - PRÓXIMAMENTE");
    }

    public void AbrirConfiguracion()
    {
        MostrarCartel("CONFIGURACIÓN - PRÓXIMAMENTE");
    }

    public void Salir()
    {
        // Application.Quit no funciona en el editor, por eso se detiene el Play
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void MostrarCartel(string mensaje)
    {
        if (cartelProximamente == null) return;

        if (textoCartel != null)
            textoCartel.text = mensaje;

        // Reinicia el contador si ya había un cartel visible
        if (rutinaCartel != null)
            StopCoroutine(rutinaCartel);

        rutinaCartel = StartCoroutine(OcultarCartel());
    }

    IEnumerator OcultarCartel()
    {
        cartelProximamente.SetActive(true);
        yield return new WaitForSeconds(segundosVisible);
        cartelProximamente.SetActive(false);
    }
}