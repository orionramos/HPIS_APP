using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject connectionPanel; // Panel de conexi�n
    public GameObject infoPanel; // Panel de informaci�n

    private void Start()
    {
        ShowConnectionPanel(); // Asegura que inicie en la pantalla de conexi�n
    }

    public void ShowConnectionPanel()
    {
        connectionPanel.SetActive(true);
        infoPanel.SetActive(false);
    }

    public void ShowInfoPanel()
    {
        connectionPanel.SetActive(false);
        infoPanel.SetActive(true);
    }
}
