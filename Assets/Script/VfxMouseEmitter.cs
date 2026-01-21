using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Sends MouseWorld (Vector3) + Emit (float) to a VisualEffect.
/// - Computes mouse world point by intersecting a ray with a plane.
/// - Supports two plane modes to avoid ok=false in edge camera angles.
/// - Optionally converts World -> Local if your VFX System Space is Local.
/// </summary>
[DisallowMultipleComponent]
public sealed class VfxMouseEmitterPlaneOnly : MonoBehaviour
{
    private enum PlaneMode
    {
        WorldYPlane,   // Plane y = planeY
        CameraPlane    // Plane facing camera, at distance in front of camera
    }

    [Header("Refs")]
    [SerializeField] private VisualEffect vfx;
    [SerializeField] private Camera cam;

    [Header("VFX Property Names (must match Blackboard exactly)")]
    [SerializeField] private string mouseWorldName = "MouseWorld";
    [SerializeField] private string emitName = "Emit";

    [Header("Emit")]
    [SerializeField] private int mouseButton = 0; // 0=Left

    [Header("Plane Settings")]
    [SerializeField] private PlaneMode planeMode = PlaneMode.WorldYPlane;

    [Tooltip("Used when PlaneMode = WorldYPlane")]
    [SerializeField] private float planeY = 0f;

    [Tooltip("Used when PlaneMode = CameraPlane (plane placed in front of camera)")]
    [SerializeField, Min(0.01f)] private float cameraPlaneDistance = 5f;

    [Header("Space")]
    [Tooltip("Turn ON if your VFX System Space is Local and you want mouse point to match that space.")]
    [SerializeField] private bool sendMouseAsLocal = true;

    [Header("Debug")]
    [SerializeField] private bool logEveryHalfSecond = true;

    private float _logTimer;

    private void Reset()
    {
        vfx = GetComponent<VisualEffect>();
        cam = Camera.main;
    }

    private void Awake()
    {
        if (!vfx) vfx = GetComponent<VisualEffect>();
        if (!cam) cam = Camera.main;

        Debug.Log($"[VFX] Awake vfx={(vfx ? vfx.name : "NULL")} cam={(cam ? cam.name : "NULL")}", this);

        if (!vfx)
        {
            Debug.LogError("[VFX] VisualEffect missing. Add Visual Effect component or assign 'vfx'.", this);
            enabled = false;
        }

        if (!cam)
        {
            Debug.LogError("[VFX] Camera missing. Assign 'cam' or tag your camera as MainCamera.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!vfx || !cam) return;

        // Emit while holding
        bool holding = Input.GetMouseButton(mouseButton);
        vfx.SetFloat(emitName, holding ? 1f : 0f);

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Choose plane
        Plane plane = planeMode == PlaneMode.WorldYPlane
            ? new Plane(Vector3.up, new Vector3(0f, planeY, 0f))
            : new Plane(cam.transform.forward, cam.transform.position + cam.transform.forward * cameraPlaneDistance);

        bool ok = plane.Raycast(ray, out float enter);

        // Compute point
        Vector3 worldPoint = ok ? ray.GetPoint(enter) : Vector3.zero;

        // Convert if VFX is Local space
        Vector3 sendPoint = sendMouseAsLocal ? vfx.transform.InverseTransformPoint(worldPoint) : worldPoint;

        // Send
        vfx.SetVector3(mouseWorldName, sendPoint);

        // Debug log
        if (logEveryHalfSecond)
        {
            _logTimer += Time.deltaTime;
            if (_logTimer > 0.5f)
            {
                _logTimer = 0f;
                Debug.Log(
                    $"[VFX] ok={ok} enter={enter:F2} " +
                    $"world=({worldPoint.x:F2},{worldPoint.y:F2},{worldPoint.z:F2}) " +
                    $"sent=({sendPoint.x:F2},{sendPoint.y:F2},{sendPoint.z:F2}) " +
                    $"planeMode={planeMode} local={sendMouseAsLocal}",
                    this
                );
            }
        }
    }
}
