// LineSnappedState.cs

using UnityEngine;

/// <summary>
/// حالة انقطاع الخيط: تعرض رسالة لفترة قصيرة ثم تعيد حالة الخمول.
/// </summary>
public class LineSnappedState : FishingRodBaseState
{
    [Header("Reset Settings")]
    [Tooltip("مدة عرض واجهة انقطاع الخيط (بالثواني)")]
    [SerializeField] private float resetDuration = 2.0f;

    private float resetTimer;

    public LineSnappedState(
        FishingRodController controller,
        StateMachine<FishingRodBaseState> stateMachine,
        Animator animator
    ) : base(controller, stateMachine, animator) { }

    public override void Enter()
    {
        Debug.Log("Entering LineSnapped State");

        // تفعيل رسوم متحركة انقطاع الخيط
        animator.SetTrigger("LineSnap");

        // عرض واجهة المستخدم المناسبة
        fishingRodController.DisplayLineSnappedUI();

        // تهيئة المؤقت
        resetTimer = resetDuration;
    }

    public override void LogicUpdate()
    {
        // عد تنازلي حتى انتهاء المؤقت
        resetTimer -= Time.deltaTime;
        if (resetTimer <= 0f)
        {
            // العودة إلى حالة الخمول
            stateMachine.ChangeState(fishingRodController.idleState);
        }
    }

    public override void Exit()
    {
        Debug.Log("Exiting LineSnapped State");

        // إخفاء واجهة انقطاع الخيط
        fishingRodController.HideLineSnappedUI();

        // إخفاء العوامة والخيط استعدادًا للدور التالي
        fishingRodController.HideBobberAndLine();
    }
}
