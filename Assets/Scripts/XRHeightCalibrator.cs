using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

/// <summary>
/// Keeps the CharacterController height and center synced to the player's
/// real head height so the capsule never makes you feel like a midget.
/// Attach to the XR Origin GameObject.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(XROrigin))]
public class XRHeightCalibrator : MonoBehaviour
{
    [Header("Height Settings")]
    [Tooltip("Minimum capsule height in case tracking is lost.")]
    [SerializeField] private float minHeight = 1.0f;
    [Tooltip("Extra buffer added above the head for the capsule top.")]
    [SerializeField] private float heightBuffer = 0.1f;
    [Tooltip("How far off the ground the capsule bottom sits. Match your Physics step-offset.")]
    [SerializeField] private float capsuleFootOffset = 0.05f;

    private CharacterController cc;
    private XROrigin xrOrigin;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        xrOrigin = GetComponent<XROrigin>();
    }

    private void Update()
    {
        // Camera height in LOCAL space of the XR Origin
        float headHeight = xrOrigin.CameraInOriginSpacePos.y;
        float capsuleHeight = Mathf.Max(minHeight, headHeight + heightBuffer);

        cc.height = capsuleHeight;

        // Center the capsule so its bottom stays near the floor
        cc.center = new Vector3(
            xrOrigin.CameraInOriginSpacePos.x,
            capsuleHeight / 2f + capsuleFootOffset,
            xrOrigin.CameraInOriginSpacePos.z
        );
    }
}