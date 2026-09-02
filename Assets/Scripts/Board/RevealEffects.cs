using Unity.Cinemachine;
using UnityEngine;

// Central feedback hub for every placement mechanic: picking up a tray
// piece, a wrong drop, a correct placement, and a full group completing.
// Each has its own small procedural SFX (see ProceduralSfx) so every action
// feels distinct; the group-complete moment also gets a particle burst and
// a camera shake via Cinemachine Impulse.
public class RevealEffects : MonoBehaviour
{
    [SerializeField] ParticleSystem burst;
    [SerializeField] CinemachineImpulseSource impulseSource;
    [SerializeField] AudioSource audioSource;

    AudioClip _pickupClip;
    AudioClip _rejectClip;
    AudioClip _placeClip;
    AudioClip _revealClip;

    void Awake()
    {
        _pickupClip = ProceduralSfx.CreateBlip(700f, 900f, 0.05f, 0.35f);
        _rejectClip = ProceduralSfx.CreateBlip(300f, 150f, 0.18f, 0.45f);
        _placeClip = ProceduralSfx.CreateBlip(600f, 950f, 0.12f, 0.5f);
        _revealClip = ProceduralSfx.CreateChime(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.11f, 0.4f);
    }

    // Picking a piece up out of the tray.
    public void PlayPickup()
    {
        if (audioSource != null && _pickupClip != null)
            audioSource.PlayOneShot(_pickupClip);
    }

    // Dropped on the wrong cell (or off the board) - pairs with
    // PaintPiece.PlayRejectAndReturn's shake/flash.
    public void PlayReject()
    {
        if (audioSource != null && _rejectClip != null)
            audioSource.PlayOneShot(_rejectClip);
    }

    // A correct placement that didn't complete its group yet - a small pop
    // so every accepted piece feels like it registered, not just the rare
    // full-group reveal.
    public void PlayPlacement(Vector3 worldPosition, Color color)
    {
        if (burst != null)
        {
            burst.transform.position = worldPosition + Vector3.up * 0.4f;
            ParticleSystem.MainModule main = burst.main;
            main.startColor = color;
            burst.Emit(8);
        }

        if (audioSource != null && _placeClip != null)
            audioSource.PlayOneShot(_placeClip, 0.6f);
    }

    // The big moment: a whole number's group just finished and is dissolving.
    public void PlayGroupComplete(Vector3 worldPosition, Color color)
    {
        if (burst != null)
        {
            burst.transform.position = worldPosition + Vector3.up * 0.4f;
            ParticleSystem.MainModule main = burst.main;
            main.startColor = color;
            burst.Play();
        }

        if (impulseSource != null)
            impulseSource.GenerateImpulseAtPositionWithVelocity(worldPosition, Vector3.down * 0.4f);

        if (audioSource != null && _revealClip != null)
            audioSource.PlayOneShot(_revealClip);
    }
}
