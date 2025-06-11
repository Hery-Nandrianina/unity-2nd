using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RbCharacter : Body
{
    [Header("Vertical")]
    public float gndOffset;
    public float gndRadius;
    public override Vector3 velocity { get { return rb.velocity; } protected set { rb.velocity = value; } }
    private bool willAir, midAir;
    CapsuleCollider col;
    Rigidbody rb;

    private void Start() {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        rb.freezeRotation = true;
    }
    
    private void FixedUpdate() {
        if(isGrounded() && midAir) {
            landDelegate();
            midAir = false;
            willAir = false;
        }

        if(!midAir) {
            midAir = !isGrounded(1);
            if(willAir && velocity.y < 0) {
                willAir = false;
                midAir = true;
            }
        }
    }

    public override bool Jump(Vector3 dir, Vector2 force) {
        rb.AddForce((Vector3.up + dir * force.x) * force.y, ForceMode.VelocityChange);
        willAir = true;
        return true;
    }

    public override void MoveTo(Vector3 to) {
        Vector3 targetVelocity = to - transform.position;
        Vector3 velocityChange = (targetVelocity - rb.velocity);
        velocityChange.y = 0;
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    public override Vector3 Plane(Vector3 dir)  {
        return dir;
    }

    public override bool isGrounded(float offset = 0) {
        Ray cast = new Ray(transform.position + col.center, Vector3.down);
        return Physics.SphereCast(cast, gndRadius, (gndOffset + offset), layerMask);
        // return (Physics.CheckCapsule(transform.position + col.center, transform.position 
            // + col.center + Vector3.down * (gndOffset + offset), gndRadius, layerMask));
    }
}
