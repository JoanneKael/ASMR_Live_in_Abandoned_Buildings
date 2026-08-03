

public enum PlayerState
{
    None,
    Idle,
    Walking,
    Running,
    Crouching,
    Interacting,
    ASMR,
    Exhaustion,
    Lobby,
    Hidden,
}

public enum ObjectType
{
    None,
    Door,
    ASMR,
    Consumable_HP,
    Consumable_SP,
    Key,
    LobbyPC,
    Hide,


}

public enum GameDifficulty
{
    Easy,
    Normal,
    Hard
}

public enum UnitState
{
    Idle,
    Patrol,     // 순찰 (배회)
    Investigate,// 수색 (소리 들은 곳 확인)
    Chase,      // 추적 (플레이어 발견 및 달리기)
    Attack      // 공격 (잡기/잡기 애니메이션 실행)
}

public enum InteractInputMode
{
    Tap,        // 누르면 즉시 Interact
    Hold,       // 길게 누르면 진행, 짧게 떼면 취소
    TapAndDrag  // 짧음=Interact, 김=드래그(TickHold)
}

public enum NoiseSource
{
    Microphone, // 마이크 비명
    Running,    // 달리기
    DoorTap,    // 문 탭 개폐 (드래그 제외)
    ASMRFail    // ASMR 미니게임 실패
}