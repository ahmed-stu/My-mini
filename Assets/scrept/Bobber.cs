// Bobber.cs

using UnityEngine;

/// <summary>
/// يتحكم في سلوك عوامة الصيد (Bobber)، بما في ذلك الكشف عن الماء وتشغيل مؤقت لدغة السمكة.
/// </summary>
public class Bobber : MonoBehaviour
{
    private Rigidbody rb;
    private FishingRodController fishingRodController;
    private LayerMask waterLayer;
    private LayerMask fishLayer;

    // --- معلمات لدغة السمكة ---
    private float minBiteTime = 5.0f;       // أقل وقت قبل احتمال اللدغة
    private float maxBiteTime = 20.0f;      // أقصى وقت قبل احتمال اللدغة
    private float biteProbability = 0.75f;  // احتمال حدوث اللدغة
    private float biteTimer;
    private bool isInWater = false;
    private bool biteTriggered = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Bobber requires a Rigidbody component!");
        }
    }

    /// <summary>
    /// تهيئة العوامة من قبل FishingRodController
    /// </summary>
    public void Initialize(FishingRodController controller, LayerMask water, LayerMask fish)
    {
        fishingRodController = controller;
        waterLayer = water;
        fishLayer = fish;
        isInWater = false;
        biteTriggered = false;

        rb.isKinematic = false;           // العوامة تبدأ بوضع غير حركي
        transform.SetParent(null);        // فك أي ارتباط أبوي سابق
    }

    private void Update()
    {
        // في حال كانت العوامة في الماء ولم تُلدغ بعد
        if (isInWater && !biteTriggered)
        {
            biteTimer -= Time.deltaTime;

            if (biteTimer <= 0)
            {
                TryFishBite(); // محاولة اللدغة
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // التحقق من ملامسة طبقة الماء
        if (((1 << other.gameObject.layer) & waterLayer) != 0)
        {
            if (!isInWater)
            {
                Debug.Log("Bobber hit water!");

                rb.isKinematic = true;                 // توقيف فيزياء العوامة
                transform.SetParent(other.transform);  // إلصاقها بالماء
                isInWater = true;

                StartBiteTimer();                      // بدء المؤقت
            }
        }

        // ملاحظة: يمكن مستقبلاً إضافة منطق للكشف عن الأسماك
    }

    /// <summary>
    /// بدء مؤقت انتظار لدغة السمكة
    /// </summary>
    public void StartBiteTimer()
    {
        biteTimer = Random.Range(minBiteTime, maxBiteTime);
        Debug.Log($"Bite timer started: {biteTimer:F2} seconds");
    }

    /// <summary>
    /// محاولة توليد لدغة سمكة بناءً على الاحتمال
    /// </summary>
    private void TryFishBite()
    {
        if (Random.value < biteProbability)
        {
            Debug.Log("🎣 A fish bit the line!");
            biteTriggered = true;

            // ⚠️ ممكن تضيف هنا تأثير بصري مثلاً يهتز أو يغوص قليلاً
            FishingEvents.TriggerFishBite(); // إطلاق الحدث
        }
        else
        {
            Debug.Log("No bite, resetting timer.");
            StartBiteTimer(); // إعادة محاولة لاحقة
        }
    }
}
