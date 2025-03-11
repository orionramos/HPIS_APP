using UnityEngine;
using UnityEngine.UI;

public class GripStatus : MonoBehaviour
{
    [Header("Iconos de Estado")]
    public Image openHandIcon;     // �cono de mano abierta
    public Image closedHandIcon;   // �cono de mano cerrada

    [Header("Umbrales de Activaci�n")]
    public int activationThreshold = 1; // Umbral para considerar que la mano se est� activando

    [Header("Velocidad de cambio")]
    public float changeSpeed = 0.1f; // Velocidad de cambio de estado.

    private bool isHolding = false; // Estado actual de la mano (cerrada/abierta)
    private float lastChangeTime = 0f; // Tiempo del ultimo cambio.

    private int lastEMGA = 0; //ultimo valor de EMGA_counter
    private int lastEMGB = 0;//ultimo valor de EMGB_counter

    // M�todo p�blico para actualizar el estado de la mano seg�n el EMGA_counter y EMGB_counter
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

    // M�todo privado para actualizar los �conos
    private void UpdateIcons()
    {
        openHandIcon.gameObject.SetActive(!isHolding);
        closedHandIcon.gameObject.SetActive(isHolding);
    }
}
