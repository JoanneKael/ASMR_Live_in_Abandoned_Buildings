using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 유닛 AI 행동 커맨드 공통 인터페이스.
/// 각 상태(Patrol, Chase 등)는 이 인터페이스를 구현한 독립 클래스로 분리됩니다.
/// </summary>
public interface IUnitCommand
{
    /// <summary>
    /// 커맨드를 비동기로 실행합니다.
    /// 상태 전환 시 CancellationToken으로 즉시 안전하게 중단됩니다.
    /// </summary>
    UniTask ExecuteAsync(CancellationToken token);
}
