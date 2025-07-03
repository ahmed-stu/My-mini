// ## FINAL ROBUST PHYSICS VERSION ##
// Bobber.cs

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bobber : MonoBehaviour
{
    private FishingRodController fishingRodController;
    private Rigidbody rb;
    private float waterSurfaceY;
    private bool hasEnteredWater = false; // To ensure the splash happens only once

    [Header("Buoyancy Settings")]
    [SerializeField] private float buoyancyMultiplier = 2.0f;
    [SerializeField] private float waterDrag = 3f;
    [SerializeField] private float waterAngularDrag = 4f;
    
    // The Initialize function is now simpler
    public void Initialize(FishingRodController controller, float surfaceY)
    {
        fishingRodController = controller;
        waterSurfaceY = surfaceY;
        rb = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Make sure "Use Gravity" is checked on the prefab's Rigidbody
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        
        bool isInWater = transform.position.y < waterSurfaceY;

        // ** NEW ROBUST LOGIC: Check position to detect entering water **
        if (isInWater && !hasEnteredWater)
        {
            hasEnteredWater = true;
            fishingRodController.OnBobberEnterWater(); // Tell the controller
            if (fishingRodController.bobberSplashEffect != null)
            {
                Instantiate(fishingRodController.bobberSplashEffect, transform.position, Quaternion.identity);
            }
        }
        
        // Apply physics based on state
        if (isInWater)
        {
            // Apply a force stronger than gravity to float
            Vector3 buoyancyForce = Physics.gravity * -1 * buoyancyMultiplier;
            rb.AddForce(buoyancyForce, ForceMode.Acceleration);

            // Apply water drag
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;
        }
        else
        {
            // In air, Unity's gravity works, and we use low drag
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.1f;
        }
    }

    public Rigidbody GetRigidbody()
    {
        return rb;
    }
}