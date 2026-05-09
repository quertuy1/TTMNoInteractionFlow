using UnityEngine;
using System.Collections.Generic;

public class PhotoStorage : MonoBehaviour
{
    public static PhotoStorage Instance;

    public List<Texture2D> photos = new List<Texture2D>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddPhoto(Texture2D photo)
    {
        photos.Add(photo);
    }
}