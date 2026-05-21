using UnityEngine;
using System.Collections.Generic;

public class PhotoStorage : MonoBehaviour
{
    public static PhotoStorage Instance;

    public List<Texture2D> photos = new List<Texture2D>();

    public int maxPhotos = 4;

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

    public int GetNextIndex()
    {
        for (int i = 0; i < photos.Count; i++)
        {
            if (photos[i] == null) return i;
        }

        return photos.Count;
    }

    public void SetPhoto(int index, Texture2D photo)
    {
        if (index < 0) return;

        while (photos.Count <= index)
        {
            photos.Add(null);
        }

        if (photos[index] != null && photos[index] != photo)
        {
            Destroy(photos[index]);
        }

        photos[index] = photo;
    }
}