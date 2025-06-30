// FishingEvents.cs

using System;
using UnityEngine;

/// <summary>
/// نظام أحداث الصيد (Fishing Events)
/// يوفر إشعارات عند وقوع أحداث مهمة مثل لدغة السمكة أو انقطاع الخيط.
/// </summary>
public static class FishingEvents
{
    /// <summary>
    /// يُطلق عند لدغة السمكة للسنارة.
    /// </summary>
    public static event Action OnFishBite;

    /// <summary>
    /// يُطلق عند نجاح اللاعب في صيد السمكة.
    /// </summary>
    public static event Action OnFishCaught;

    /// <summary>
    /// يُطلق عند انقطاع خيط السنارة (فشل الصيد).
    /// </summary>
    public static event Action OnLineSnapped;

    /// <summary>
    /// يستدعي حدث لدغة السمكة.
    /// </summary>
    public static void TriggerFishBite()
    {
        Debug.Log("Event: Fish Bite Triggered");
        OnFishBite?.Invoke();
    }

    /// <summary>
    /// يستدعي حدث صيد السمكة.
    /// </summary>
    public static void TriggerFishCaught()
    {
        Debug.Log("Event: Fish Caught Triggered");
        OnFishCaught?.Invoke();
    }

    /// <summary>
    /// يستدعي حدث انقطاع الخيط.
    /// </summary>
    public static void TriggerLineSnapped()
    {
        Debug.Log("Event: Line Snapped Triggered");
        OnLineSnapped?.Invoke();
    }
}
