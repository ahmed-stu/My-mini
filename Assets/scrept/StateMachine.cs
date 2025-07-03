// // StateMachine.cs

// using UnityEngine;

// /// <summary>
// /// نظام الحالة العام (Generic State Machine).
// /// يدير الانتقال بين الحالات التي ترث من IState.
// /// </summary>
// /// <typeparam name="T">نوع الحالة الذي يرث من IState.</typeparam>
// public class StateMachine<T> where T : IState
// {
//     /// <summary>
//     /// الحالة الحالية للنظام.
// /// </summary>
//     public T CurrentState { get; private set; }

//     /// <summary>
//     /// يهيئ النظام بحالة ابتدائية ويدعو دالة الدخول (Enter) لتلك الحالة.
// /// </summary>
//     /// <param name="startingState">الحالة التي يبدأ بها النظام.</param>
//     public void Initialize(T startingState)
//     {
//         CurrentState = startingState;
//         CurrentState.Enter();
//     }

//     /// <summary>
//     /// يغير الحالة إلى الحالة الجديدة:
//     /// - يستدعي Exit() على الحالة القديمة  
//     /// - يعيّن الحالة الجديدة  
//     /// - يستدعي Enter() على الحالة الجديدة  
// /// </summary>
//     /// <param name="newState">الحالة المراد الانتقال إليها.</param>
//     public void ChangeState(T newState)
//     {
//         CurrentState.Exit();
//         CurrentState = newState;
//         CurrentState.Enter();
//     }
// }
