using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputProvider : MonoBehaviour, IKartInputInterface
{
    [Header("Mobile Input Overrides")]
    public float mobileHorizontal = 0f;
    public float mobileVertical = 0f;
    public bool mobileDrift = false;
    public bool mobileUseItem = false;

    private Vector2 throttleInput = Vector2.zero;
    private bool driftPressed = false;
    private bool useItemPressed = false;

    [SerializeField] private bool isMobile = false;

    public static PlayerInputProvider LocalInstance;

    private void Awake()
    {
        // Optional: if you want to assign automatically
        if (LocalInstance == null)
            LocalInstance = this;
    }

    // PC InputSystem Callbacks
    public void OnThrottleControl(InputAction.CallbackContext context)
    {
        if (!isMobile)
        {
            throttleInput = context.ReadValue<Vector2>();
        }
    }

    public void OnDriftButton(InputAction.CallbackContext context)
    {
        if (!isMobile)
            driftPressed = context.ReadValueAsButton();
    }

    public void OnUseItemButton(InputAction.CallbackContext context)
    {
        if (!isMobile && context.started)
        {
            useItemPressed = true;
        }
    }

    // Mobile input setters (called from UI)
    public void SetMobileHorizontal(float value) => mobileHorizontal = value;
    public void SetMobileVertical(float value) => mobileVertical = value;
    public void SetMobileDrift(bool value) => mobileDrift = value;
    public void SetMobileUseItem(bool value) => mobileUseItem = value;

    // IKartInputInterface
    public float GetVerticalInput() => isMobile ? mobileVertical : throttleInput.y;
    public float GetHorizontalInput() => isMobile ? mobileHorizontal : throttleInput.x;
    public bool GetDriftInput() => isMobile ? mobileDrift : driftPressed;
    public bool GetUseItemInput()
    {
        if (isMobile)
        {
            bool result = mobileUseItem;
            mobileUseItem = false; // Reset after reading
            return result;
        }
        else
        {
            bool result = useItemPressed;
            useItemPressed = false;
            return result;
        }
    }
}
