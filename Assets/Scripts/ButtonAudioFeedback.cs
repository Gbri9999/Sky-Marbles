using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler
{
    private Button button;
    private SceneAudioManager audioManager;
    private AudioClip hoverSound;

    public void Initialize(SceneAudioManager manager, Button target, AudioClip clickSound, AudioClip hoverSound)
    {
        audioManager = manager;
        button = target;
        this.hoverSound = hoverSound;
        button.onClick.AddListener(() => audioManager.PlayButtonClick(clickSound));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.IsInteractable() && audioManager != null)
            audioManager.PlayButtonHover(hoverSound);
    }
}
