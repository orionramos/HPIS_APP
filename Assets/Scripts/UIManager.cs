using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject connectionPanel; // Panel de conexión
    public GameObject infoPanel; // Panel de información

    private void Start()
    {
        ShowConnectionPanel(); // Asegura que inicie en la pantalla de conexión
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
