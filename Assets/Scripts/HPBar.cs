using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    private Slider slider;

    private void Start()
    {
       slider = GetComponent<Slider>(); 
    }

    public void CambiarVidaMax(float vidaMaxima)
    {
        slider.maxValue = vidaMaxima;
    }

    public void CambiarVidaActual(float cantidadVida)
    {
        slider.value = cantidadVida;
    }

    public void InicializarBarra(float cantidadVida)
    {
        CambiarVidaMax(cantidadVida);
        CambiarVidaActual(cantidadVida);
    }
}

