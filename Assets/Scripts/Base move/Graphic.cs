using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graphic : MonoBehaviour
{
    public float cycleSmooth;
    public float turnSpeed;
    public float lockedSpeed;
    private float lastCamX;
    [HideInInspector] public float angle;
    private Vector3 lastDir;
    public CamControl camControl;
    Animator anim;
    Body body;

    private void Start() {
        anim = GetComponent<Animator>();
        body = GetComponentInParent<Body>();
        Body.landDelegate += OnLand;
    }

    private void Update() {
        bool rLock = ( lastDir != Vector3.zero && body.velocity.y > 0 );
        Vector3 dir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        float input = Mathf.Ceil(Mathf.Clamp01(dir.magnitude));
        if(body.fallBack)
            input = 0;
       
        if(input > 0) {
            angle = (rLock ? lastCamX : camControl.x) + Vector3.SignedAngle(Vector3.forward, (rLock ? lastDir : dir), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, angle, 0), ((rLock) ? lockedSpeed:turnSpeed) * Time.deltaTime);
        }

        anim.SetFloat("Speed",input, cycleSmooth, Time.deltaTime);

        // if(!body.isGrounded(1))
        //     anim.SetBool("midAir", true);
        // else if(body.isGrounded())
        //     anim.SetBool("midAir", false);
        anim.SetBool("close", body.isGrounded(1));
        //TODO: Related to gndStep
        anim.SetBool("onGround", body.isGrounded(0.2f));
    }

    public void Jump(){
        anim.SetTrigger("Jump");
        anim.SetBool("close", true);
        // anim.SetBool("midAir", true);

        lastDir = Vector3.Normalize(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")));
        lastCamX = camControl.x;
    }

    public void OnLand() {
        lastDir = Vector3.zero;
    }
}
