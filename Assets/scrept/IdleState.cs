// IdleState.cs

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// حالة الخمول (Idle): عندما لا يقوم اللاعب بأي حركة.
/// </summary>
public class IdleState : FishingRodBaseState
{
    public IdleState(FishingRodController controller, StateMachine<FishingRodBaseState> stateMachine, Animator animator)
        : base(controller, stateMachine, animator) { }

    public override void Enter()
    {
        Debug.Log("Entering Idle State");

        // تعطيل الحركة في الأنيميشن
        animator.SetBool("IsFishing", false); // تأكد من وجود متغير IsFishing داخل Animator

        // إخفاء السنارة والخيط
        fishingRodController.HideBobberAndLine();
    }

    public override void LogicUpdate()
    {
        // إذا ضغط اللاعب زر الرمي، ننتقل لحالة الرمي
        if (fishingRodController.IsCastingInputPressed())
        {
            stateMachine.ChangeState(fishingRodController.castingState);
        }
    }

    public override void Exit()
    {
        Debug.Log("Exiting Idle State");
    }
}
