using UnityEngine;
using UnityEngine.UI;

public class WorldButton : MonoBehaviour
{
    [SerializeField] private WorldSelectionUI selectionUI;
    [SerializeField] private WorldData worldData;
    [SerializeField] private Button button;

    private void Awake()
    {
        button.onClick.AddListener(() => selectionUI.SelectedWorld(worldData));
    }
}
