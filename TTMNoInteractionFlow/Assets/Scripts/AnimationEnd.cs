using UnityEngine;

public class AnimationEnd : MonoBehaviour
{
    public string nextScene;

    public void EndAnimation()
    {
        SceneFlowManager.Instance.LoadScene(nextScene);
    }
}