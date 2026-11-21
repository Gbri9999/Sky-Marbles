using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAppearance : MonoBehaviour
{
    [SerializeField] private RuntimeAnimatorController bluey;
    [SerializeField] private RuntimeAnimatorController breeze;
    [SerializeField] private RuntimeAnimatorController bouncy;
    [SerializeField] private RuntimeAnimatorController bravy;
    [SerializeField] private RuntimeAnimatorController volty;
    [SerializeField] private RuntimeAnimatorController marinee;

    [SerializeField] private RuntimeAnimatorController loki;

    private void Awake()
    {
        string selected = PlayerPrefs.GetString(StoreProgress.SelectedCharacter, StoreProgress.Bluey);
        RuntimeAnimatorController chosen = bluey;

        if (selected == StoreProgress.Breeze && PlayerPrefs.GetInt(StoreProgress.Breeze, 0) == 1)
            chosen = breeze;
        else if (selected == StoreProgress.Bouncy && PlayerPrefs.GetInt(StoreProgress.Bouncy, 0) == 1)
            chosen = bouncy;
        else if (selected == StoreProgress.Bravy && StoreProgress.IsOwned(StoreProgress.Bravy))
            chosen = bravy;
        else if (selected == StoreProgress.Volty && StoreProgress.IsOwned(StoreProgress.Volty))
            chosen = volty;

        else if (selected == StoreProgress.Marinee && StoreProgress.IsOwned(StoreProgress.Marinee))
            chosen = marinee;

        if (selected == StoreProgress.Loki && StoreProgress.IsOwned(StoreProgress.Loki))
            chosen = loki;

        GetComponent<Animator>().runtimeAnimatorController = chosen;
    }
}
