using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    public float walkSpeed;
    public float airSpeed;

    [Space]
    [SerializeField] private bool ready;
    public float jumpDelay;
    public Vector2 jumpHeight;

    private float speed;
    private Vector3 dir;
    Body body;
    CamControl cam;
    Graphic graphic;
    

    private void Start() {
        ready = true;
        body = GetComponent<Body>();
        cam = GetComponentInChildren<CamControl>();
        graphic = GetComponentInChildren<Graphic>();

        Body.landDelegate += OnLand;
    }

    public void OnLand() {
        // Debug.Log("Landed!");
        //TODO stop instance and start new one!
        this.Invoke( () => ready = true, jumpDelay);
    }

    private void Update() {
        speed = walkSpeed;
        dir = Vector3.Normalize( Quaternion.Euler(0, cam.x, 0) * 
            new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) );

        //TODO: Move this to body (midAir variable)
        if(!body.isGrounded(1)) {
            ready = false;
        }

        if(Input.GetButtonDown("Jump") && (dir.magnitude > 0 || body.fallBack) && ready) {
            if(body.Jump(dir, jumpHeight)) {
                graphic.Jump();
                ready = false;
            }
        }
    }

    //Move
    private void FixedUpdate() {
        if(body.isGrounded(1))
            body.MoveTo(transform.position + body.Plane(dir)*speed);
        else if(body.velocity.y < 0)
            body.MoveTo(transform.position + dir * speed * airSpeed);
    }
}
