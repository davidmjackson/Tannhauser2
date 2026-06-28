using UnityEngine;

/// <summary>
/// Spawns a scattered field of placeholder shapes at play time so the pilot can
/// sense speed, distance, and orientation while flying. Pure test aid: it runs
/// only in Play mode and creates throwaway objects. Delete before real art.
/// </summary>
public class ReferenceField : MonoBehaviour
{
    [Tooltip("How many floating shapes to scatter.")]
    public int count = 60;
    [Tooltip("Radius of the scatter volume around the origin.")]
    public float radius = 120f;
    [Tooltip("Smallest / largest shape size.")]
    public float minScale = 2f;
    public float maxScale = 8f;
    [Tooltip("Add a large ground slab below for a sense of 'down'.")]
    public bool addGround = true;

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            PrimitiveType type = (i % 2 == 0) ? PrimitiveType.Cube : PrimitiveType.Sphere;
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = "Ref_" + i;

            Vector3 p = Random.insideUnitSphere * radius;
            p.y *= 0.5f; // flatten the field a little
            go.transform.position = p;
            go.transform.localScale = Vector3.one * Random.Range(minScale, maxScale);

            // No colliders: these are just visual reference, not obstacles.
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        if (addGround)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ref_Ground";
            ground.transform.position = new Vector3(0f, -40f, 0f);
            ground.transform.localScale = new Vector3(60f, 1f, 60f);
            Collider col = ground.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
    }
}
