// Updated UIManager.cs – always show the main info panel
// Refactored by Gemini to use UIReferenceHolder
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public UIReferenceHolder uiHolder; // Centralized UI references

    [Header("Managers")]
    public DataManager dataManager;

    private void Start()
    {
        // Arranca directamente en la interfaz principal
        ShowInfoPanel();
    }

    /// <summary>
    /// Muestra solo el panel de información
    /// </summary>
    public void ShowInfoPanel()
    {
        if (uiHolder != null && uiHolder.infoPanel != null)
            uiHolder.infoPanel.SetActive(true);
        else
            Debug.LogWarning("UIHolder or its infoPanel is not assigned in UIManager.");
    }

    /// <summary>
    /// Limpia la UI y mantiene visible el panel principal
    /// </summary>
    public void ResetUIAndShowInfo()
    {
        if (dataManager != null)
            dataManager.ResetUI();
        ShowInfoPanel();
    }
}
