using System.Collections;
using UnityEngine;

// Lightweight "cube cracks apart" VFX for a correct placement: a handful of
// small tinted cubes scatter outward and shrink to nothing over a fraction
// of a second, then the whole burst self-destroys. Purely transient (one
// GameObject per placement, gone in ~0.3s) - not a per-cell object, so it
// doesn't touch the board's GPU-instancing budget at any grid size.
public class CrackFragments : MonoBehaviour
{
    const int FragmentCount = 5;
    const float Duration = 0.32f;

    public static void Spawn(Vector3 position, Color color, float pieceSize)
    {
        var go = new GameObject("Crack Fragments") { transform = { position = position } };
        CrackFragments runner = go.AddComponent<CrackFragments>();
        runner.StartCoroutine(runner.Run(color, pieceSize));
    }

    IEnumerator Run(Color color, float pieceSize)
    {
        var mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };

        var fragments = new Transform[FragmentCount];
        var velocities = new Vector3[FragmentCount];
        var baseScales = new float[FragmentCount];

        for (int i = 0; i < FragmentCount; i++)
        {
            var fragmentGO = new GameObject("Fragment");
            fragmentGO.transform.SetParent(transform, false);
            fragmentGO.AddComponent<MeshFilter>().sharedMesh = mesh;
            fragmentGO.AddComponent<MeshRenderer>().sharedMaterial = material;
            baseScales[i] = pieceSize * Random.Range(0.16f, 0.28f);
            fragmentGO.transform.localScale = Vector3.one * baseScales[i];
            fragments[i] = fragmentGO.transform;

            Vector2 dir = Random.insideUnitCircle.normalized;
            velocities[i] = new Vector3(dir.x, Random.Range(0.8f, 1.4f), dir.y) * Random.Range(1.4f, 2.4f);
        }

        float t = 0f;
        while (t < Duration)
        {
            t += Time.deltaTime;
            float t01 = t / Duration;
            float scaleMul = Mathf.Lerp(1f, 0f, t01 * t01);

            for (int i = 0; i < FragmentCount; i++)
            {
                velocities[i] += Vector3.down * 7f * Time.deltaTime;
                fragments[i].position += velocities[i] * Time.deltaTime;
                fragments[i].localScale = Vector3.one * baseScales[i] * scaleMul;
                fragments[i].Rotate(Vector3.one * (360f * Time.deltaTime), Space.Self);
            }

            yield return null;
        }

        Destroy(material);
        Destroy(gameObject);
    }
}
