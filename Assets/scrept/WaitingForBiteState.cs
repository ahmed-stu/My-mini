// WaitingForBiteState.cs

using UnityEngine;

/// <summary>
/// حالة الانتظار لدغة السمكة: تبدأ مؤقت اللدغة وتنتقل إلى حالة السحب عند اللدغة.
/// </summary>
public class WaitingForBiteState : FishingRodBaseState
{
    public WaitingForBiteState(
        FishingRodController controller,
        StateMachine<FishingRodBaseState> stateMachine,
        Animator animator
    ) : base(controller, stateMachine, animator) { }

    public override void Enter()
    {
        Debug.Log("Entering WaitingForBite State");

        // بدء مؤقت اللدغة في العوامة
        fishingRodController.bobberInstance.ResetBiteTimer();

        // الاشتراك في حدث لدغة السمكة
        FishingEvents.OnFishBite += OnFishBite;
    }

    public override void LogicUpdate()
    {
        // لا حاجة لتحديث مستمر؛ ننتظر حدث اللدغة
    }

    public override void PhysicsUpdate()
    {
        // لا يوجد منطق فيزيائي لهذه الحالة
    }

    private void OnFishBite()
    {
        Debug.Log("Fish bit!");
        // الانتقال إلى حالة السحب
        stateMachine.ChangeState(fishingRodController.reelingState);
    }

    public override void Exit()
    {
        Debug.Log("Exiting WaitingForBite State");

        // إلغاء الاشتراك في حدث لدغة السمكة
        FishingEvents.OnFishBite -= OnFishBite;
    }
}
