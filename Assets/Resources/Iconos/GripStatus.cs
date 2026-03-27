using UnityEngine;
using UnityEngine.UI;

public class GripStatus : MonoBehaviour
{
    [Header("Iconos de Estado")]
    public Image handIcon;
    public Sprite openHandSprite;
    public Sprite closedHandSprite;

    [Header("Iconos de GT")]
    public Image GTIcon;
    public Sprite GT1Sprite;
    public Sprite GT2Sprite;
    public Sprite GT3Sprite;
    public Sprite GT4Sprite;

    [Header("Iconos de Palma")]
    public Image palmIcon;
    public Sprite Palm1Sprite;
    public Sprite Palm2Sprite;
    public Sprite Palm3Sprite;

    [Header("Umbrales de Activación")]
    public int activationThreshold = 1;

    [Header("Velocidad de cambio")]
    public float changeSpeed = 0.1f;

    private bool isHolding = false;
    private float lastChangeTime = 0f;

    private int lastEMGA = 0;
    private int lastEMGB = 0;
    private int lastGM = 0;

    public void UpdateGripStatus(int EMGA_counter, int EMGB_counter, int GT, int GM)
    {
        if (Time.time - lastChangeTime < changeSpeed) return;

        if (Mathf.Abs(EMGA_counter - lastEMGA) >= activationThreshold || Mathf.Abs(EMGB_counter - lastEMGB) >= activationThreshold)
        {
            if (EMGA_counter > lastEMGA)
            {
                isHolding = false;
                SetHandIcon();
            }
            else if (EMGB_counter > lastEMGB)
            {
                isHolding = true;
                SetHandIcon();
            }
        }

        lastEMGA = EMGA_counter;
        lastEMGB = EMGB_counter;

        SetGTIcon(GT);
        UpdateGripMode(GM); // Asegura que se actualice la palma con el nuevo valor

        lastChangeTime = Time.time;
    }

    public void UpdateGripMode(int GM)
    {
        if (GM == lastGM) return;
        lastGM = GM;

        if (palmIcon == null)
        {
            Debug.LogWarning("palmIcon no asignado.");
            return;
        }

        switch (GM)
        {
            case 1:
                palmIcon.sprite = Palm1Sprite;
                break;
            case 2:
                palmIcon.sprite = Palm2Sprite;
                break;
            case 3:
                palmIcon.sprite = Palm3Sprite;
                break;
            default:
                Debug.LogWarning("Valor de GM no reconocido: " + GM);
                break;
        }
    }

    private void SetHandIcon()
    {
        if (handIcon == null)
        {
            Debug.LogWarning("handIcon no asignado.");
            return;
        }
        handIcon.sprite = isHolding ? closedHandSprite : openHandSprite;
        Debug.Log(isHolding ? "Mano Cerrada (B)" : "Mano Abierta (A)");
    }

    private void SetGTIcon(int GT)
    {
        if (GTIcon == null)
        {
            Debug.LogWarning("GTIcon no asignado.");
            return;
        }
        switch (GT)
        {
            case 1:
                GTIcon.sprite = GT1Sprite;
                break;
            case 2:
                GTIcon.sprite = GT2Sprite;
                break;
            case 3:
                GTIcon.sprite = GT3Sprite;
                break;
            case 4:
                GTIcon.sprite = GT4Sprite;
                break;
            default:
                Debug.LogWarning("Valor de GT no reconocido: " + GT);
                break;
        }
    }

    private void Start()
    {
        isHolding = false;
        SetHandIcon();
        UpdateGripMode(1); // Valor inicial por defecto
    }
}
