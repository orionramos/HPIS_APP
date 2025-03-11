using UnityEngine;                    // Importa la librer�a de Unity para funcionalidades b�sicas.
using UnityEngine.UI;                 // Importa la librer�a para trabajar con elementos de la interfaz de usuario (UI).

// Define la clase GripStatus que hereda de MonoBehaviour para poder asignarla como componente en GameObjects.
public class GripStatus : MonoBehaviour
{
    [Header("Iconos de Estado")]     // Etiqueta para organizar en el Inspector las variables relacionadas con el estado de la mano.
    public Image handIcon;              // Componente Image que se usar� para mostrar el icono de la mano.
    public Sprite openHandSprite;       // Sprite que representa la mano abierta.
    public Sprite closedHandSprite;     // Sprite que representa la mano cerrada.

    [Header("Iconos de GT")]           // Etiqueta para agrupar las variables relacionadas con los iconos de GT.
    public Image GTIcon;                // Componente Image que se usar� para mostrar el icono correspondiente a GT.
    public Sprite GT1Sprite;            // Sprite para cuando GT tiene el valor 1.
    public Sprite GT2Sprite;            // Sprite para cuando GT tiene el valor 2.
    public Sprite GT3Sprite;            // Sprite para cuando GT tiene el valor 3.
    public Sprite GT4Sprite;            // Sprite para cuando GT tiene el valor 4.

    [Header("Umbrales de Activaci�n")] // Etiqueta para agrupar la variable de umbral.
    public int activationThreshold = 1; // Umbral que determina cu�nto debe cambiar un contador EMG para considerar que hubo activaci�n.

    [Header("Velocidad de cambio")]    // Etiqueta para agrupar la variable de velocidad de cambio.
    public float changeSpeed = 0.1f;      // Tiempo m�nimo (en segundos) entre cambios de estado para evitar actualizaciones muy frecuentes.

    private bool isHolding = false;     // Variable interna que indica si la mano est� cerrada (true) o abierta (false).
    private float lastChangeTime = 0f;    // Almacena el tiempo en que se realiz� el �ltimo cambio de estado.

    private int lastEMGA = 0;           // Guarda el �ltimo valor recibido del contador EMGA.
    private int lastEMGB = 0;           // Guarda el �ltimo valor recibido del contador EMGB.

    // M�todo p�blico que actualiza el estado de la mano y el icono de GT basado en los contadores EMG y el valor GT.
    public void UpdateGripStatus(int EMGA_counter, int EMGB_counter, int GT)
    {
        // Comprueba si ha pasado suficiente tiempo desde el �ltimo cambio para evitar actualizaciones muy r�pidas.
        if (Time.time - lastChangeTime < changeSpeed)
        {
            return;  // Si no se cumple, termina el m�todo sin realizar cambios.
        }

        // Comprueba si hay una diferencia significativa en los contadores EMG respecto a sus �ltimos valores.
        if (Mathf.Abs(EMGA_counter - lastEMGA) >= activationThreshold || Mathf.Abs(EMGB_counter - lastEMGB) >= activationThreshold)
        {
            // Si EMGA_counter ha aumentado respecto a su �ltimo valor, se considera que la mano debe estar abierta.
            if (EMGA_counter > lastEMGA)
            {
                isHolding = false;   // Establece el estado de la mano a abierta.
                SetHandIcon();       // Actualiza el icono de la mano con el sprite correspondiente.
            }
            // Si EMGB_counter ha aumentado respecto a su �ltimo valor, se considera que la mano debe estar cerrada.
            else if (EMGB_counter > lastEMGB)
            {
                isHolding = true;    // Establece el estado de la mano a cerrada.
                SetHandIcon();       // Actualiza el icono de la mano con el sprite correspondiente.
            }
        }
        // Actualiza los �ltimos valores conocidos de los contadores EMG para futuras comparaciones.
        lastEMGA = EMGA_counter;
        lastEMGB = EMGB_counter;

        // Actualiza el icono de GT seg�n el valor recibido (de 1 a 4).
        SetGTIcon(GT);

        // Registra el tiempo en el que se realiz� este cambio.
        lastChangeTime = Time.time;
    }

    // M�todo privado que asigna el sprite correcto al componente handIcon seg�n el estado actual de la mano.
    private void SetHandIcon()
    {
        // Verifica que se haya asignado el componente handIcon; si no, muestra una advertencia.
        if (handIcon == null)
        {
            Debug.LogWarning("handIcon no asignado.");
            return;
        }
        // Asigna el sprite correspondiente:
        // Si isHolding es true (mano cerrada), se asigna closedHandSprite; de lo contrario, openHandSprite.
        handIcon.sprite = isHolding ? closedHandSprite : openHandSprite;
        // Muestra en la consola el estado actual para ayudar en la depuraci�n.
        Debug.Log(isHolding ? "Mano Cerrada (B)" : "Mano Abierta (A)");
    }

    // M�todo privado que actualiza el icono de GT asignando el sprite adecuado seg�n el valor GT.
    private void SetGTIcon(int GT)
    {
        // Verifica que se haya asignado el componente GTIcon; si no, muestra una advertencia.
        if (GTIcon == null)
        {
            Debug.LogWarning("GTIcon no asignado.");
            return;
        }
        // Utiliza una estructura switch para seleccionar el sprite seg�n el valor de GT.
        switch (GT)
        {
            case 1:
                GTIcon.sprite = GT1Sprite;  // Asigna el sprite para GT = 1.
                break;
            case 2:
                GTIcon.sprite = GT2Sprite;  // Asigna el sprite para GT = 2.
                break;
            case 3:
                GTIcon.sprite = GT3Sprite;  // Asigna el sprite para GT = 3.
                break;
            case 4:
                GTIcon.sprite = GT4Sprite;  // Asigna el sprite para GT = 4.
                break;
            default:
                // Si el valor de GT no es reconocido (no es 1, 2, 3 o 4), 
                Debug.LogWarning("Valor de GT no reconocido: " + GT);
                break;
        }
    }

    // M�todo de inicializaci�n que se ejecuta cuando el script se inicia.
    private void Start()
    {
        isHolding = false;   // Inicializa el estado de la mano como abierta.
        SetHandIcon();       // Actualiza el icono de la mano para reflejar el estado inicial.
    }
}
