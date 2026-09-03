using UnityEngine;

// A single beveled backing plate under the tray pieces (reuses the same
// fake-3D pillow shader as BoardPlatformFrame), so the colored cubes read
// as sitting on their own tray/platform rather than floating over the
// bare background. Sized dynamically since the tray's row count depends
// on how many pieces start masked.
public class TrayPlatform : MonoBehaviour
{
    [SerializeField] float margin = 0.6f;
    [SerializeField] float yPosition = -0.15f;

    public void Fit(Vector3 boardOrigin, float minX, float maxX, float minZ, float maxZ)
    {
        float centerX = boardOrigin.x + (minX + maxX) * 0.5f;
        float centerZ = boardOrigin.z + (minZ + maxZ) * 0.5f;
        float width = (maxX - minX) + margin * 2f;
        float depth = (maxZ - minZ) + margin * 2f;

        transform.position = new Vector3(centerX, yPosition, centerZ);
        transform.localScale = new Vector3(width / 10f, 1f, depth / 10f);
        gameObject.SetActive(width > 0f && depth > 0f);
    }
}
