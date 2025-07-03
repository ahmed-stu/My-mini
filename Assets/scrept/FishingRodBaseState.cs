// // FishingRodBaseState.cs

// using UnityEngine;

// /// <summary>
// /// الكلاس الأساسي (Base) لجميع حالات سنارة الصيد (FSM).
// /// يحتوي على المراجع المشتركة مثل الـ Controller، ونظام الحالات، وAnimator.
// /// ويرث من واجهة IState التي تُلزم كل حالة بتنفيذ الدوال الأساسية.
// /// </summary>
// public abstract class FishingRodBaseState : IState
// {
//     protected FishingRodController fishingRodController;          // المرجع إلى Controller الرئيسي
//     protected StateMachine<FishingRodBaseState> stateMachine;    // نظام الحالة للتحكم بالتبديل بين الحالات
//     protected Animator animator;                                  // Animator للتحكم بالرسوم المتحركة

//     /// <summary>
//     /// المُنشئ: يستقبل المراجع اللازمة لكل حالة.
//     /// </summary>
//     protected FishingRodBaseState(
//         FishingRodController controller,
//         StateMachine<FishingRodBaseState> stateMachine,
//         Animator animator
//     )
//     {
//         this.fishingRodController = controller;
//         this.stateMachine = stateMachine;
//         this.animator = animator;
//     }

//     /// <summary>
//     /// يتم استدعاؤه عند الدخول إلى الحالة.
//     /// </summary>
//     public virtual void Enter() { }

//     /// <summary>
//     /// يتم استدعاؤه عند الخروج من الحالة.
//     /// </summary>
//     public virtual void Exit() { }

//     /// <summary>
//     /// التحديث المنطقي (Logic) ويتم استدعاؤه في Update().
//     /// </summary>
//     public virtual void LogicUpdate() { }

//     /// <summary>
//     /// التحديث الفيزيائي ويتم استدعاؤه في FixedUpdate().
//     /// </summary>
//     public virtual void PhysicsUpdate() { }
// }
