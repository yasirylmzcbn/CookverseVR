using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using System.Collections;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance;
    public ContinuousMoveProvider moveProvider;

    void Awake()
    {
        Instance = this;
    }

    public void ApplySpeedBuff(float multiplier, float duration)
    {
        StartCoroutine(SpeedBuff(multiplier, duration));
    }

    private IEnumerator SpeedBuff(float multiplier, float duration)
    {
        Debug.Log("yasir123 Applying speed buff in buff manager: " + multiplier + " for duration: " + duration + " " + moveProvider.moveSpeed);
        float originalSpeed = moveProvider.moveSpeed;
        moveProvider.moveSpeed *= multiplier;
        Debug.Log("yasir123 New speed after buff: " + moveProvider.moveSpeed);


        yield return new WaitForSeconds(duration);

        moveProvider.moveSpeed = originalSpeed;
    }
}