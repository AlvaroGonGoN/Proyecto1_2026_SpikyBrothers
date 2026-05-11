using UnityEngine;

public class ParallaxLoop : MonoBehaviour
{
    [SerializeField] private Vector2 parallaxEffectMultiplier;
    private Transform cameraTransform;
    private Vector3 lastCamaraPosition;
    private float textureUnitSizeX;


    void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCamaraPosition = cameraTransform.position;
        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        textureUnitSizeX = sprite.bounds.size.x;
    }

    private void LateUpdate()
    {
        Vector3 deltaMovement = cameraTransform.position - lastCamaraPosition;
        transform.position += new Vector3(deltaMovement.x * parallaxEffectMultiplier.x, 0, 0);
        lastCamaraPosition = cameraTransform.position;
        float relativeDist = cameraTransform.position.x - transform.position.x;

        if (Mathf.Abs(relativeDist) >= textureUnitSizeX)
        {
            float offsetPositonX = relativeDist % textureUnitSizeX;
            transform.position = new Vector3(cameraTransform.position.x - offsetPositonX, transform.position.y);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
