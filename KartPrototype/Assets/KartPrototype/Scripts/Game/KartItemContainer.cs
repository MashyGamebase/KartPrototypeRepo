using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class KartItemContainer : MonoBehaviour
{
    public List<Powerup> powerups = new List<Powerup>();
    public Powerup HeldPowerup;
    public Transform dropTransform;

    [Header("UI")]
    public Image heldItemImagePC;
    public Image heldItemImageMobile;

    private bool hasUsedPowerup = false;

    public void GetRandomPowerUp()
    {
        if (powerups.Count == 0)
            return;

        if (HeldPowerup != null)
            return;

        HeldPowerup = powerups[UnityEngine.Random.Range(0, powerups.Count)];

        heldItemImagePC.color = new Color(1f, 1f, 1f, 1f);
        heldItemImageMobile.color = new Color(1f, 1f, 1f, 1f);

        heldItemImageMobile.sprite = HeldPowerup.icon;
        heldItemImagePC.sprite = HeldPowerup.icon;
        hasUsedPowerup = false;

        Debug.Log($"[Powerup Assigned] {HeldPowerup.name}");
    }

    /// <summary>
    /// Uses the held powerup, and if it's a Boost type, runs the provided action.
    /// </summary>
    /// <param name="onBoostAction">Action to invoke only if the held powerup is a Boost.</param>
    public void UsePowerup(Action onBoostAction = null)
    {
        if (HeldPowerup == null || hasUsedPowerup)
            return;

        switch (HeldPowerup.powerupType)
        {
            case PowerupType.Banana:
                if (HeldPowerup.powerupActivation == PowerupActivation.Drop && HeldPowerup.dropPrefab)
                {
                    Instantiate(HeldPowerup.dropPrefab, dropTransform.position, Quaternion.identity);
                }
                break;

            case PowerupType.Boost:
                onBoostAction?.Invoke();
                break;

            default:
                break;
        }

        Debug.Log($"[Powerup Used] {HeldPowerup.name}");

        heldItemImagePC.color = new Color(1f, 1f, 1f, 0f);
        heldItemImageMobile.color = new Color(1f, 1f, 1f, 0f);

        HeldPowerup = null;
        hasUsedPowerup = true;
    }
}
