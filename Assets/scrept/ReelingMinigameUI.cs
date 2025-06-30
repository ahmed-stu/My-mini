// ReelingMinigameUI.cs

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// يدير واجهة المستخدم للعبة المصغرة للسحب، ويتفاعل مع أحداث الصيد.
/// </summary>
public class ReelingMinigameUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("لوحة اللعبة المصغرة (Minigame Panel)")]
    [SerializeField] private GameObject minigamePanel;

    [Tooltip("نص عرض رسالة صيد السمكة")]
    [SerializeField] private Text fishCaughtText;

    [Tooltip("نص عرض رسالة انقطاع الخيط")]
    [SerializeField] private Text lineSnappedText;

    private void Awake()
    {
        // إخفاء جميع عناصر UI في البداية
        if (minigamePanel != null) minigamePanel.SetActive(false);
        if (fishCaughtText != null) fishCaughtText.gameObject.SetActive(false);
        if (lineSnappedText != null) lineSnappedText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        FishingEvents.OnFishBite += ShowMinigamePanel;
        FishingEvents.OnFishCaught += ShowFishCaughtMessage;
        FishingEvents.OnLineSnapped += ShowLineSnappedMessage;
    }

    private void OnDisable()
    {
        FishingEvents.OnFishBite -= ShowMinigamePanel;
        FishingEvents.OnFishCaught -= ShowFishCaughtMessage;
        FishingEvents.OnLineSnapped -= ShowLineSnappedMessage;
    }

    /// <summary>
    /// عرض لوحة اللعبة المصغرة عند لدغة السمكة
    /// </summary>
    private void ShowMinigamePanel()
    {
        if (minigamePanel != null)
            minigamePanel.SetActive(true);

        if (fishCaughtText != null)
            fishCaughtText.gameObject.SetActive(false);

        if (lineSnappedText != null)
            lineSnappedText.gameObject.SetActive(false);
    }

    /// <summary>
    /// عرض رسالة صيد السمكة
    /// </summary>
    private void ShowFishCaughtMessage()
    {
        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (fishCaughtText != null)
        {
            fishCaughtText.text = "لقد اصطدت سمكة!";
            fishCaughtText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// عرض رسالة انقطاع الخيط
    /// </summary>
    private void ShowLineSnappedMessage()
    {
        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (lineSnappedText != null)
        {
            lineSnappedText.text = "انقطع الخيط!";
            lineSnappedText.gameObject.SetActive(true);
        }
    }

    // مستقبلاً: يمكن إضافة طرق لإخفاء الرسائل بعد فترة زمنية
}

