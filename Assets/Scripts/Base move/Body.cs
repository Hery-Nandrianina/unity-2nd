using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Body : MonoBehaviour
{
    public LayerMask layerMask;
    public delegate void OnLandDelegate();
    public static OnLandDelegate landDelegate;
    public virtual Vector3 velocity { get; protected set; }
    //TODO: fallback should be proper to charCtrl
    [HideInInspector] public bool fallBack;

    public abstract Vector3 Plane(Vector3 dir);
    public abstract void MoveTo(Vector3 to);
    public abstract bool Jump(Vector3 dir, Vector2 force);
    public abstract bool isGrounded(float offset = 0);
}

public static class Utility
{
    //Keep track of invoke instance to stop and check running status
    public static void Invoke(this MonoBehaviour mb, System.Action f, float delay) {
        mb.StartCoroutine(InvokeRoutine(f, delay));
    }
 
    private static IEnumerator InvokeRoutine(System.Action f, float delay)
    {
        yield return new WaitForSeconds(delay);
        f();
    }
}