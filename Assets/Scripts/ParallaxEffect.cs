using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Configuración")]
    public Transform camara;      // Arrastra la Cámara Principal aquí
    [Range(-1f, 1f)]
    public float parallaxFactor; // 0 = se mueve con la cámara, 1 = se queda quieto

    private Vector3 ultimaPosicionCamara;

    void Start()
    {
        if (camara == null) camara = Camera.main.transform;
        ultimaPosicionCamara = camara.position;
    }

    // Usamos LateUpdate para que la cámara ya se haya movido antes
    void LateUpdate()
    {
        Vector3 movimientoCamara = camara.position - ultimaPosicionCamara;
        
        // Multiplicamos el movimiento de la cámara por nuestro factor
        // Solo afectamos al eje X para un plataformas lateral
        transform.position += new Vector3(movimientoCamara.x * parallaxFactor, 0, 0);

        ultimaPosicionCamara = camara.position;
    }
}
