using UnityEngine;

[CreateAssetMenu(fileName = "New World", menuName = "SkyMarble/World Data")]
public class WorldData : ScriptableObject
{
    public int worldNumber;
    [TextArea(2, 4)]public string worldName;
    [TextArea(3, 6)] public string description;
    public Sprite previewImage;
    public int Price
    {
        get
        {
            if(worldNumber <= 1) return 0;

            return 10*(1<<worldNumber - 1);
        }
    }
}