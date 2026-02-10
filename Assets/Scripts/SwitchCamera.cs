using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections.Generic;

public class SwitchCamera : MonoBehaviour
{
    public GameObject firstPersonCamera;
    public GameObject thirdPersonCamera;
    public GameObject kitchenCamera;
    public GameObject playerBody;
    // TODO add more kitchen cameras once models are created
    public enum KitchenCameras { Stove, Oven, Sink, Fryer, Microwave, }
    private Dictionary<KitchenCameras, GameObject> cameras;
    public KitchenCameras currentKitchenCamera;
    bool kitchenCam = false;
    bool firstPerson = true;

    public bool IsInKitchenCamera => kitchenCam;
    public bool IsInkitchenCamera => kitchenCam && currentKitchenCamera == KitchenCameras.Stove;

    void Start()
    {
        firstPersonCamera.SetActive(true);
        thirdPersonCamera.SetActive(false);
        kitchenCamera.SetActive(false);
        currentKitchenCamera = KitchenCameras.Stove;
        cameras = new Dictionary<KitchenCameras, GameObject>()
        {
            { KitchenCameras.Stove, kitchenCamera },
            // TODO Add other kitchen cameras here when implemented
        };
    }

    void Update()
    {
        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            CycleCameraMode();
        }
    }

    public void CycleCameraMode()
    {
        if (!kitchenCam)
        {
            if (firstPerson)
            {
                firstPersonCamera.SetActive(false);
                thirdPersonCamera.SetActive(true);
                firstPerson = false;
            }
            else
            {
                thirdPersonCamera.SetActive(false);
                firstPersonCamera.SetActive(true);
                firstPerson = true;
            }
        }
    }

    public void SwitchToKitchenCamera(KitchenCameras cam)
    {
        playerBody.SetActive(false);
        kitchenCam = true;
        firstPersonCamera.SetActive(false);
        thirdPersonCamera.SetActive(false);
        cameras[cam].SetActive(true);
        currentKitchenCamera = cam;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitKitchenCamera()
    {
        playerBody.SetActive(true);
        kitchenCam = false;
        kitchenCamera.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (firstPerson)
        {
            firstPersonCamera.SetActive(true);
        }
        else
        {
            thirdPersonCamera.SetActive(true);
        }
    }
}
