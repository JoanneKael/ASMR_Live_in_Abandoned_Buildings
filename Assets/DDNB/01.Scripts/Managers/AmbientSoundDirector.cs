using UnityEngine;

/// <summary>
/// 인게임 시계가 도는 동안 Ambient 스팅어를 재생합니다.
/// - cinematic_*: 하루 중반 1회
/// - door_A_*: 1~2분 간격 랜덤
/// </summary>
public class AmbientSoundDirector : MonoBehaviour
{
    public static AmbientSoundDirector Instance { get; private set; }

    [SerializeField] private float cinematicAtNormalized = 0.5f;
    [SerializeField] private float creakMinInterval = 60f;
    [SerializeField] private float creakMaxInterval = 120f;

    private bool _active;
    private bool _paused;
    private float _dayDuration = 300f;
    private float _elapsed;
    private float _nextCreakAt;
    private bool _cinematicPlayed;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>하루 시계 시작과 함께 호출</summary>
    public void StartDayAmbient(float realDurationSeconds)
    {
        _dayDuration = Mathf.Max(30f, realDurationSeconds);
        _elapsed = 0f;
        _cinematicPlayed = false;
        _active = true;
        _paused = false;
        ScheduleNextCreak();
    }

    public void StopDayAmbient()
    {
        _active = false;
        _paused = false;
    }

    /// <summary>토스트/일시정지 등 — 타이머만 멈추고 진행도는 유지</summary>
    public void SetPaused(bool paused)
    {
        _paused = paused;
    }

    private void Update()
    {
        if (!_active || _paused) return;
        if (Time.timeScale <= 0f) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        _elapsed += Time.deltaTime;

        if (!_cinematicPlayed && _elapsed >= _dayDuration * cinematicAtNormalized)
        {
            _cinematicPlayed = true;
            PlayCinematic();
        }

        if (_elapsed >= _nextCreakAt)
        {
            PlayDoorCreak();
            ScheduleNextCreak();
        }
    }

    private void ScheduleNextCreak()
    {
        float interval = Random.Range(creakMinInterval, creakMaxInterval);
        _nextCreakAt = _elapsed + interval;
    }

    private void PlayCinematic()
    {
        if (SoundManager.Instance == null) return;
        AudioClip clip = SoundManager.Instance.GetRandomClipWithPrefix(
            SoundManager.PathSfxAmbient,
            "cinematic_");
        if (clip != null)
            SoundManager.Instance.PlayAmbientEnvOneShot(clip);
    }

    private void PlayDoorCreak()
    {
        if (SoundManager.Instance == null) return;
        AudioClip clip = SoundManager.Instance.GetRandomClipWithPrefix(
            SoundManager.PathSfxAmbient,
            "door_a_");
        if (clip != null)
            SoundManager.Instance.PlayAmbientEnvOneShot(clip);
    }
}
