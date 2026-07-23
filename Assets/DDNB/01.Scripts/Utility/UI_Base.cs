using UnityEngine;

public class UI_Base : MonoBehaviour
{
    public virtual void CloseUI()
    {
        gameObject.SetActive(false);
    }
}
