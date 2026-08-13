using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 유닛 발소리(3D World) + 위협/고스트 공격음(3D Threat).
/// </summary>
public class UnitAudio : MonoBehaviour
{
    [SerializeField] private AudioSource threatSource;
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private string threatChildName = "Audio_Threat";
    [SerializeField] private string footstepChildName = "Audio_Footsteps";

    [Header("Footsteps")]
    [SerializeField] private float walkStepInterval = 0.48f;
    [SerializeField] private float runStepInterval = 0.34f;
    [SerializeField] private float minMoveSpeed = 0.2f;
    [SerializeField] private float footstepVolume = 0.9f;

    [Header("Attack")]
    [SerializeField] private float attackVolume = 1f;

    private NavMeshAgent _agent;
    private UnitAI _unitAi;
    private float _stepTimer;

    public AudioSource ThreatSource
    {
        get
        {
            EnsureThreatSource();
            return threatSource;
        }
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _unitAi = GetComponent<UnitAI>();
        EnsureThreatSource();
        EnsureFootstepSource();
    }

    private void Update()
    {
        UpdateFootsteps();
    }

    private void UpdateFootsteps()
    {
        if (_agent == null || SoundManager.Instance == null) return;
        if (_unitAi != null && _unitAi.CurrentState == UnitState.Attack)
        {
            _stepTimer = 0f;
            return;
        }

        // OffMeshLink는 agent.velocity=0 → ForcedLocomotionSpeed(현재 walk/run)로 발소리 유지
        float speed;
        bool forcedMove = false;
        if (_unitAi != null && _unitAi.ForcedLocomotionSpeed.HasValue)
        {
            speed = _unitAi.ForcedLocomotionSpeed.Value;
            forcedMove = true;
        }
        else
        {
            Vector3 v = _agent.velocity;
            v.y = 0f;
            speed = v.magnitude;
        }

        if ((!forcedMove && _agent.isStopped) || speed < minMoveSpeed)
        {
            _stepTimer = 0f;
            return;
        }

        float interval = speed >= 3.5f ? runStepInterval : walkStepInterval;
        _stepTimer += Time.deltaTime;
        if (_stepTimer < interval) return;

        _stepTimer = 0f;
        AudioClip clip = SoundManager.Instance.GetRandomClipAt(SoundManager.PathUnitFootsteps);
        if (clip == null) return;

        EnsureFootstepSource();
        SoundManager.Instance.PlayOneShot(footstepSource, clip, footstepVolume);
    }

    /// <summary>공격 시 고스트 보이스 랜덤 재생</summary>
    public void PlayGhostAttack(float volumeScale = -1f)
    {
        if (SoundManager.Instance == null) return;
        AudioClip clip = SoundManager.Instance.GetRandomClipAt(SoundManager.PathUnitGhost);
        if (clip == null) return;

        EnsureThreatSource();
        float vol = volumeScale < 0f ? attackVolume : volumeScale;
        SoundManager.Instance.PlayOneShot(threatSource, clip, vol);
    }

    private void EnsureThreatSource()
    {
        if (threatSource != null)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.ConfigureThreatSource(threatSource);
            return;
        }

        Transform child = transform.Find(threatChildName);
        if (child != null)
            threatSource = child.GetComponent<AudioSource>();

        if (threatSource == null)
            threatSource = GetComponent<AudioSource>();

        if (threatSource == null)
        {
            GameObject go = new GameObject(threatChildName);
            go.transform.SetParent(transform, false);
            threatSource = go.AddComponent<AudioSource>();
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.ConfigureThreatSource(threatSource);
        else
            ApplyFallback3D(threatSource, 2f, 12f);
    }

    private void EnsureFootstepSource()
    {
        if (footstepSource != null)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.ConfigureWorldSfxSource(footstepSource, 1.2f, 12f);
            return;
        }

        Transform child = transform.Find(footstepChildName);
        if (child != null)
            footstepSource = child.GetComponent<AudioSource>();

        if (footstepSource == null)
        {
            GameObject go = new GameObject(footstepChildName);
            go.transform.SetParent(transform, false);
            footstepSource = go.AddComponent<AudioSource>();
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.ConfigureWorldSfxSource(footstepSource, 1.2f, 12f);
        else
            ApplyFallback3D(footstepSource, 1.2f, 12f);
    }

    private static void ApplyFallback3D(AudioSource source, float minDist, float maxDist)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDist;
        source.maxDistance = maxDist;
        source.dopplerLevel = 0f;
    }

    public void PlayThreatOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        EnsureThreatSource();
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayOneShot(threatSource, clip, volume);
        else
            threatSource.PlayOneShot(clip, volume);
    }

    public void PlayThreatLoop(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        EnsureThreatSource();
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayLoop(threatSource, clip, volume);
        else
        {
            threatSource.clip = clip;
            threatSource.loop = true;
            threatSource.volume = volume;
            threatSource.Play();
        }
    }

    public void StopThreat()
    {
        if (threatSource == null) return;
        if (SoundManager.Instance != null)
            SoundManager.Instance.Stop(threatSource);
        else
            threatSource.Stop();
    }
}
