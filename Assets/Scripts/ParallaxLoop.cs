using UnityEngine;

public class ParallaxLoop : MonoBehaviour
{
    [SerializeField] private Vector2 parallaxEffectMultiplier;

    private Transform cameraTransform;
    private Vector3   lastCameraPosition;
    private float     textureUnitSizeX;

    void Start()
    {
        cameraTransform    = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;
        textureUnitSizeX   = GetComponent<SpriteRenderer>().sprite.bounds.size.x;
    }

    void LateUpdate()
    {
        Vector3 delta = cameraTransform.position - lastCameraPosition;
        transform.position += new Vector3(delta.x * parallaxEffectMultiplier.x, 0f, 0f);
        lastCameraPosition  = cameraTransform.position;

        float relativeDist = cameraTransform.position.x - transform.position.x;
        if (Mathf.Abs(relativeDist) >= textureUnitSizeX)
        {
            float offset = relativeDist % textureUnitSizeX;
            transform.position = new Vector3(cameraTransform.position.x - offset, transform.position.y);
        }
    }
}
