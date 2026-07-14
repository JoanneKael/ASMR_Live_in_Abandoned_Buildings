using System.Collections;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentStamina;
    [SerializeField] private bool isInvincible = false;
    [SerializeField] private bool canRun = true;

    public bool IsExhauseted => !canRun;

    [Header("Timer Settings")]
    [SerializeField] private float invincibleTime = 3f;
    [SerializeField] private float exhaustionTime = 3f;

    [Header("Recovery Settings")]
    [SerializeField] private float healthRecovery = 5f;
    [SerializeField] private float healthRecoveryTime = 6f;
    [SerializeField] private float staminaRecovery = 5f;
    [SerializeField] private float staminaRecoveryTime = 3f;

    [SerializeField] private float staminaReduction = 20f;

    private float maxHealth = 100;
    private float maxStamina = 100;

    private float lastHitTime = -10f;
    private float lastRunTime = -10f;

    private void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    private void Update()
    {
        HandleHealth();
        HandleStamina();
    }

    private void HandleHealth()
    {
        if (Time.time - lastHitTime > healthRecoveryTime && currentHealth < maxHealth)
        {
            currentHealth += Time.deltaTime * healthRecovery;
            UIManager.Instance.UI_Status.RefreshHealthUI(currentHealth, maxHealth);
        }
    }

    private void HandleStamina()
    {
        if (InputManager.Instance.IsRunning && canRun)
        {
            currentStamina -= Time.deltaTime * staminaReduction;
            lastRunTime = Time.time;

            if (currentStamina <= 0)
            {
                currentStamina = 0f;
                StartCoroutine(IE_Exhaustion(exhaustionTime));
            }

            UIManager.Instance.UI_Status.RefreshStaminaUI(currentStamina, maxStamina);
        }
        else if (currentStamina < maxStamina)
        {
            if (Time.time - lastRunTime >= staminaRecoveryTime)
            {
                currentStamina += Time.deltaTime * staminaRecovery;
                UIManager.Instance.UI_Status.RefreshStaminaUI(currentStamina, maxStamina);
            }
        }
    }

    private IEnumerator IE_Exhaustion(float delay)
    {
        canRun = false;
        yield return new WaitForSeconds(delay);
        canRun = true;
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            currentHealth = 0f;

            Debug.Log($"GameOver");
            return;
        }

        UIManager.Instance.UI_Status.RefreshHealthUI(currentHealth, maxHealth);

        lastHitTime = Time.time;
        StartCoroutine(IE_Invincible(invincibleTime));
    }

    private IEnumerator IE_Invincible(float delay)
    {
        isInvincible = true;
        yield return new WaitForSeconds(delay);
        isInvincible = false;
    }
}