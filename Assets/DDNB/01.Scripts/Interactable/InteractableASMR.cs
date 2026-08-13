using UnityEngine;

public class InteractableASMR : InteractableObject
{
    [Header("ASMR")]
    public bool isASMRCompleted = false;
    public float currentASMRProgress = 0f;

    [Header("ASMR SFX")]
    [Tooltip("Resources/Sounds/ASMRSFX/{카테고리}. 비우면 Data.interactionSoundName 또는 이름 추정.")]
    [SerializeField] private string asmrSfxCategory;

    /// <summary>ASMRSFX 하위 폴더명 (keyboard, box, book 등)</summary>
    public string AsmrSfxCategory => asmrSfxCategory;

    private void Reset()
    {
        inputMode = InteractInputMode.Tap;
    }

    public override void Interact()
    {
        if (GameManager.Instance.Player.CurrentState == PlayerState.ASMR)
        {
            ASMRManager.Instance.EndASMR();
            return;
        }

        ASMRManager.Instance.StartASMR(this);
    }
}
