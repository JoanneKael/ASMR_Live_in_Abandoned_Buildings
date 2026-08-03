using UnityEngine;

public interface IInteractable
{
    /// <summary>
    /// 스크립터블 오브젝트
    /// </summary>
    InteractableData Data { get; }

    /// <summary>
    /// 입력 해석 방식
    /// </summary>
    InteractInputMode InputMode { get; }

    /// <summary>
    /// 일반(탭) 상호작용
    /// </summary>
    void Interact();

    /// <summary>
    /// 홀드 판정 시작
    /// </summary>
    void BeginHold();

    /// <summary>
    /// 홀드 유지 중 매 프레임
    /// </summary>
    void TickHold(Vector2 lookDelta);

    /// <summary>
    /// 홀드 종료 (손을 뗌)
    /// </summary>
    void EndHold(bool wasHeld);

    /// <summary>
    /// 홀드 취소
    /// </summary>
    void CancelHold();
}
