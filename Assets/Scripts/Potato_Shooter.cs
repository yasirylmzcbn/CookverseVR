using UnityEngine;
using System.Collections;
using System.Resources;

public class Potato_Shooter : MonoBehaviour
{

    public GameObject Bullet;
    public Transform Shoot_Pos;
    public int initialAmmo;
    public float shootCooldownDuration;
    public float reloadDuration = 2f;
    private int ammo;
    private Coroutine reloadCoroutine;
    private Coroutine shootCooldownCoroutine;
    enum WeaponState
    {
        Ready,
        Cooldown, //async op to wait for the cooldown and then set it to ready or empty
        Reloading,
        Empty
    }
    private WeaponState state;
    [Header("Aiming")]
    [Tooltip("Optional override. If empty, uses the currently active camera from SwitchCamera, else Camera.main.")]
    [SerializeField] private Transform aimReference;

    [Tooltip("If true, aim is computed by raycasting from the camera forward and shooting towards the hit point. Helps in third-person.")]
    [SerializeField] private bool useCameraRaycastAim = true;

    [SerializeField] private float aimMaxDistance = 200f;

    [Tooltip("Layers the aim ray can hit.")]
    [SerializeField] private LayerMask aimLayers = ~0;

    private SwitchCamera _switchCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        _switchCamera = FindFirstObjectByType<SwitchCamera>();
        ammo = initialAmmo;

    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Shoot()
    {
        if (state == WeaponState.Cooldown || state == WeaponState.Reloading)
        {
            return;
        }
        if (ammo <= 0)
        {
            state = WeaponState.Empty;
            //make a click sound or change color in grayboxing
            return;
        }
        if (Bullet == null || Shoot_Pos == null) return;

        Transform aim = GetAimTransform();
        Vector3 direction = GetAimDirection(aim);

        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Instantiate(Bullet, Shoot_Pos.position, rotation);
        ammo -= 1;
        SetCooldown();
    }

    public void TryReload()
    {
        if (state == WeaponState.Reloading)
        {
            return;
        }

        if (reloadCoroutine != null)
        {
            return;
        }

        reloadCoroutine = StartCoroutine(ReloadRoutine());
        state = WeaponState.Reloading;
    }

    IEnumerator ReloadRoutine()
    {
        //animation
        yield return new WaitForSeconds(reloadDuration);
        ammo = initialAmmo;
        state = WeaponState.Ready;
        reloadCoroutine = null;
    }

    public void SetCooldown()
    {
        if (state == WeaponState.Cooldown || shootCooldownCoroutine != null || state == WeaponState.Reloading)
        {
            return;
        }

        shootCooldownCoroutine = StartCoroutine(WaitCooldownRoutine());
        state = WeaponState.Cooldown;
    }

    IEnumerator WaitCooldownRoutine()
    {
        yield return new WaitForSeconds(shootCooldownDuration);
        if (state == WeaponState.Cooldown) //so that shooting during reload can't happen
        {
            if (ammo <= 0)
            {
                state = WeaponState.Empty;
            }
            else
            {
                state = WeaponState.Ready;
            }
        }
        shootCooldownCoroutine = null;
    }

    private Transform GetAimTransform()
    {
        if (aimReference != null)
            return aimReference;

        if (_switchCamera == null)
            _switchCamera = FindFirstObjectByType<SwitchCamera>();

        if (_switchCamera != null)
        {
            if (_switchCamera.kitchenCamera != null && _switchCamera.kitchenCamera.activeInHierarchy)
                return _switchCamera.kitchenCamera.transform;

            if (_switchCamera.thirdPersonCamera != null && _switchCamera.thirdPersonCamera.activeInHierarchy)
                return _switchCamera.thirdPersonCamera.transform;

            if (_switchCamera.firstPersonCamera != null && _switchCamera.firstPersonCamera.activeInHierarchy)
                return _switchCamera.firstPersonCamera.transform;
        }

        return Camera.main != null ? Camera.main.transform : transform;
    }

    private Vector3 GetAimDirection(Transform aim)
    {
        if (!useCameraRaycastAim || aim == null)
            return aim != null ? aim.forward : transform.forward;

        Ray ray = new Ray(aim.position, aim.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, aimMaxDistance, aimLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 toHit = hit.point - Shoot_Pos.position;
            return toHit.sqrMagnitude > 0.0001f ? toHit.normalized : aim.forward;
        }

        return aim.forward;
    }
}
