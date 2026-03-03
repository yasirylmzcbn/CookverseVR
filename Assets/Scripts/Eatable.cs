using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using System.Collections;

public class Eatable : MonoBehaviour
{
    [Header("Buff Settings")]
    public float speedMultiplier = 10f;
    public float buffDuration = 10f;

    [Header("References")]
    public ContinuousMoveProvider moveProvider;

    public void ApplyEffect()
    {
        Debug.Log("yasir123 Applying speed buff: " + speedMultiplier + " for duration: " + buffDuration);
        BuffManager.Instance.ApplySpeedBuff(speedMultiplier, buffDuration);
    }

    private IEnumerator SpeedBuff()
    {
        float originalSpeed = moveProvider.moveSpeed;
        moveProvider.moveSpeed *= speedMultiplier;

        yield return new WaitForSeconds(buffDuration);

        moveProvider.moveSpeed = originalSpeed;
    }
}