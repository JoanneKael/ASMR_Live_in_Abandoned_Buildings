using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 인게임 시계 UI. 게임 시간 00:00~06:00을 20분 간격으로 표시합니다.
/// 시간 진행은 GameManager가 담당하고, 이 UI는 표시만 갱신합니다.
/// </summary>
public class UI_IngameClock : UI_Base
{
    [SerializeField] private TextMeshProUGUI timeText;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnIngameClockChanged += RefreshTime;
            RefreshTime(GameManager.Instance.IngameClockDisplay);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnIngameClockChanged -= RefreshTime;
    }

    public void RefreshTime(string timeDisplay)
    {
        if (timeText != null)
            timeText.text = timeDisplay;
    }
}
