using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAppearance : MonoBehaviour
{
    [SerializeField] private RuntimeAnimatorController bluey;
    [SerializeField] private RuntimeAnimatorController breeze;
    [SerializeField] private RuntimeAnimatorController bouncy;

    private void Awake()
    {
        string selected = PlayerPrefs.GetString(StoreProgress.SelectedCharacter, StoreProgress.Bluey);
        RuntimeAnimatorController chosen = bluey;

        if (selected == StoreProgress.Breeze && PlayerPrefs.GetInt(StoreProgress.Breeze, 0) == 1)
            chosen = breeze;
        else if (selected == StoreProgress.Bouncy && PlayerPrefs.GetInt(StoreProgress.Bouncy, 0) == 1)
            chosen = bouncy;

        GetComponent<Animator>().runtimeAnimatorController = chosen;
    }
}
