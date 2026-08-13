using System;

/// <summary>게임 전역 이벤트 (매니저 간 느슨한 연결)</summary>
public static class GameEvents
{
    /// <summary>유닛이 플레이어 공격에 성공했을 때</summary>
    public static event Action OnUnitAttackSucceeded;

    /// <summary>퀘스트 목표 문구가 바뀌어야 할 때 (맵 선택·미션 달성 등)</summary>
    public static event Action OnQuestObjectiveChanged;

    public static void RaiseUnitAttackSucceeded()
    {
        OnUnitAttackSucceeded?.Invoke();
    }

    public static void RaiseQuestObjectiveChanged()
    {
        OnQuestObjectiveChanged?.Invoke();
    }
}
