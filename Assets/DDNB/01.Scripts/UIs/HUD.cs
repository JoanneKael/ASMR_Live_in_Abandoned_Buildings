using UnityEngine;

public class HUD : MonoBehaviour
{
    [SerializeField] private GameObject[] uis;

    public void Init(bool lobby)
    {
        if (lobby)
        {
            foreach (var go in uis)
            {
                go.SetActive(false);
            }
            uis[4].SetActive(true);
        }
        else
        {
            foreach (var go in uis)
            {
                go.SetActive(true);
            }
            uis[2].SetActive(false);
        }
    }
}