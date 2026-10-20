using UnityEngine;
using UnityEngine.SceneManagement;

public class InputHandler : MonoBehaviour
{
    // Arrastra aquí el auto (el objeto que tiene CarHandler)
    [SerializeField]
    CarHandler carHandler;

    void Update()
    {
        Vector2 input = Vector2.zero;

        input.x = Input.GetAxis("Horizontal"); // A/D o flechas: dirección
        input.y = Input.GetAxis("Vertical");   // W/S o flechas: acelerar/frenar

        // Espacio = freno de mano (para derrapar)
        bool handbrake = Input.GetKey(KeyCode.Space);

        carHandler.SetInput(input, handbrake);

        // R = reiniciar escena
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
