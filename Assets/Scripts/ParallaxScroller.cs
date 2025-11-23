using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ParallaxScroller : MonoBehaviour
{
    [Tooltip("Camera used for parallax. If null, Camera.main is used.")]
    public Transform cameraTransform;
    [Range(0f, 1f)] public float parallaxFactor = 0.3f; // 0 = fixed to camera, 1 = moves with camera (usually <1)
    public bool onlyHorizontal = true; // sky often only needs horizontal parallax

    Renderer rend;
    Vector3 previousCamPos;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform != null) previousCamPos = cameraTransform.position;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 camDelta = cameraTransform.position - previousCamPos;

        // Move texture offset opposite to camera movement for parallax
        Vector2 offset = rend.material.mainTextureOffset;
        if (onlyHorizontal)
        {
            offset.x += camDelta.x * parallaxFactor / transform.localScale.x;
        }
        else
        {
            offset.x += camDelta.x * parallaxFactor / transform.localScale.x;
            offset.y += camDelta.y * parallaxFactor / transform.localScale.y;
        }

        // keep offset in 0..1 range to avoid float growth
        offset.x = offset.x % 1f;
        offset.y = offset.y % 1f;

        rend.material.mainTextureOffset = offset;

        previousCamPos = cameraTransform.position;
    }
}
