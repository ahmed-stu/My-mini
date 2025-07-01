
// FishAI.cs

using UnityEngine;

/// <summary>
/// يتحكم بحركة السمكة: تتجول بشكل عشوائي وعند اكتشاف الطُعم تتبعه بسلاسة.
/// </summary>
public class FishAI : MonoBehaviour
{
    #region Settings
    [Header("Movement Settings")]
    [Tooltip("السرعة الدنيا للسمكة أثناء التجوال")]
    [SerializeField] private float minSpeed = 1f;
    [Tooltip("السرعة العليا للسمكة أثناء التجوال")]
    [SerializeField] private float maxSpeed = 3f;
    [Tooltip("سرعة الدوران لتوجيه السمكة نحو الوجهة")]
    [SerializeField] private float rotationSpeed = 2f;

    [Header("Wander Settings")]
    [Tooltip("المسافة الأفقية لمنطقة التجوال")]
    [SerializeField] private float swimLimit = 10f;
    [Tooltip("الفاصل الزمني لتغيير الوجهة العشوائية")]
    [SerializeField] private float changeDirectionInterval = 3f;

    [Header("Bait Follow Settings")]
    [Tooltip("نصف قطر اكتشاف الطُعم")]
    [SerializeField] private float baitDetectionRadius = 5f;
    [Tooltip("مضاعف السرعة عند اتباع الطُعم")]
    [SerializeField] private float followSpeedMultiplier = 1.5f;
    [Tooltip("المسافة التي تكون عندها السمكة قريبة كفاية")]
    [SerializeField] private float approachDistance = 0.5f;

    [Header("Speed Smoothing")]
    [Tooltip("معدل تنعيم التغيير في السرعة")]
    [SerializeField] private float speedDamping = 2f;
    #endregion

    #region State
    private Vector3 wanderTarget;
    private float currentSpeed;
    private float baseSpeed;
    private float directionTimer;

    private Bobber currentBobber;
    #endregion

    #region Unity Methods
    private void Start()
    {
        SetNewWanderTarget();
        directionTimer = changeDirectionInterval;
        baseSpeed = Random.Range(minSpeed, maxSpeed);
        currentSpeed = baseSpeed;
    }

    private void Update()
    {
        AcquireBobberReference();
        if (currentBobber != null && Vector3.Distance(transform.position, currentBobber.transform.position) <= baitDetectionRadius)
            FollowBait();
        else
            Wander();

        ConstrainVerticalPosition();
    }

    private void OnDrawGizmosSelected()
    {
        // عرض نصف قطر اكتشاف الطُعم
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, baitDetectionRadius);
    }
    #endregion

    #region Behavior
    private void Wander()
    {
        // تغيير الوجهة بعد انقضاء المؤقت
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0f)
        {
            SetNewWanderTarget();
            directionTimer = changeDirectionInterval;
            baseSpeed = Random.Range(minSpeed, maxSpeed);
        }

        MoveTowards(wanderTarget, baseSpeed);
    }

    private void FollowBait()
    {
        Vector3 baitPos = currentBobber.transform.position;
        float distance = Vector3.Distance(transform.position, baitPos);

        float targetSpeed = (distance < approachDistance) ? 0f : baseSpeed * followSpeedMultiplier;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * speedDamping);

        MoveTowards(baitPos, currentSpeed);
    }
    #endregion

    #region Helpers
    private void MoveTowards(Vector3 destination, float speed)
    {
        // حركة خطية
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);

        // تدوير نحو الوجهة
        Vector3 dir = (destination - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void SetNewWanderTarget()
    {
        wanderTarget = new Vector3(
            Random.Range(-swimLimit, swimLimit),
            transform.position.y,
            Random.Range(-swimLimit, swimLimit)
        );
    }

    private void ConstrainVerticalPosition()
    {
        // احتفظ بمستوى الماء y=0 أو أي قيمة ترغب
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, -Mathf.Abs(transform.position.y), 0f);
        transform.position = pos;
    }

    private void AcquireBobberReference()
    {
        if (currentBobber == null)
            currentBobber = FindAnyObjectByType<Bobber>();
    }
    #endregion
}

