using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }
    public static bool Locked
    {
        set
        {
            Instance.enabled = !value;
        }
    }
    float originSpeed;
    public float speed = 10f;
    public int cameraDragSpeed = 100;

    Vector3 angle;
    // Start is called before the first frame update
    void Start()
    {
        originSpeed = speed;
        angle = transform.eulerAngles;
    }

    void Update()
    {
        speed = Input.GetButton("Shift") ? originSpeed * 5 : originSpeed;

        if (Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized != Vector3.zero)
            transform.position += Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * Input.GetAxis("Vertical") * speed * Time.deltaTime;
        else
            transform.position += Vector3.ProjectOnPlane(transform.up, Vector3.up).normalized * Input.GetAxis("Vertical") * speed * Time.deltaTime;

        transform.position += Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized * Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        transform.position += Vector3.up * Input.GetAxis("Vertical Movement") * speed * Time.deltaTime;
        transform.position += transform.forward * Input.GetAxis("Mouse ScrollWheel") * speed * 300 * Time.deltaTime;

        if (Input.GetMouseButton(1))
        {
            float m_speed = cameraDragSpeed * Time.deltaTime;
            angle.y += Input.GetAxis("Mouse X") * -m_speed;
            angle.x += Input.GetAxis("Mouse Y") * m_speed;
            angle.z = 0;
            angle.x = Mathf.Clamp(angle.x, 0, 90);
            transform.eulerAngles = angle;
        }
    }

    float ClampAngle(float angle, float from, float to)
    {
        // accepts e.g. -80, 80
        if (angle < 0f) angle = 360 + angle;
        if (angle > 180f) return Mathf.Max(angle, 360 + from);
        return Mathf.Min(angle, to);
    }
}
