// Bobber.cs

using UnityEngine;

/// <summary>
/// يتحكم بسلوك العوامة، بما في ذلك الطفو واكتشاف لدغة السمكة.
/// </summary>
public class Bobber : MonoBehaviour
{
    // مرجع لـ FishingRodController
    // لا تحتاج لأنها تُعيّن برمجياً
    private FishingRodController fishingRodController;
    private LayerMask waterLayer;
    private LayerMask fishLayer;

    // متغيرات الطفو
   
   
    private float buoyancyForce     = 10f;   // قوة الطفو للأعلى
   
    private float waterDrag         = 1f;    // مقاومة الماء للحركة
   
    private float waterAngularDrag = 1f;    // مقاومة الماء للدوران
   
    private float airDrag           = 0f;    // مقاومة الهواء
   
    private float airAngularDrag   = 0.05f; // مقاومة الهواء للدوران

    // متغيرات لدغة السمكة
   
   
    private float minBiteTime       = 5f;   // أقل وقت قبل احتمال اللدغة
   
    private float maxBiteTime       = 20f;  // أقصى وقت قبل احتمال اللدغة
   
    private float biteProbability   = 0.75f;// احتمال اللدغة
    private float   biteTimer;
    private bool    biteTriggered = false;

    private Rigidbody rb;
    public bool     IsInWater     { get; private set; } // لتتبع ما إذا كانت العوامة في الماء

    /// <summary>
    /// يُستدعى عند Instantiate لإعداد العوامة.
    /// </summary>
    /// <param name="controller">مرجع لـ FishingRodController.</param>
    /// <param name="water">طبقة الماء.</param>
    /// <param name="fish">طبقة الأسماك.</param>
    public void Initialize(FishingRodController controller, LayerMask water, LayerMask fish)
    {
        fishingRodController = controller;
        waterLayer           = water;
        fishLayer            = fish;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError(" Rigidbody missing on this GameObject!", this);

        // لم نعد نضبط isKinematic هنا، سيتم ضبطها بواسطة FishingRodController بعد الرمي
        IsInWater            = false;

        // ضب مؤقت اللدغة
        ResetBiteTimer();
        biteTriggered        = false;

        Debug.Log(" Initialized", this);
    }

    void FixedUpdate() // FixedUpdate أفضل لتطبيق القوى الفيزيائية [1]
    {
        if (rb == null) return;

        if (IsInWater)
        {
            // تطبيق قوة الطفو [2, 3, 4, 5]
            rb.AddForce(Vector3.up * buoyancyForce, ForceMode.Acceleration);
            // تطبيق مقاومة الماء 
            rb.linearDamping       = waterDrag;
            rb.angularDamping = waterAngularDrag;
        }
        else
        {
            // تطبيق مقاومة الهواء عندما تكون خارج الماء
            rb.linearDamping        = airDrag;
            rb.angularDamping= airAngularDrag;
        }
    }

    void Update()
    {
        if (!IsInWater || biteTriggered)
            return;

        // عد تنازلي لللدغة [6]
        biteTimer -= Time.deltaTime;
        if (biteTimer <= 0f)
            TryFishBite();
    }

    void OnTriggerEnter(Collider other)
    {
        // تحقق مما إذا كان الكائن الذي دخلت إليه العوامة هو طبقة الماء [4, 5]
        if (((1 << other.gameObject.layer) & waterLayer)!= 0)
        {
            IsInWater = true;
            // لم نعد نضبط isKinematic هنا، تم نقلها إلى FishingRodController
            Debug.Log(" Entered water", this);
            // يمكنك تشغيل تأثيرات صوتية أو بصرية هنا (مثل رشاش الماء) 
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & waterLayer)!= 0)
        {
            IsInWater = false;
            Debug.Log(" Exited water", this);
        }
    }

    /// <summary>
    /// يضبط مؤقت اللدغة إلى قيمة عشوائية بين minBiteTime و maxBiteTime. [6]
    /// </summary>
    public void ResetBiteTimer() // *** تم تغييرها إلى public ***
    {
        biteTimer = Random.Range(minBiteTime, maxBiteTime);
        Debug.Log($" Bite timer reset to {biteTimer:F2}s", this);
    }

    /// <summary>
    /// محاولة توليد لدغة، بناءً على الاحتمال. [6, 7]
    /// </summary>
    private void TryFishBite()
    {
        if (Random.value <= biteProbability)
        {
            biteTriggered = true;
            Debug.Log(" Fish bite!", this);
            // هنا يجب أن تقوم بتغيير حالة الصنارة إلى "FishOn" أو بدء اللعبة المصغرة
            // يمكنك استخدام حدث (Event) أو استدعاء دالة مباشرة على FishingRodController
            // مثال: fishingRodController.stateMachine.ChangeState(fishingRodController.reelingState);
            // أو: FishingEvents.TriggerFishBite(); // إذا كان لديك نظام أحداث
        }
        else
        {
            Debug.Log(" No bite, resetting timer", this);
            ResetBiteTimer();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}