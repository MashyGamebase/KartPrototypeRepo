using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Powerup", menuName = "Kart/Powerups", order = 1)]
public class Powerup : ScriptableObject
{
    public PowerupType powerupType = PowerupType.None;
    public PowerupActivation powerupActivation = PowerupActivation.Self;
    public Sprite icon;

    [Tooltip("Only used if activation is Drop.")]
    public GameObject dropPrefab;

    [NonSerialized]
    public Action onBoostActivated; // Assign this in code
}

public enum PowerupType
{
    None,
    Banana,
    Boost
}

public enum PowerupActivation
{
    Drop,
    Self
}
