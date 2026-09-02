using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class BoardCameraFitter : MonoBehaviour
{
    [SerializeField] CinemachineCamera targetCamera;
    [SerializeField] float paddingFactor = 1.28f;

    void Start()
    {
        StartCoroutine(FitWhenReady());
    }

    IEnumerator FitWhenReady()
    {
        yield return new WaitUntil(() => LevelSettings.Instance != null);

        LevelSettings settings = LevelSettings.Instance;
        Fit(settings.GridWidth, settings.GridHeight, settings.PieceSize);
    }

    public void Fit(int gridWidth, int gridHeight, float pieceSize)
    {
        if (targetCamera == null)
            targetCamera = FindAnyObjectByType<CinemachineCamera>();

        if (targetCamera == null)
        {
            Debug.LogWarning("BoardCameraFitter: no CinemachineCamera found to fit.");
            return;
        }

        float worldWidth = gridWidth * pieceSize;
        float worldHeight = gridHeight * pieceSize;

        float aspect = UnityEngine.Camera.main != null ? UnityEngine.Camera.main.aspect : 16f / 9f;
        float fieldOfView = targetCamera.Lens.FieldOfView;
        float tanHalfFov = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);

        float distanceForHeight = (worldHeight * 0.5f) / tanHalfFov;
        float distanceForWidth = (worldWidth * 0.5f) / (tanHalfFov * aspect);
        float distance = Mathf.Max(distanceForHeight, distanceForWidth) * paddingFactor;

        float centerX = (gridWidth - 1) * pieceSize * 0.5f;
        float centerZ = (gridHeight - 1) * pieceSize * 0.5f;

        Transform cameraTransform = targetCamera.transform;
        cameraTransform.position = new Vector3(centerX, distance, centerZ);
    }
}
