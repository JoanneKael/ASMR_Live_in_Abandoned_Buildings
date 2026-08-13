using UnityEngine;

/// <summary>
/// 플레이어 발소리 / 스턴 / 넉아웃 (2D · SFX_Player).
/// </summary>
public class PlayerAudio : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] private float walkStepInterval = 0.42f;
    [SerializeField] private float runStepInterval = 0.30f;
    [SerializeField] private float crouchStepInterval = 0.55f;
    [SerializeField] private float minMoveSpeed = 0.15f;
    [SerializeField] private float footstepVolume = 0.85f;

    [Header("Hit")]
    [SerializeField] private float stunVolume = 1f;
    [SerializeField] private float knockoutVolume = 1f;

    private Rigidbody _rb;
    private Player _player;
    private float _stepTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        UpdateFootsteps();
    }

    private void UpdateFootsteps()
    {
        if (_player == null || SoundManager.Instance == null) return;

        PlayerState state = _player.CurrentState;
        if (state == PlayerState.Hidden
            || state == PlayerState.Stunned
            || state == PlayerState.ASMR
            || state == PlayerState.Lobby
            || _player.IsHideTransitioning)
        {
            _stepTimer = 0f;
            return;
        }

        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked)
        {
            _stepTimer = 0f;
            return;
        }

        float speed = 0f;
        if (_rb != null)
        {
            Vector3 v = _rb.linearVelocity;
            v.y = 0f;
            speed = v.magnitude;
        }

        bool wantsMove = InputManager.Instance != null
            && InputManager.Instance.MoveValue.sqrMagnitude > 0.01f;

        if (!wantsMove || speed < minMoveSpeed)
        {
            _stepTimer = 0f;
            return;
        }

        float interval = walkStepInterval;
        if (state == PlayerState.Running) interval = runStepInterval;
        else if (state == PlayerState.Crouching) interval = crouchStepInterval;

        _stepTimer += Time.deltaTime;
        if (_stepTimer < interval) return;

        _stepTimer = 0f;
        SoundManager.Instance.PlayPlayerFromFolder(SoundManager.PathPlayerFootsteps, footstepVolume);
    }

    public void PlayStunned()
    {
        if (SoundManager.Instance == null) return;
        AudioClip clip = SoundManager.Instance.GetNamedClip(SoundManager.PlayerResourcesRoot, "stunned");
        if (clip != null)
            SoundManager.Instance.PlayPlayerOneShot(clip, stunVolume);
    }

    public void PlayKnockout()
    {
        if (SoundManager.Instance == null) return;
        AudioClip clip = SoundManager.Instance.GetNamedClip(SoundManager.PlayerResourcesRoot, "knockout");
        if (clip != null)
            SoundManager.Instance.PlayPlayerOneShot(clip, knockoutVolume);
    }
}
