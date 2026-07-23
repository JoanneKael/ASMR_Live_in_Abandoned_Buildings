using UnityEngine;

public class MANAGER : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
}
