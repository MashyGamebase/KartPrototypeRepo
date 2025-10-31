using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IKartInputInterface
{
    float GetVerticalInput();
    float GetHorizontalInput();
    bool GetDriftInput();
    bool GetUseItemInput();
}