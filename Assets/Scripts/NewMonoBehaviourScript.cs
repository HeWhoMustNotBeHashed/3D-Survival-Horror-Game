using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// SIMPLE PICK UP
// Look at an object and press E to hold it on the left side of your view. Press E again to drop it.
//
// Put this script on the Main Camera.
// Pickable objects need: the tag set below ("canPickUp"), a Rigidbody and a Collider.
//
// This script does NOT parent anything, change layers, or touch your torch or camera setup.
// It works with the new Input System, the old one, or "Both" (Project Settings > Player > Active Input Handling).
[DefaultExecutionOrder(100)] // run after the camera has moved this frame, so the held object doesn't jitter
public class SimplePickUp : MonoBehaviour
{
    [Header("Picking up")]
    public string pickUpTag = "canPickUp";
    [Tooltip("How far you can reach, in metres.")]
    public float pickUpRange = 7f;

    [Header("Where the object floats (relative to the camera)")]
    [Tooltip("X: negative = left.  Y: negative = down.  Z: distance in front of the camera.")]
    public Vector3 holdOffset = new Vector3(-0.7f, -0.4f, 1.0f);
    [Tooltip("Only if the held object looks turned the wrong way (degrees). Example: 0, 90, 0")]
    public Vector3 rotationOffset = Vector3.zero;
    [Tooltip("How far to keep the held object away from walls, in metres.")]
    public float wallPadding = 0.3f;

    private Rigidbody heldRb;
    private Collider[] heldColliders;
    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    void Update()
    {
        if (!InteractPressed()) return;

        if (heldRb == null) TryPickUp();
        else Drop();
    }

    void LateUpdate()
    {
        if (heldRb != null) MoveHeldObject();
    }

    void OnDisable()
    {
        if (heldRb != null) Drop(); // don't leave an object frozen in the air
    }

    // ---------------------------------------------------------------- input

    bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    // ---------------------------------------------------------------- pick up / drop

    void TryPickUp()
    {
        if (!Cast(transform.position, transform.forward, pickUpRange, out RaycastHit hit)) return;
        if (!hit.transform.CompareTag(pickUpTag)) return;

        if (!hit.transform.TryGetComponent(out heldRb))
        {
            Debug.LogWarning($"SimplePickUp: '{hit.transform.name}' needs a Rigidbody to be picked up.");
            return;
        }

        heldRb.isKinematic = true; // no physics while we hold it

        // switch the colliders off while held, so the object can't shove the player around
        heldColliders = heldRb.GetComponentsInChildren<Collider>();
        SetColliders(false);
    }

    void Drop()
    {
        SetColliders(true);
        heldRb.isKinematic = false; // gravity takes over, it falls to the floor
        heldRb = null;
        heldColliders = null;
    }

    void SetColliders(bool on)
    {
        if (heldColliders == null) return;
        foreach (Collider c in heldColliders)
            if (c != null) c.enabled = on;
    }

    // ---------------------------------------------------------------- holding

    void MoveHeldObject()
    {
        Vector3 start = transform.position;

        // Work out the hold point from the camera's directions. This ignores the camera's scale on purpose,
        // so a squashed camera/player can't push the object to the wrong place.
        Vector3 target = start
                       + transform.right * holdOffset.x
                       + transform.up * holdOffset.y
                       + transform.forward * holdOffset.z;

        // If a wall is between the camera and the hold point, pull the object back so it doesn't go through it.
        Vector3 toTarget = target - start;
        float distance = toTarget.magnitude;
        if (distance > 0.001f)
        {
            Vector3 dir = toTarget / distance;
            if (Cast(start, dir, distance + wallPadding, out RaycastHit wall))
            {
                float safe = Mathf.Max(0.1f, wall.distance - wallPadding);
                target = start + dir * Mathf.Min(distance, safe);
            }
        }

        // Stay upright and turn only left/right with the camera.
        Quaternion rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Quaternion.Euler(rotationOffset);
        heldRb.transform.SetPositionAndRotation(target, rotation);
    }

    // ---------------------------------------------------------------- helper

    // A raycast that returns the nearest hit, ignoring the player's own colliders
    // (body, torch, etc.), the held object and trigger zones.
    bool Cast(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearest)
    {
        nearest = default;
        bool found = false;
        float best = float.MaxValue;

        int count = Physics.RaycastNonAlloc(origin, direction, hitBuffer, distance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hitBuffer[i];
            if (h.transform.root == transform.root) continue;      // our own body / torch
            if (heldRb != null && h.rigidbody == heldRb) continue; // the held object
            if (h.distance < best)
            {
                best = h.distance;
                nearest = h;
                found = true;
            }
        }
        return found;
    }
}