using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 공격 상태 커맨드.
/// 이동을 멈추고 공격 애니메이션 대기 후 플레이어에게 데미지를 입히고 유닛을 제거합니다.
/// </summary>
public class AttackCommand : IUnitCommand
{
    private readonly UnitAIContext _context;

    public AttackCommand(UnitAIContext context)
    {
        _context = context;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        _context.Agent.isStopped = true;
        Debug.Log("공격 시작!");

        // 공격 애니메이션 재생 대기
        await UniTask.Delay(TimeSpan.FromSeconds(2.0f), cancellationToken: token);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Player.GetComponent<PlayerStatus>().TakeDamage(_context.UnitData.damage);
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnUnitDespawned();
        }

        UnityEngine.Object.Destroy(_context.Transform.gameObject);
    }
}
