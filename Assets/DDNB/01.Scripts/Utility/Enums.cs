

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