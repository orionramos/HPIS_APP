using UnityEngine;
using UnityEngine.UI;

public class CameraDisplay : MonoBehaviour
{
#if ENABLE_TABLET_CAMERA
    public RawImage rawImage;
    private WebCamTexture webCamTexture;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            webCamTexture = new WebCamTexture(devices[0].name);
            rawImage.texture = webCamTexture;
            rawImage.material.mainTexture = webCamTexture;
            webCamTexture.Play();
        }
        else
        {
            Debug.LogError("No se encontró ninguna cámara.");
        }
    }

    void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }
#else
    void Start()
    {
        GameObject pcCameraObject = GameObject.Find("PCCamera");
        if (pcCameraObject != null)
        {
            pcCameraObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PCCamera no encontrado en la escena.");
        }
    }
#endif
}
