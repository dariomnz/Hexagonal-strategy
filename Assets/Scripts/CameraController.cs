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
    public int cameraDragSpeed = 300;
    public HexGrid hexGrid;
    public bool IsCentringMap = true;

    Vector3 angle;

    void Start()
    {
        originSpeed = speed;
        angle = transform.eulerAngles;
    }

    public void ValidatePosition()
    {
        Vector3 position = transform.position;
        position.x = hexGrid.cellCountX / 2 * HexMetrics.xDiameter;
        position.z = hexGrid.cellCountZ / 2 * HexMetrics.zDiameter;
        transform.position = position;
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
        if (IsCentringMap)
            CenterMap();

        Quaternion rotatex = Quaternion.AngleAxis(transform.position.x * 2, Vector3.right);
        Quaternion rotatez = Quaternion.AngleAxis(transform.position.z * 2, Vector3.forward);
        Quaternion rotation = rotatex * rotatez;


        // Quaternion skyrotation = Quaternion.Euler((transform.position.x * 2) % 360, 0, (transform.position.z * 2) % 360);
        // skyrotation.Rotate(transform.position.x * 2, 0, transform.position.z * 2);
        // Vector3 rotation = skyrotation.eulerAngles;
        // Quaternion skyrotation = Quaternion.FromToRotation(Vector3.up, new Vector3((transform.position.x * 2) % 360, 0, (transform.position.z * 2) % 360));
        // Vector3 rotation = skyrotation.eulerAngles;
        RenderSettings.skybox.SetFloat("_RotationX", rotation.x);
        RenderSettings.skybox.SetFloat("_RotationY", rotation.y);

        RenderSettings.skybox.SetFloat("_RotationZ", rotation.z);

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

    public void CenterMap()
    {
        hexGrid.CenterMap(transform.position.x, transform.position.z);
    }
}
