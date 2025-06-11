using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamControl : MonoBehaviour
{
    public Vector2 sensitivity;
    public Vector2 yBorder;
    public float distance;
    public float smooth;
    //public float smooth * Time.deltaTime;
	private Transform target;
    [Header("Collision")]
    public float castRadius;
    public float offset;
    [Space]
    public Vector2 close;
    public float radius;
    public LayerMask layerMask;
    [Header("Debug")]
    public bool collision;
    public bool drawGizmo;
    public bool inside;
    // public bool project;
    private int cullingMask, pass, player;
    private Vector3 position;
    private Vector3 up;

    private bool wasRay;
    private float dist;
    // private float hitDistance;
    Camera cam;

    [HideInInspector] public float x, y;

    private void Start() {
        cam = GetComponent<Camera>();
        cullingMask = cam.cullingMask;
        target = transform.parent;
        dist = distance;

        pass = (int) Mathf.Pow(2, LayerMask.NameToLayer("Passthrough"));
        player = (int) Mathf.Pow(2, LayerMask.NameToLayer("Player"));
    }

    private void OnDrawGizmos() {
        if(drawGizmo) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position - up, radius);
            Gizmos.color = Color.white;
        }
    }

    private float Clamp0360(float eulerAngles) {
        float result = eulerAngles - Mathf.CeilToInt(eulerAngles / 360f) * 360f;
        if (result < 0) result += 360f;
        return result;
    }

    private void Update() {
        // x += Input.GetAxis("Mouse X")  * sensitivity.x + Input.GetAxis("Horizontal");
        x += Input.GetAxis("Mouse X")  * sensitivity.x;
		y += Input.GetAxis("Mouse Y") * sensitivity.y;
		x = Clamp0360(x);
		
		y = Mathf.Clamp (y, yBorder.x, yBorder.y);

		dist += Input.GetAxis("Mouse ScrollWheel") * -2;
    }

    // private float lastX;

    private void LateUpdate() {
        RaycastHit hit;
        // cam.cullingMask = cullingMask;
        position = Vector3.Slerp(position, Quaternion.Euler (y, x, 0) * Vector3.forward * -dist, smooth);
        Vector3 point = position + target.position;
        
        if(Physics.SphereCast(target.position, castRadius, position, out hit, dist, layerMask) && collision) {
            if(Physics.CheckSphere(position + target.position, radius, pass)
                || hit.collider.gameObject.layer != LayerMask.NameToLayer("Passthrough")) {
                point = hit.point + hit.normal * offset ;
                // hitDistance = hit.distance - offset;
                wasRay = true;
            }
            
            // ray = true;
            // Debug.DrawLine(hit.point, hit.point + hit.normal, Color.blue);
            // Vector3 yOnly = newPosition;
            // yOnly.x = transform.localPosition.x;
            // yOnly.z = transform.localPosition.z;
            // transform.localPosition = yOnly;
        }/*  else if (wasRay) {
            dist = hitDistance;
            wasRay = false;
        } else {
            dist = Mathf.Lerp(dist, distance, smooth);
        } */
        
        transform.localPosition = point - target.position + up;
        
        // if(newPosition.magnitude < close) {
        if(inside = Physics.CheckSphere(point, radius, player)) {
                // if(y < 30) y = 30;
                if(Physics.Raycast(point, Vector3.up, out hit, close.y, layerMask))
                    up = Vector3.Lerp(up, Vector3.up * Mathf.Clamp(hit.distance, close.x, close.y), smooth);
                else
                    up = Vector3.Lerp(up, Vector3.up * close.y, smooth);
                // Vector3 yLess = transform.localPosition;
                // yLess.y = 0.6F;
                // transform.localPosition = yLess;
                // Vector3 start = newPosition + target.position + Vector3.down * 0.6f;
                // if(Physics.SphereCast(target.position, radius, newPosition, out hit, newPosition.magnitude+0.1f, layerMask)) {
                //     newPosition =  hit.point - target.position + hit.normal * 0.04f;
                // }
                // Vector3 now = newPosition; now.y = transform.localPosition.y;
                // transform.localPosition = Vector3.Lerp(transform.localPosition, now, close);
                // x += 180; if(x > 360) x -= 360;
                // x += Mathf.Sign(lastX - x) * 30;
                // y += 6;

                // if(Physics.CheckSphere(newPosition + target.position, 0.4f, player)) {
                //     cam.cullingMask = cullingMask ^ player;
                // }
        } else {
            up = Vector3.Lerp(up, Vector3.zero, smooth);
        }

        /* if(project && ray) {
            Vector3 projection = Vector3.Project(newPosition + target.position - transform.position, hit.normal);
            // if(!inside)
                // projection.y = newPosition.y - transform.localPosition.y;
            // if(projection.normalized == hit.normal)
                transform.localPosition += projection;
        }
        transform.localPosition = Lerp(transform.localPosition, newPosition, ((inside) ? close : smooth * Time.deltaTime)); */

        // transform.LookAt (target);
        Quaternion rotation = Quaternion.LookRotation(target.position - transform.position);
        // transform.rotation = Quaternion.Lerp(transform.rotation, rotation, 0.8f);
        transform.localRotation = rotation;
    }
}
