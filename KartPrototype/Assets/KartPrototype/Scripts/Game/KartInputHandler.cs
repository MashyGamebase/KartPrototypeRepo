using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KartInputHandler : MonoBehaviour
{
    private IKartInputInterface inputProvider;

    void Awake()
    {
        inputProvider = GetComponent<IKartInputInterface>();
    }

    public KartInput GetKartInput()
    {
        if (inputProvider == null) return default;

        return new KartInput
        {
            Horizontal = inputProvider.GetHorizontalInput(),
            Vertical = inputProvider.GetVerticalInput(),
            Drift = inputProvider.GetDriftInput(),
            UseItem = inputProvider.GetUseItemInput()
        };
    }
}