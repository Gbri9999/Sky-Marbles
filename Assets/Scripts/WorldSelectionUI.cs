using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldSelectionUI : MonoBehaviour
{
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text worldName;
    [SerializeField] private TMP_Text worldDescription;
    [SerializeField] private TMP_Text worldPrice;
    [SerializeField] private Button enterButton;
    [SerializeField] private GameObject worldDataPanel;

    [SerializeField] private CanvasGroup levelsPanel;
    [SerializeField] private TMP_Text levelsTitle;
    [SerializeField] private PlayMenu playMenu;
    private WorldData selectedWorld;
    
    public void SelectedWorld(WorldData world)
    {
        selectedWorld = world;
        previewImage.sprite = world.previewImage;
        worldName.text = world.worldName;
        worldDescription.text = world.description;
        worldPrice.text = world.Price == 0 ? "FREE": world.Price.ToString() + "COINS";
        enterButton.interactable = true;
        worldDataPanel.SetActive(true);

        CanvasGroup panelGroup = worldDataPanel.GetComponent<CanvasGroup>();

        panelGroup.alpha = 1f;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;

        worldDataPanel.transform.SetAsLastSibling();
    }
    public void EnterWorld()
    {
        levelsTitle.text = selectedWorld.worldName;
        worldDataPanel.SetActive(false);
        playMenu.OpenPanel(levelsPanel);
    }
}
