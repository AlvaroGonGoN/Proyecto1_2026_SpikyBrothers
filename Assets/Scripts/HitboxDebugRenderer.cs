// HitBoxDebugRenderer.cs
// Coloca este script en cada GameObject de HitBox o HurtBox.
// Solo actúa en el Editor, sin coste en build.
using UnityEngine;

#if UNITY_EDITOR
[RequireComponent(typeof(Collider2D))]
public class HitBoxDebugRenderer : MonoBehaviour
{
    [SerializeField] private Color activeColor = new Color(1f, 0f, 0f, 0.4f);
    [SerializeField] private Color inactiveColor = new Color(1f, 0f, 0f, 0.1f);
    [SerializeField] private bool isHurtBox = false;

    private Collider2D col;

    private void Awake() => col = GetComponent<Collider2D>();

    private void OnDrawGizmos()
    {
        col = GetComponent<Collider2D>();
        if (col == null) return;

        Color c = isHurtBox
            ? new Color(0f, 1f, 0f, col.enabled ? 0.35f : 0.1f)
            : new Color(1f, 0f, 0f, col.enabled ? 0.35f : 0.1f);

        Gizmos.color = c;

        if (col is BoxCollider2D box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
        }
        else if (col is CircleCollider2D circle)
        {
            Gizmos.DrawSphere((Vector3)circle.offset + transform.position, circle.radius);
        }
        else if (col is CapsuleCollider2D capsule)
        {
            // Aproximación con cubo para cápsulas
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(capsule.offset, capsule.size);
        }
    }
}
#endif
