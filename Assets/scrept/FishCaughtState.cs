// FishCaughtState.cs

using UnityEngine;

/// <summary>
/// حالة ما بعد صيد السمكة: تعرض واجهة المستخدم لرسالة الصيد لفترة قصيرة ثم تعود إلى حالة الخمول.
/// </summary>
public class FishCaughtState : FishingRodBaseState
{
    [Header("Display Settings")]
    [Tooltip("مدة عرض واجهة صيد السمكة (بالثواني)")]
    [SerializeField] private float displayDuration = 3.0f;

    private float displayTimer;

    public FishCaughtState(
        FishingRodController controller,
        StateMachine<FishingRodBaseState> stateMachine,
        Animator animator
    ) : base(controller, stateMachine, animator) { }

    public override void Enter()
    {
        Debug.Log("Entering FishCaught State");

        // تشغيل أنيميشن صيد السمكة
        animator.SetTrigger("CatchFish");

        // إظهار واجهة المستخدم
        fishingRodController.DisplayFishCaughtUI();

        // تهيئة المؤقت
        displayTimer = displayDuration;
    }

    public override void LogicUpdate()
    {
        // عدّ تنازلي حتى انتهاء مدة العرض
        displayTimer -= Time.deltaTime;
        if (displayTimer <= 0f)
        {
            // العودة إلى حالة الخمول
            stateMachine.ChangeState(fishingRodController.idleState);
        }
    }

    public override void Exit()
    {
        Debug.Log("Exiting FishCaught State");

        // إخفاء واجهة المستخدم
        fishingRodController.HideFishCaughtUI();

        // إخفاء العوامة والخيط استعدادًا للحالة التالية
        fishingRodController.HideBobberAndLine();
    }
}
