using System.Collections;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] private float currentStamina;
    [SerializeField] private bool canRun = true;

    public bool IsExhauseted => !canRun;

    [Header("Timer Settings")]
    [SerializeField] private float exhaustionTime = 3f;

    [Header("Recovery Settings")]
    [SerializeField] private float staminaRecovery = 5f;
    [SerializeField] private float staminaRecoveryTime = 3f;
    [SerializeField] private float staminaReduction = 20f;

    private float maxStamina = 100;
    private float lastRunTime = -10f;

    private UI_Status ui_status;

    private void Start()
    {
        currentStamina = maxStamina;
        ui_status = UIManager.Instance.ShowUI<UI_Status>();
        if (ui_status != null)
            ui_status.RefreshStaminaUI(currentStamina, maxStamina);
    }

    private void Update()
    {
        HandleStamina();
    }

    private void HandleStamina()
    {
        Player player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        bool blockRunInput = player != null
            && (player.CurrentState == PlayerState.Hidden
                || player.CurrentState == PlayerState.Stunned
                || player.IsHideTransitioning);

        if (!blockRunInput && InputManager.Instance.IsRunning && canRun)
        {
            currentStamina -= Time.deltaTime * staminaReduction;
            lastRunTime = Time.time;

            if (currentStamina <= 0)
            {
                currentStamina = 0f;
                StartCoroutine(IE_Exhaustion(exhaustionTime));
            }

            ui_status.RefreshStaminaUI(currentStamina, maxStamina);
        }
        else if (currentStamina < maxStamina)
        {
            if (Time.time - lastRunTime >= staminaRecoveryTime)
            {
                currentStamina += Time.deltaTime * staminaRecovery;
                ui_status.RefreshStaminaUI(currentStamina, maxStamina);
            }
        }
    }

    private IEnumerator IE_Exhaustion(float delay)
    {
        canRun = false;
        yield return new WaitForSeconds(delay);
        canRun = true;
    }

    /// <summary>새 하루 시작 시 스태미나 회복</summary>
    public void ResetStatus()
    {
        StopAllCoroutines();
        canRun = true;
        currentStamina = maxStamina;
        lastRunTime = -10f;

        if (ui_status == null && UIManager.Instance != null)
            ui_status = UIManager.Instance.ShowUI<UI_Status>();

        if (ui_status != null)
            ui_status.RefreshStaminaUI(currentStamina, maxStamina);
    }
}
