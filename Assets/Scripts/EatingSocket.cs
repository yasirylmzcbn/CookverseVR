using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class EatingSocket : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

    void Awake()
    {
        Debug.Log("yasir123 Awake called");
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        socket.selectEntered.AddListener(OnObjectEaten);
        Debug.Log("yasir123 socket: " + socket);
    }

    void OnDestroy()
    {
        Debug.Log("yasir123 OnDestroy called");
        socket.selectEntered.RemoveListener(OnObjectEaten);
    }

    private void OnObjectEaten(SelectEnterEventArgs args)
    {
        Debug.Log("yasir123 OnObjectEaten called with: " + args.interactableObject.transform.name);
        GameObject eatenObject = args.interactableObject.transform.gameObject;
        Eatable eatable = eatenObject.GetComponent<Eatable>();
        if (eatable != null)
        {
            eatable.ApplyEffect();
            Destroy(eatenObject, 0.1f);
        }
    }
}