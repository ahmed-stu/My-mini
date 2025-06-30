// FishAI.cs

using UnityEngine;

/// <summary>
/// يتحكم بحركة السمكة في البيئة بشكل عشوائي ضمن حدود محددة.
/// </summary>
public class FishAI : MonoBehaviour
{
    [Header("Speed Settings")]
    [Tooltip("أقل سرعة للسمكة")]
    [SerializeField] private float minSpeed = 1f;
    [Tooltip("أعلى سرعة للسمكة")]
    [SerializeField] private float maxSpeed = 3f;

    [Header("Movement Settings")]
    [Tooltip("الفترة الزمنية قبل تغيير الاتجاه")]
    [SerializeField] private float changeDirectionInterval = 3f;
    [Tooltip("حدود منطقة السباحة (مساوية في المحاور X, Y, Z)")]
    [SerializeField] private float swimLimits = 10f;

    private Vector3 targetPosition;      // الوجهة الحالية للسمكة
    private float currentSpeed;          // السرعة الحالية للسمكة
    private float directionChangeTimer;  // مؤقت تغيير الاتجاه

    private void Start()
    {
        // تهيئة الوجهة والسرعة والموقت
        SetNewTargetPosition();
        currentSpeed = Random.Range(minSpeed, maxSpeed);
        directionChangeTimer = changeDirectionInterval;
    }

    private void Update()
    {
        MoveFish();  // تحريك السمكة نحو الوجهة

        // تحديث المؤقت، وعند انتهاءه نغير الوجهة
        directionChangeTimer -= Time.deltaTime;
        if (directionChangeTimer <= 0f)
        {
            SetNewTargetPosition();
            directionChangeTimer = changeDirectionInterval;
        }
    }

    /// <summary>
    /// ينقل السمكة تدريجيًا نحو الوجهة المحددة.
    /// </summary>
    private void MoveFish()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            currentSpeed * Time.deltaTime
        );

        // إذا وصلنا قريبًا جدًا من الوجهة، نختار وجهة جديدة
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            SetNewTargetPosition();
        }
    }

    /// <summary>
    /// يحدد وجهة عشوائية جديدة ضمن حدود السباحة.
    /// </summary>
    private void SetNewTargetPosition()
    {
        targetPosition = new Vector3(
            Random.Range(-swimLimits, swimLimits),
            Random.Range(-swimLimits, swimLimits),
            Random.Range(-swimLimits, swimLimits)
        );
    }

    // ملاحظة:
    // إذا رغبت في جعل السمكة تلاحق العوامة عند قربها:
    // تحقق من مسافة العوامة وأعد توجيه targetPosition نحو موقع العوامة.
}
