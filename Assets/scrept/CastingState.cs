// CastingState.cs

using UnityEngine;

/// <summary>
/// حالة رمي السنارة: عند إطلاق العوامة.
/// </summary>
public class CastingState : FishingRodBaseState
{
    private float castDuration = 1.0f; // مدة الرسوم المتحركة للرمي
    private float castTimer;

    public CastingState(FishingRodController controller, StateMachine<FishingRodBaseState> stateMachine, Animator animator)
        : base(controller, stateMachine, animator) { }

    public override void Enter()
    {
        Debug.Log("Entering Casting State");

        // تشغيل أنيميشن الرمي
        animator.SetTrigger("Cast");

        // تهيئة المؤقت
        castTimer = castDuration;

        // إطلاق العوامة
        fishingRodController.CastBobber();
    }

    public override void LogicUpdate()
    {
        castTimer -= Time.deltaTime;

        // عند انتهاء المؤقت، ننتقل إلى حالة انتظار السمكة
        if (castTimer <= 0)
        {
            stateMachine.ChangeState(fishingRodController.waitingForBiteState);
        }
    }

    public override void Exit()
    {
        Debug.Log("Exiting Casting State");
    }
}
