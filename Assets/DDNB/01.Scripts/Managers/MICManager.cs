using UnityEngine;

/// <summary>
/// 마이크 캡처 및 dB 측정. 소음 이벤트/UI는 NoiseManager가 담당합니다.
/// </summary>
public class MICManager : MonoBehaviour
{
    public static MICManager Instance;

    [Header("Settings")]
    private string selectedMic;
    private AudioClip micClip;
    private float[] samples = new float[256];

    [Header("Calibration")]
    [Range(0.5f, 2.5f)][SerializeField] private float calibrationValue = 1.0f;

    [Header("Judges")]
    [SerializeField] private float thresholdDb = -10f;
    [SerializeField] private float asmrMinDb = -30f;

    public float CurrentDB { get; private set; }
    public float ThresholdDb => thresholdDb;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            selectedMic = Microphone.devices[0];
            micClip = Microphone.Start(selectedMic, true, 1, 44100);
        }
        else
        {
            Debug.LogError("마이크 장치를 찾을 수 없습니다.");
        }
    }

    private void Update()
    {
        if (micClip == null) return;

        CurrentDB = GetCalibratedDb();

        // 임계 이상이면 매 프레임 보고 (AI 이벤트는 NoiseManager 전역 쿨다운)
        if (CurrentDB < thresholdDb) return;
        if (NoiseManager.Instance == null) return;

        Player player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player == null) return;

        NoiseManager.Instance.ReportNoise(NoiseSource.Microphone, player.transform.position, bypassCooldown: false);
    }

    public bool CheckASMR()
    {
        if (GameManager.Instance == null || GameManager.Instance.Player == null) return false;

        return GameManager.Instance.Player.CurrentState == PlayerState.ASMR
            && asmrMinDb <= CurrentDB
            && CurrentDB < thresholdDb;
    }

    private float GetCalibratedDb()
    {
        int micPosition = Microphone.GetPosition(selectedMic);
        micClip.GetData(samples, micPosition - samples.Length < 0 ? 0 : micPosition - samples.Length);

        float sum = 0f;
        foreach (float s in samples) sum += s * s;
        float rms = Mathf.Sqrt(sum / samples.Length);
        float boostedRMS = rms * calibrationValue;
        return 20f * Mathf.Log10(boostedRMS + 0.0001f);
    }

    public void SetCalibration(float newValue)
    {
        calibrationValue = newValue;
    }
}
