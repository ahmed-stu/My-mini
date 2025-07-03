// // ReelingMinigameLogic.cs

// using UnityEngine;
// using UnityEngine.UI;

// /// <summary>
// /// منطق اللعبة المصغرة لعملية السحب (Reeling).
// /// يتحكم بشد الخيط وخطر انقطاعه وصحة السمكة.
// /// </summary>
// public class ReelingMinigameLogic : MonoBehaviour
// {
//     [Header("Minigame Settings")]
//     [Tooltip("الحد الأقصى للشد قبل انقطاع الخيط")]
//     [SerializeField] private float maxTension = 100f;
//     [Tooltip("معدل زيادة الشد عند السحب")]
//     [SerializeField] private float reelRate = 30f;
//     [Tooltip("معدل انخفاض الشد عند تحرير المقبض")]
//     [SerializeField] private float releaseRate = 10f;
//     [Tooltip("معدل زيادة الشد بسبب صراع السمكة")]
//     [SerializeField] private float fishStruggleRate = 15f;
//     [Tooltip("صحة السمكة أو المسافة المتبقية لسحبها")]
//     [SerializeField] private float fishHealthMax = 100f;
//     [Tooltip("معدل انخفاض صحة السمكة عند السحب")]
//     [SerializeField] private float fishHealthDecreaseRate = 20f;

//     [Header("UI References")]
//     [Tooltip("شريط UI لعرض مقدار الشد الحالي")]
//     [SerializeField] private Slider tensionSlider;
//     [Tooltip("صورة التعبئة داخل الشريط لتغيير اللون")]
//     [SerializeField] private Image tensionFillImage;

//     [Header("Tension Colors")]
//     [Tooltip("اللون الآمن للشريط")]
//     [SerializeField] private Color safeColor = Color.green;
//     [Tooltip("لون التحذير للشريط")]
//     [SerializeField] private Color warningColor = Color.yellow;
//     [Tooltip("اللون الحرج للشريط")]
//     [SerializeField] private Color criticalColor = Color.red;

//     private float currentTension;
//     private float currentFishHealth;
//     private bool minigameActive = false;

//     private void Awake()
//     {
//         // تأكد من الربط في الـ Inspector
//         if (tensionSlider == null)
//             Debug.LogError("Tension Slider reference is missing in ReelingMinigameLogic!");
//         if (tensionFillImage == null)
//             Debug.LogError("Tension Fill Image reference is missing in ReelingMinigameLogic!");
//     }

//     private void Start()
//     {
//         // تهيئة الشريط
//         if (tensionSlider != null)
//         {
//             tensionSlider.maxValue = maxTension;
//             tensionSlider.value = 0f;
//         }
//         currentTension = 0f;
//     }

//     /// <summary>
//     /// تشغيل اللعبة المصغرة.
//     /// </summary>
//     public void StartMinigame()
//     {
//         minigameActive = true;
//         currentTension = maxTension * 0.2f; // ابدأ بنسبة 20% من الشد الأقصى
//         currentFishHealth = fishHealthMax;
//         UpdateTensionUI();
//         Debug.Log("Reeling Minigame Started!");
//     }

//     /// <summary>
//     /// إيقاف اللعبة المصغرة.
//     /// </summary>
//     public void StopMinigame()
//     {
//         minigameActive = false;
//         Debug.Log("Reeling Minigame Stopped!");
//     }

//     /// <summary>
//     /// تطبيق إدخال السحب.
//     /// </summary>
//     public void ApplyReelInput(float deltaTime)
//     {
//         if (!minigameActive) return;

//         currentTension += reelRate * deltaTime;
//         currentFishHealth -= fishHealthDecreaseRate * deltaTime;

//         CheckMinigameStatus(deltaTime);
//         UpdateTensionUI();
//     }

//     /// <summary>
//     /// تطبيق تحرير الشد.
//     /// </summary>
//     public void ReleaseReelInput(float deltaTime)
//     {
//         if (!minigameActive) return;

//         currentTension -= releaseRate * deltaTime;
//         CheckMinigameStatus(deltaTime);
//         UpdateTensionUI();
//     }

//     /// <summary>
//     /// فحص حالة اللعبة المصغرة (انقطاع الخيط أو صيد السمكة).
//     /// </summary>
//     private void CheckMinigameStatus(float deltaTime)
//     {
//         // زيادة الشد بسبب صراع السمكة
//         currentTension += fishStruggleRate * deltaTime;
//         currentTension = Mathf.Clamp(currentTension, 0f, maxTension);

//         if (currentTension >= maxTension)
//         {
//             Debug.Log("Line snapped! Too much tension.");
//             minigameActive = false;
//             FishingEvents.TriggerLineSnapped();
//         }
//         else if (currentFishHealth <= 0f)
//         {
//             Debug.Log("Fish caught!");
//             minigameActive = false;
//             FishingEvents.TriggerFishCaught();
//         }
//     }

//     /// <summary>
//     /// تحديث واجهة المستخدم الخاصة بالشريط، بما في ذلك اللون.
//     /// </summary>
//     private void UpdateTensionUI()
//     {
//         if (tensionSlider == null || tensionFillImage == null) return;

//         tensionSlider.value = currentTension;

//         // تغيير اللون بناءً على مستوى الشد
//         float ratio = currentTension / maxTension;
//         if (ratio < 0.5f)
//             tensionFillImage.color = safeColor;
//         else if (ratio < 0.8f)
//             tensionFillImage.color = warningColor;
//         else
//             tensionFillImage.color = criticalColor;
//     }
// }
