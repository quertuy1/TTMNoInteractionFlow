using UnityEngine;

public class AccessorySpriteLibrary : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite[] sprites;

    public Sprite[] GetSprites()
    {
        return sprites;
    }
}
