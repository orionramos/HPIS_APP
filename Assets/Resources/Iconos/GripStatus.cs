using UnityEngine;
using UnityEngine.UI;

public class GripStatus : MonoBehaviour
{
    [Header("Iconos de Estado")]
    public Image openHandIcon;     // Ícono de mano abierta
    public Image closedHandIcon;   // Ícono de mano cerrada

    [Header("Umbrales de Activación")]
    public int activationThreshold = 1; // Umbral para considerar que la mano se está activando

    [Header("Velocidad de cambio")]
    public float changeSpeed = 0.1f; // Velocidad de cambio de estado.

    private bool isHolding = false; // Estado actual de la mano (cerrada/abierta)
    private float lastChangeTime = 0f; // Tiempo del ultimo cambio.

    private int lastEMGA = 0; //ultimo valor de EMGA_counter
    private int lastEMGB = 0;//ultimo valor de EMGB_counter

    // Método público para actualizar el estado de la mano según el EMGA_counter y EMGB_counter
    public void UpdateGripStatus(int EMGA_counter, int EMGB_counter)
    {
        // Check if the last change was recent enough
        if (Time.time - lastChangeTime < changeSpeed)
        {
            return;
        }

        // Check if EMGA_counter or EMGB_counter have been triggered
        if (Mathf.Abs(EMGA_counter - lastEMGA) >= activationThreshold || Mathf.Abs(EMGB_counter - lastEMGB) >= activationThreshold)
        {

            if (EMGA_counter > lastEMGA) // se presiono el boton A
            {
                isHolding = false;
                UpdateIcons();
                lastChangeTime = Time.time;
                Debug.Log("Mano Abierta (A)");
            }
            else if (EMGB_counter > lastEMGB) // se presiono el boton B
            {
                isHolding = true;
                UpdateIcons();
                lastChangeTime = Time.time;
                Debug.Log("Mano Cerrada (B)");
            }


        }
        lastEMGA = EMGA_counter; //update last value
        lastEMGB = EMGB_counter; //update last value
    }

    // Método privado para actualizar los íconos
    private void UpdateIcons()
    {
        openHandIcon.gameObject.SetActive(!isHolding);
        closedHandIcon.gameObject.SetActive(isHolding);
    }
}
