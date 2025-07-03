// // ReelingState.cs

// using UnityEngine;
// using UnityEngine.InputSystem;

// #region Summary
// /// <summary>
// /// حالة السحب: تتحكم في عملية سحب السمكة وتشغيل اللعبة المصغرة وإدارة الأحداث.
// /// </summary>
// #endregion
// public class ReelingState : FishingRodBaseState
// {
//     public ReelingState(
//         FishingRodController controller,
//         StateMachine<FishingRodBaseState> stateMachine,
//         Animator animator
//     ) : base(controller, stateMachine, animator) { }

//     public override void Enter()
//     {
//         Debug.Log("Entering Reeling State");

//         // تفعيل رسمية السحب في Animator
//         animator.SetBool("IsReeling", true);

//         // بدء اللعبة المصغرة للسحب
//         fishingRodController.StartReelingMinigame();

//         // الاشتراك في أحداث الصيد
//         FishingEvents.OnFishCaught   += OnFishCaught;
//         FishingEvents.OnLineSnapped += OnLineSnapped;
//     }

//     public override void LogicUpdate()
//     {
//         // إذا مستمر في الضغط، نسحب؛ وإلا نخفض التوتر
//         if (fishingRodController.IsReelingInputHeld())
//             fishingRodController.ReelIn(Time.deltaTime);
//         else
//             fishingRodController.ReleaseReel(Time.deltaTime);
//     }

//     private void OnFishCaught()
//     {
//         Debug.Log("Fish Caught!");
//         stateMachine.ChangeState(fishingRodController.fishCaughtState);
//     }

//     private void OnLineSnapped()
//     {
//         Debug.Log("Line Snapped!");
//         stateMachine.ChangeState(fishingRodController.lineSnappedState);
//     }

//     public override void Exit()
//     {
//         Debug.Log("Exiting Reeling State");

//         // إيقاف رسمية السحب في Animator
//         animator.SetBool("IsReeling", false);

//         // إلغاء الاشتراك في الأحداث لتجنب النداء المزدوج
//         FishingEvents.OnFishCaught   -= OnFishCaught;
//         FishingEvents.OnLineSnapped -= OnLineSnapped;

//         // إنهاء اللعبة المصغرة
//         fishingRodController.StopReelingMinigame();
//     }
// }
