using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerslideKartPhysics;

public class DeadzoneTrigger : MonoBehaviour
{
    [Tooltip("Where to teleport the kart back to")]
    public Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Kart obj))
        {
            obj.gameObject.transform.position = respawnPoint.position;
            obj.gameObject.transform.rotation = respawnPoint.rotation;

            if (obj.TryGetComponent(out Rigidbody rb))
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
