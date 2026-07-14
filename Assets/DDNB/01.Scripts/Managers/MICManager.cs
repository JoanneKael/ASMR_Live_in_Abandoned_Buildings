using System;
using UnityEngine;

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
    [SerializeField] private float minScreamDuration = 0.3f;

    private float screamTimer = 0.0f;

    public event Action<float> OnVolumeChanged;
    public event Action OnNoiseDetected;

    public float CurrentDB {get; private set;}

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        //PlayerPrefs.GetFloat("MICCalibration", calibrationValue);

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

    void Update()
    {
        if (micClip == null) return;

        // 볼륨 데이터 추출
        CurrentDB = GetCalibratedDb();

        // 볼륨 체크
        CheckVolume(CurrentDB);

        // UI 반영
        RefreshDecibelUI(CurrentDB);
        
    }

    public void RefreshDecibelUI(float currentDb)
    {
        float normalizedVolume = Mathf.Clamp01(Mathf.InverseLerp(-60f, 0f, currentDb));
        OnVolumeChanged?.Invoke(normalizedVolume);
    }

    public bool CheckASMR()
    {
        if (GameManager.Instance.Player.CurrentState == PlayerState.ASMR && asmrMinDb <= CurrentDB && CurrentDB < thresholdDb)
        {
            return true;
        }
        return false;
    }

    private void CheckVolume(float db)
    {
        if (db >= thresholdDb)
        {
            screamTimer += Time.deltaTime;
            if (screamTimer >= minScreamDuration)
            {
                OnNoiseDetected?.Invoke();
                screamTimer = 0f;
            }
        }
        else
        {
            screamTimer = Mathf.Max(0, screamTimer - Time.deltaTime * 2f);
        }
    }

    private float GetCalibratedDb()
    {
        // 데이터 추출
        int micPosition = Microphone.GetPosition(selectedMic);
        micClip.GetData(samples, micPosition - samples.Length < 0 ? 0 : micPosition - samples.Length);

        // RMS 계산
        float sum = 0f;
        foreach (float s in samples) sum += s * s;
        float rms = Mathf.Sqrt(sum / samples.Length);

        // RMS 증폭
        float boostedRMS = rms * calibrationValue;

        // 데시벨 변환
        float db = 20 * Mathf.Log10(boostedRMS + 0.0001f);

        return db;
    }

    public void SetCalibration(float newValue)
    {
        calibrationValue = newValue;
        //PlayerPrefs.SetFloat("MICCalibration", calibrationValue);
    }
}