using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : Body
{
    public LayerMask stepMask;
    public float drag;
    public float pushPower = 2.0F;

    public override Vector3 velocity { get { return momentum; } protected set {} }
    private Vector3 to;
    CharacterController charCtrl;
    private Vector3 momentum;
    private float defaultHeight, defaultRadius;
    private Vector3 defaultCenter;

    [Header("Slope")]
    public float stepOffset;
    public float deepness;
    // public bool stopDir;
    public Vector2 stepVelocity;
    public Vector2 dirVelocity;
   
    [Space]
    public float maxSpeed;
    public float slock;
    public Vector2 effector;
    [Range(45, 90)] public float angle;
    [Range(0, 180)] public float downRange;
    [Range(0, 180)] public float upRange;
    
    [Space]
    [Range(0, 90)] public float slopeLimit;
    [Range(45, 90)] public float slideAngle;
    public float delay, force;
    
    // public new bool fallBack;
    private bool isCo;
    private float input;

    [Header("Vertical")]
    public float gravity = 0.196F;
    public float gndOffset;
    public float gndStep;
    public float gndRadius;

    private bool midAir, willAir;
    private bool step, slide;

    [Header("Debug")]
    public bool onGround;
    public bool close;
    public bool stepDebug;
    [HideInInspector]
    public Vector3 veloDisplay;
    CamControl cam;
    Graphic graphic;

    #region Core
    private void Start() {
        momentum = Vector3.zero;
        stepOffset = Mathf.Min(stepOffset, 0.18f*4);
        
        cam = GetComponentInChildren<CamControl>();
        graphic = GetComponentInChildren<Graphic>();
        charCtrl = GetComponent<CharacterController>();

        //Get the default shape of the character controller collider
        defaultCenter = charCtrl.center;
        defaultRadius = charCtrl.radius;
        defaultHeight = charCtrl.height;
    }

    
    //Emulate velocity, gravity and collision
    //And implement onLand event
    private void FixedUpdate() {
        //Update onGround status for debug purpose
        onGround = isGrounded();
        close = isGrounded(1);

        /* if(reference) {
            if(charCtrl) {
                character.center = defaultCenter + reference.rotation * colOffset;
            } else {
                cap.center = defaultCenter + reference.rotation * colOffset;
            }
        } */

        //Land event
        //TODO: Remove garbage variables
        if( (isGrounded() || step) && midAir) {
            if(!fallBack && !slide) {
                //TODO: Move this shit out of this place!
                Vector3 dir = Quaternion.Euler(0, graphic.angle, 0) * Vector3.forward;
                momentum = dir*0.2f;
                // momentum.y = 0;
                // momentum = momentum.normalized * 0.5f;
                // momentum = Vector3.zero;
            }
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
        // if(step && stopDir)
        //     to = Vector3.zero;
        step = slide = false;

        //////////Collision
        veloDisplay = momentum;
        if(charCtrl.enabled)
            charCtrl.Move((to + velocity) * Time.fixedDeltaTime);

        to = Vector3.zero;

        //Different method to apply drag on velocity
        momentum.x *= Mathf.Exp(-drag* Time.fixedDeltaTime);
        momentum.z *= Mathf.Exp(-drag* Time.fixedDeltaTime);
        // _velocity.x = Mathf.Lerp(_velocity.x, 0, 1 - Mathf.Exp(2* -Time.fixedDeltaTime));
        // _velocity.z = Mathf.Lerp(_velocity.z, 0, 1 - Mathf.Exp(2* -Time.fixedDeltaTime));
        
        /////////////////Gravity
        if(isGrounded() && velocity.y < 0)
            momentum.y *= Mathf.Exp(-drag* Time.fixedDeltaTime);
        else if(velocity.y > -gravity*20)
            momentum += Vector3.down * gravity;
    }

    //Emulate interaction with rigidbody
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic)
            return;
        if (hit.moveDirection.y < -0.3f)
            return;

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        body.velocity = pushDir * pushPower;
    }
    
    //Jump implementation
    public override bool Jump(Vector3 dir, Vector2 force) {
        if(isGrounded(gndStep)) {
            // if(!Physics.Raycast(transform.position, dir, 1f))
            //TODO: fallBack and ready should be related to an unique suspend variable
            if(fallBack) {
                if(isCo)
                    return false;
                dir = gndNormal();
                dir.y = 0;
                fallBack = false;
            }

            if(!checkPosition(transform.position + dir * 0.2f + Vector3.up)) {
                momentum = (Vector3.up + dir * force.x) * force.y;
                willAir = true;
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Slope
    public override Vector3 Plane(Vector3 dir) {
        // Debug.DrawRay(transform.position, Snap(dir), Color.red);
        Step(dir);
        Slide(dir);
        return Snap(dir) * Speed(dir);
        // return dir;
    }

    //Handle step system
    [Space]
    [SerializeField] private float height;
    [SerializeField] private float deep;
    public GameObject sphere;
    private void Step(Vector3 dir) {
        RaycastHit hit;
        Vector3 start = transform.position + charCtrl.center + Vector3.down * charCtrl.height/2;
        if(!fallBack && (momentum.y <= stepVelocity.x && isGrounded(1)) && dir.magnitude > 0) {
            if(isStair(dir) && deep>=0.1f) {
                if(momentum.y >= 0 || isGrounded(0.1f)) {
                    float rise = stepVelocity.x;
                    //TODO: Too abrupt! Make an equation to fit these values
                    if(height < 0.2f)
                        rise *= 0.75f;
                    else if(deep < 0.4f)
                        rise *= 1.5f;
                    
                    // Debug.Log(rise);
                    momentum = dir *dirVelocity.x+ Vector3.up * rise;
                    step = true;
                    if(stepDebug)
                    Debug.Log("Step on me!");
                } else {
                if(stepDebug)
                Debug.Log("Air step");
                }
            } else {
                // if(Physics.Raycast(start, -dir, out hit, 0.4f, layerMask))
                if(stepBox(transform.position -dir*0.3f, dir) && !isGrounded()) {
                    if(Physics.Raycast(start - Vector3.down *0.02f, Vector3.down, out hit, 
                                        (stepOffset + 0.02f), layerMask)) {
                            // _velocity.y = -step;
                            momentum = dir * dirVelocity.y + stepVelocity.y * Vector3.down;
                            step = true;
                            if(stepDebug)
                            Debug.Log("Step down");
                    }
                }
            }
        }
    }

    public bool stepBox(Vector3 position, Vector3 dir) {
        return  Physics.CheckBox(position + charCtrl.center, new Vector3(charCtrl.radius, charCtrl.height/2,
            charCtrl.radius), Quaternion.Euler(0, Vector3.SignedAngle(dir, Vector3.forward, Vector3.up), 0), layerMask);
    }

    //Check stair authencity
    private bool isStair(Vector3 dir) {
        //TODO: Clean this and no predefined values!
        RaycastHit hitInfo, hit;
        //&& isGrounded()
        // if(!stepBox(transform.position + dir*0.2f + Vector3.up * stepOffset, dir)) {
        bool check = checkPosition(transform.position + dir*deepness + Vector3.up * 0.01f) && 
            // !checkPosition(transform.position + dir*deepness + Vector3.up * stepOffset);
            !stepBox(transform.position + dir*deepness + Vector3.up * stepOffset, dir);
        if(!check && deepness > 0.1f) {
            check = checkPosition(transform.position + dir*0.1f + Vector3.up * 0.01f) && 
            // !checkPosition(transform.position + dir*0.1f + Vector3.up * stepOffset);
            !stepBox(transform.position + dir*0.1f + Vector3.up * stepOffset, dir);
        }
        if(check) {
        Vector3 bottom = transform.position + charCtrl.center + Vector3.down * charCtrl.height/2;
        float angle = Vector3.Angle(Vector3.up, gndNormal());
        float radius = Mathf.Clamp(stepOffset/2, 0, 0.18F);
        
        //TODO: case where stepOffset exceed two radius!
        bool cast = Physics.SphereCast(bottom + Vector3.up *radius, radius, dir, out hitInfo,
                deepness + charCtrl.radius, layerMask); 
        if(radius < stepOffset/2) {
            if(Physics.SphereCast(bottom + Vector3.up * (stepOffset - radius + 0.01f), radius, dir, out hit,
                deepness + charCtrl.radius, layerMask)) {
                if(!(cast && hit.distance >= hitInfo.distance)) {
                    hitInfo = hit;
                }
                cast = true;
            }
        }

        if(cast) {
            if(bottom.y + charCtrl.stepOffset > hitInfo.point.y && isGrounded(0.2f)) {
                if(stepDebug)
                Debug.Log("Too close");
                return false;
            }
            Vector3 start = hitInfo.point - dir*hitInfo.distance;
            
            // start.y = hitInfo.point.y - 0.02f;
            // start.y -= 0.02f;
            // if(stepDebug)
            // Debug.DrawLine(hitInfo.point, hitInfo.point + hitInfo.normal, Color.blue);
            // float d = hitInfo.distance+0.2f;
            // Debug.DrawLine(start, start+dir, Color.blue);
            //TODO: WTF is this?
            bool ray = Physics.Raycast(start, dir, out hitInfo, 1, layerMask);
            if(!ray) {
                start.y -= 0.01f;
                ray = Physics.Raycast(start, dir, out hitInfo, 1, layerMask);
                if(!ray) {
                    start.y += 0.02f;
                    ray = Physics.Raycast(start, dir, out hitInfo, 1, layerMask);
                }
            }

            if(ray) {
                //TODO: This method is not accurate
                Vector3 yless = -hitInfo.normal; yless.y = 0;
                yless = dir;
                if(Physics.SphereCast(hitInfo.point + Vector3.up *(stepOffset/2+0.12f)-
                    yless*(stepOffset/2), stepOffset/2, yless, out hit, 2, layerMask)) {
                // if(Physics.BoxCast(hitInfo.point + Vector3.up *(stepOffset/2+0.11f)-yless*(stepOffset/2),
                // new Vector3(0.05f,stepOffset/2,0), yless, out hit,
                // Quaternion.Euler(0, Vector3.SignedAngle(yless, Vector3.forward, Vector3.up), 0), 2, layerMask)) {
                        yless = hitInfo.point; yless.y = hit.point.y; 
                        deep = Vector3.Distance(hit.point, yless);
                        height = hit.point.y - hitInfo.point.y;
                        // Debug.DrawLine(hit.point, yless);
                        deep = Mathf.Round(deep*100)/100;
                        height = Mathf.Round(height*100)/100;
                        // if(deep >= 0.1f)
                        //     Debug.Break();
                        // Debug.Log(deep +":"+hit.point + "/" + hitInfo.point);
                } else {
                    deep = 1;
                    height = 1;
                    if(stepDebug)
                    Debug.Log("No next step");
                    // sphere.transform.position = hitInfo.point + Vector3.up *(stepOffset/2+0.1f)-dir*0.1f;
                    // sphere.transform.rotation = Quaternion.Euler(0, Vector3.SignedAngle(Vector3.forward, dir, Vector3.up), 0);
                    // Debug.Break();
                }
                if(stepDebug) {
                    Debug.DrawLine(hitInfo.point, hitInfo.point + hitInfo.normal, Color.magenta);
                }

                float angle1 = Vector3.Angle(Vector3.up, hitInfo.normal);
                float diff = angle - angle1;
                if(diff < -30) {
                    // if(stepDebug)
                    // Debug.Log("Angle: "+diff);
                    return true;
                } else if(stepDebug) {
                    Debug.Log("Fake step!");
                    // Debug.Log("Difference: "+diff);
                }
            } else if(stepDebug || true) {
                Debug.Log("Raycast error!");
                // Debug.Break();
                // Debug.Log(start + " + " + dir + " * " + d);
                return true;
            }
            if(!Physics.SphereCast(start + Vector3.down * 0.12f, 0.1f, dir, out hitInfo, 2, layerMask)
                && start.y - 0.12f > bottom.y) {//TODO: Not yet sure: begin from char center, not from -dir
                if(stepDebug)
                Debug.Log("Faceless step");
                return true;
            }
        } else if(stepDebug || true) {
            Debug.Log("SphereCast error!");
            sphere.transform.position = (bottom + Vector3.up * (stepOffset - radius));
            sphere.transform.rotation = Quaternion.Euler(0, Vector3.SignedAngle(Vector3.forward, dir, Vector3.up), 0);
            Debug.Break();
        }
        }

    return false;
    }

    //Make sure player snap on ground
    // public bool snap;
    private Vector3 Snap(Vector3 dir) {
        if(momentum.y <= 0.2f) {
            Vector3 normal = gndNormal();
            if(dir.magnitude > 0) {
                float downAngle = 45 + downRange/4;
                if(Vector3.Angle(normal, dir) <= downAngle) 
                    return Vector3.Normalize(dir + Vector3.down * 
                    (Mathf.Tan( ( Vector3.Angle(Vector3.up, normal)) * Mathf.Deg2Rad) + slock));
            }
        }
        /* RaycastHit hit;
        if(momentum.y <= 0.2f)
        if(Physics.Raycast(transform.position + charCtrl.center + dir*(0.01f), Vector3.down, out hit, 2, layerMask)) {
            Vector3 upper = hit.point;
            Vector3 normal = hit.normal;
            if(Physics.Raycast(transform.position + charCtrl.center, Vector3.down, out hit, 2, layerMask)) {
            float down = (upper - hit.point).normalized.y;
            if(down < 0)
                return Vector3.Normalize(dir + Vector3.up * (down - 0.1f));
            }
        } */
        return dir;
    }

    //Handle sliding on steep slope
    private void Slide(Vector3 dir) {
        if(momentum.y <= 0 && (isGrounded() || fallBack)) {
            Vector3 normal = gndNormal();
            if(fallBack && Vector3.Angle(Vector3.up, normal) < slopeLimit && !isCo) {
                this.Invoke(() => { fallBack = false; isCo = false; }, delay);
                isCo = true;
                //TODO: Stop if fallback again and modify Utility to keep the state of each coroutine!
            }

            //TODO: Clean this shit! (SphereCast)
            if(Vector3.Angle(Vector3.up, normal) > slopeLimit) { // && isGrounded()
                Vector3 to = -normal;
                to.y = 0;

                RaycastHit hit;
                if(Physics.Raycast(transform.position, to, out hit, 1, layerMask)) {
                    if(Vector3.Angle(Vector3.up, hit.normal) > slopeLimit) {
                        if(Physics.Raycast((transform.position + hit.point)/2, Vector3.down, out hit, 1, layerMask)) {
                            if(Vector3.Angle(Vector3.up, hit.normal) > slopeLimit) {
                                fallBack = true;
                                input = 0;
                            }
                        }
                    }
                }
            }
            
            if(Vector3.Angle(Vector3.up, normal) > slideAngle
                && (dir.magnitude == 0 || fallBack)) {
                float a = 1 / Mathf.Cos( (Vector3.Angle(Vector3.up, normal) + 5f) * Mathf.Deg2Rad);
                if(a < 0) a = 0;
                if(fallBack) {
                    //TODO: Move this shit out of this place!
                    input = Mathf.Clamp(input + Input.GetAxis("Horizontal"), -1, 1);
                    Vector3 temp = Quaternion.Euler(0, cam.x, 0) * Vector3.right * input;
                    float sign = Mathf.Sign(Vector3.SignedAngle(temp, normal, Vector3.up));
                    float c = Mathf.Abs(Mathf.Ceil(input));

                    momentum = Quaternion.Euler(0, -sign*45*c, 0)
                            * (normal + a * Vector3.down) * force;
                } else {
                    momentum = (normal + a * Vector3.down);
                    slide = true;
                }
            }
        } 
    }

    //Emulate speed level on slope
    //Can also be used to implement different ground friction
    private float Speed(Vector3 dir) {
        Vector3 normal = gndNormal();
        float downAngle = 45 + downRange/4;
        float x = Vector3.Angle(Vector3.up, normal);
        
        float speed = 1;
        float upAngle = 135 - upRange/4;

        if(momentum.y <= 0 && x >= angle) {
            if (Vector3.Angle(normal, dir) <= downAngle)
                speed = Mathf.Clamp(Mathf.Tan(x * Mathf.Deg2Rad)*effector.x, 1, maxSpeed);
            else if(Vector3.Angle(normal, dir) >= upAngle) {
                float b = x*Mathf.Deg2Rad;
                speed = Mathf.Clamp01(effector.y / Mathf.Tan(b));

                // float pi = Mathf.PI;
                // float a = slopeLimit*Mathf.Deg2Rad;
                // float b = (pi*x*Mathf.Deg2Rad)/(4*a-pi)+(4*pi*a-2*Mathf.Pow(pi, 2))/(16*a-4*pi);
            }
        }

        if(fallBack) speed = 0;
        return speed;
    }
    #endregion

    #region Utility
    public override void MoveTo(Vector3 position) {
            to = position - transform.position;
    }

    //Check if player is on ground
    public override bool isGrounded(float offset = 0) {
        Ray cast = new Ray(transform.position + charCtrl.center, Vector3.down);
        return Physics.SphereCast(cast, gndRadius, (gndOffset + offset), layerMask);
        // return (Physics.CheckCapsule(transform.position + charCtrl.center, transform.position 
            // + charCtrl.center + Vector3.down * (gndOffset + offset), gndRadius, layerMask));
    }

    //Return the ground normal
    private Vector3 gndNormal() {
        RaycastHit hit;
        //TODO: Predefined value are not the best
        if(Physics.Raycast(transform.position + charCtrl.center, Vector3.down, out hit, 2f, layerMask)) {
            // Debug.DrawLine(transform.position + charCtrl.center, hit.point, Color.red);
            if(stepDebug)
            Debug.DrawLine(hit.point, hit.point + hit.normal, Color.red);
            return hit.normal;
        } else
            return Vector3.zero;
    }

    //Check if the player is not inside any collider at position
    public bool checkPosition(Vector3 position) {
        // return  Physics.CheckCapsule(transform.position + capCenter + Vector3.up * (capHeight/2-capRadius), 
        // capCenter + transform.position, capRadius, layerMask);
        return  Physics.CheckCapsule(position + charCtrl.center + Vector3.up * (charCtrl.height/2-charCtrl.radius), 
            charCtrl.center + position + Vector3.down * (charCtrl.height/2-charCtrl.radius), charCtrl.radius, layerMask);
    }

    //REshape the character controller collider
    public void shapeCollider(float height, float radius) {
        // colOffset = offset;
        // if(refer != null)
        //     reference = refer;
        to = Vector3.zero;
        charCtrl.height = (height != 0) ? height : defaultHeight;
        charCtrl.radius = (radius != 0) ? radius : defaultRadius;
        if(height != 0)
            charCtrl.center = defaultCenter + Vector3.down * ((defaultHeight - height) / 2) ;
    }

    //Reset the shape of the character controller collider
    public void resetCollider() {
        // reference = transform;
        // colOffset = Vector3.zero;
        charCtrl.height = defaultHeight;
        charCtrl.radius = defaultRadius;
        charCtrl.center = defaultCenter;
    }
    #endregion
}
