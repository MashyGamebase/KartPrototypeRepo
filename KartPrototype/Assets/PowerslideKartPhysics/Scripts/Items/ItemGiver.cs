// Copyright (c) 2023 Justin Couch / JustInvoke
using UnityEngine;

namespace PowerslideKartPhysics
{
    [DisallowMultipleComponent]
    // Class for objects that give items to karts when touched
    public class ItemGiver : MonoBehaviour
    {
        ItemManager manager;
        Collider trig;
        public Renderer rend1;
        public Renderer rend2;
        public string itemName;
        public int ammo = 1;
        public float cooldown = 1.0f;
        float offTime = 0.0f;

        private void Awake() {
            manager = FindObjectOfType<ItemManager>();
            trig = GetComponent<Collider>();
            //rend1 = GetComponent<Renderer>();
            offTime = cooldown;
        }

        private void Update() {
            if (trig == null || rend1 == null) { return; }

            offTime += Time.deltaTime;

            // Disable trigger and renderer during cooldown
            trig.enabled = rend1.enabled = offTime >= cooldown;
            trig.enabled = rend2.enabled = offTime >= cooldown;
        }

        private void OnTriggerEnter(Collider other) {
            if (manager != null) {
                // Give item to caster
                ItemCaster caster = other.transform.GetTopmostParentComponent<ItemCaster>();
                if (caster != null) {
                    offTime = 0.0f;

                    // Give specific item if named, otherwise random item
                    caster.GiveItem(
                        string.IsNullOrEmpty(itemName) ? manager.GetRandomItem() : manager.GetItem(itemName),
                        ammo, false);
                }
            }
        }
    }
}