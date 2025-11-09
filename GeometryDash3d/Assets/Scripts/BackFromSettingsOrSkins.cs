using UnityEngine;

public class BackFromSettingsOrSkins : MonoBehaviour
{
    // Appelé par le bouton Back des panels Settings/Skins
    public void OnBack()
    {
        // 1) Si on est en PAUSE, on revient au PausePanel
        var pause = FindObjectOfType<PauseController>();
        if (pause != null && pause.IsPaused)
        {
            pause.Btn_BackFromSubpanel();
            return;
        }

        // 2) Sinon, on est dans le MENU : on revient au MainPanel
        var menu = FindObjectOfType<SimpleMenuController>();
        if (menu != null)
        {
            menu.Back(); // ShowMain()
            return;
        }

        // 3) Fallback : si aucun des deux (rare)
        Debug.LogWarning("[BackFromSettingsOrSkins] Aucun contrôleur trouvé (Pause ou Menu).");
    }
}
