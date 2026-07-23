using UnityEngine;

public class UIRoot : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

}