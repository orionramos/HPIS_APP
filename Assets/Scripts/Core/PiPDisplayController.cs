using UnityEngine;

namespace HPIS.Core.Visuals
{
    public class PiPDisplayController : MonoBehaviour
    {
        [Header("PiP Components")]
        [SerializeField]
        [Tooltip("El Canvas flotante que contiene la RawImage")]
        private GameObject pipCanvas;

        [SerializeField]
        [Tooltip("Arrastra aquí todas las cámaras espía del Prefab")]
        private Camera[] availableCameras;

        public void ActivatePiP(string cameraName)
        {
            bool cameraFound = false;

            // Si el JSON manda un string vacío o "none", apagamos todo
            if (string.IsNullOrEmpty(cameraName) || cameraName.ToLower() == "none")
            {
                TurnOffAll();
                return;
            }

            // Buscamos la cámara solicitada y apagamos las demás para ahorrar recursos
            foreach (Camera cam in availableCameras)
            {
                if (cam != null)
                {
                    if (cam.gameObject.name == cameraName)
                    {
                        cam.gameObject.SetActive(true);
                        cameraFound = true;
                    }
                    else
                    {
                        cam.gameObject.SetActive(false);
                    }
                }
            }

            // Encendemos la pantalla holográfica solo si encontramos la cámara
            if (pipCanvas != null)
            {
                pipCanvas.SetActive(cameraFound);
            }

            if (!cameraFound)
            {
                Debug.LogWarning($"[PiPController] No se encontró la cámara '{cameraName}' en el Prefab.");
            }
        }

        private void TurnOffAll()
        {
            if (pipCanvas != null)
            {
                pipCanvas.SetActive(false);
            }

            foreach (Camera cam in availableCameras)
            {
                if (cam != null)
                {
                    cam.gameObject.SetActive(false);
                }
            }
        }
    }
}