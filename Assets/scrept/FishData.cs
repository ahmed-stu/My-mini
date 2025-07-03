// ## NEW SCRIPT ##
// FishData.cs

using UnityEngine;

// This line allows you to create instances of this object from the Assets menu
[CreateAssetMenu(fileName = "New Fish Data", menuName = "Fishing/Fish Data")]
public class FishData : ScriptableObject
{
    [Header("Fish Identity")]
    public string fishName = "Common Fish";
    public enum Rarity { Common, Rare, Epic, Legendary }
    public Rarity fishRarity = Rarity.Common;

    [Header("Physical Characteristics")]
    [Tooltip("How big the fish model will be.")]
    [Range(0.5f, 3f)] public float modelScale = 1.0f;

    [Header("Behavior & Difficulty")]
    [Tooltip("How much health the fish has during the fight.")]
    [Range(50f, 500f)] public float health = 100f;

    [Tooltip("How hard the fish pulls on the line.")]
    [Range(10f, 50f)] public float pullForce = 15f;
    
    [Tooltip("How fast the fish moves when wandering or following bait.")]
    [Range(2f, 8f)] public float moveSpeed = 4f;

    [Header("Rewards")]
    [Tooltip("How many points the player gets for catching this fish.")]
    public int pointsValue = 10;
}