using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
using Unity.Cinemachine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ActiveWeapon : MonoBehaviour
{
    [SerializeField] WeaponSO[] inventoryWeapons;
    [SerializeField] WeaponSO startingWeapon;
    [SerializeField] int startingWeaponIndex = 0;
    [SerializeField] CinemachineVirtualCameraBase playerFollowCamera;
    [SerializeField] Camera weaponCamera;
    [SerializeField] GameObject zoomVignette;
    [SerializeField] TMP_Text ammoText;

    struct WeaponAmmoState
    {
        public int ammo;
        public float reloadTimer;
    }

    WeaponSO currentWeaponSO;
    Animator animator;
    StarterAssetsInputs starterAssetsInputs;
    FirstPersonController firstPersonController;
    Weapon currentWeapon;

    WeaponAmmoState[] weaponStates;
    int currentWeaponIndex = -1;

    const string SHOOT_STRING = "Shoot";

    float timeSinceLastShot;
    float defaultFOV;
    float defaultRotationSpeed;

    void Awake()
    {
        starterAssetsInputs = GetComponentInParent<StarterAssetsInputs>();
        firstPersonController = GetComponentInParent<FirstPersonController>();
        animator = GetComponent<Animator>();
        ResolveReferences();

        defaultFOV = CinemachineLensHelper.GetVerticalFov(playerFollowCamera);

        defaultRotationSpeed = firstPersonController != null
            ? firstPersonController.RotationSpeed
            : 1f;
    }

    void ResolveReferences()
    {
        if (playerFollowCamera == null)
            playerFollowCamera = CinemachineLensHelper.FindFirstVirtualCamera();

        if (weaponCamera == null)
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam.CompareTag("MainCamera") || cam.name.Contains("Weapon"))
                {
                    weaponCamera = cam;
                    break;
                }
            }

            if (weaponCamera == null)
                weaponCamera = Camera.main;
        }

        if (ammoText == null)
        {
            GameObject ammoObject = GameObject.Find("Ammo Text");
            if (ammoObject != null)
                ammoText = ammoObject.GetComponent<TMP_Text>();
        }

        if (zoomVignette == null)
        {
            GameObject vignette = GameObject.Find("Zoom Vignette");
            if (vignette != null)
                zoomVignette = vignette;
        }
    }

    void Start()
    {
        EnsureInventory();
        weaponStates = new WeaponAmmoState[inventoryWeapons.Length];

        for (int i = 0; i < inventoryWeapons.Length; i++)
            RefillWeaponMagazine(i);

        int startIndex = Mathf.Clamp(startingWeaponIndex, 0, inventoryWeapons.Length - 1);
        EquipWeaponIndex(startIndex);
    }

    void Update()
    {
        UpdateReloadTimers();
        HandleWeaponSwitch();
        HandleShoot();
        HandleZoom();
        UpdateAmmoUI();
    }

    void EnsureInventory()
    {
        if (inventoryWeapons != null && inventoryWeapons.Length >= 3)
            return;

        WeaponSO pistol = null;
        WeaponSO machineGun = null;
        WeaponSO sniper = null;

        foreach (WeaponSO weapon in Resources.FindObjectsOfTypeAll<WeaponSO>())
        {
            switch (weapon.name)
            {
                case "Pistol": pistol = weapon; break;
                case "Machine Gun": machineGun = weapon; break;
                case "Sniper Rifle": sniper = weapon; break;
            }
        }

        var list = new List<WeaponSO>();
        if (startingWeapon != null) list.Add(startingWeapon);
        if (pistol != null && !list.Contains(pistol)) list.Add(pistol);
        if (machineGun != null && !list.Contains(machineGun)) list.Add(machineGun);
        if (sniper != null && !list.Contains(sniper)) list.Add(sniper);

        if (list.Count > 0)
            inventoryWeapons = list.ToArray();
    }

    void UpdateReloadTimers()
    {
        for (int i = 0; i < weaponStates.Length; i++)
        {
            if (weaponStates[i].reloadTimer <= 0f) continue;

            weaponStates[i].reloadTimer -= Time.deltaTime;
            if (weaponStates[i].reloadTimer <= 0f)
                RefillWeaponMagazine(i);
        }
    }

    void HandleWeaponSwitch()
    {
        if (inventoryWeapons == null || inventoryWeapons.Length == 0)
            return;

        for (int i = 0; i < inventoryWeapons.Length && i < 9; i++)
        {
            if (WasWeaponSwitchPressed(i))
                EquipWeaponIndex(i);
        }
    }

    bool WasWeaponSwitchPressed(int index)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return false;

        Key digitKey = IndexToDigitKey(index);
        Key keypadKey = IndexToNumpadKey(index);
        return digitKey != Key.None
            && (Keyboard.current[digitKey].wasPressedThisFrame
                || (keypadKey != Key.None && Keyboard.current[keypadKey].wasPressedThisFrame));
#else
        return Input.GetKeyDown(KeyCode.Alpha1 + index)
            || Input.GetKeyDown(KeyCode.Keypad1 + index);
#endif
    }

#if ENABLE_INPUT_SYSTEM
    static Key IndexToDigitKey(int index)
    {
        return index switch
        {
            0 => Key.Digit1,
            1 => Key.Digit2,
            2 => Key.Digit3,
            3 => Key.Digit4,
            4 => Key.Digit5,
            5 => Key.Digit6,
            6 => Key.Digit7,
            7 => Key.Digit8,
            8 => Key.Digit9,
            _ => Key.None
        };
    }

    static Key IndexToNumpadKey(int index)
    {
        return index switch
        {
            0 => Key.Numpad1,
            1 => Key.Numpad2,
            2 => Key.Numpad3,
            3 => Key.Numpad4,
            4 => Key.Numpad5,
            5 => Key.Numpad6,
            6 => Key.Numpad7,
            7 => Key.Numpad8,
            8 => Key.Numpad9,
            _ => Key.None
        };
    }
#endif

    public void EquipWeaponIndex(int index)
    {
        if (inventoryWeapons == null || index < 0 || index >= inventoryWeapons.Length) return;
        if (index == currentWeaponIndex && currentWeapon != null) return;

        currentWeaponIndex = index;
        SwitchWeapon(inventoryWeapons[index]);
        UpdateAmmoUI();
    }

    void RefillWeaponMagazine(int index)
    {
        WeaponSO weapon = inventoryWeapons[index];
        weaponStates[index].ammo = weapon.MagazineSize;
        weaponStates[index].reloadTimer = 0f;
    }

    void StartReload(int index)
    {
        WeaponSO weapon = inventoryWeapons[index];
        weaponStates[index].reloadTimer = weapon.ReloadTime;
        weaponStates[index].ammo = 0;
    }

    bool IsReloading(int index) => weaponStates[index].reloadTimer > 0f;

    bool CanShoot()
    {
        return !IsReloading(currentWeaponIndex)
            && weaponStates[currentWeaponIndex].ammo > 0;
    }

    void UpdateAmmoUI()
    {
        if (ammoText == null || currentWeaponIndex < 0) return;

        if (IsReloading(currentWeaponIndex))
            ammoText.text = "RLD";
        else
            ammoText.text = weaponStates[currentWeaponIndex].ammo.ToString("D2");
    }

    public void SwitchWeapon(WeaponSO weaponSO)
    {
        if (currentWeapon)
            Destroy(currentWeapon.gameObject);

        Weapon newWeapon = Instantiate(weaponSO.weaponPrefab, transform).GetComponent<Weapon>();
        currentWeapon = newWeapon;
        currentWeaponSO = weaponSO;
        timeSinceLastShot = currentWeaponSO.FireRate;
    }

    void HandleShoot()
    {
        if (currentWeapon == null || currentWeaponSO == null || starterAssetsInputs == null)
            return;

        timeSinceLastShot += Time.deltaTime;

        if (!WantsToShoot()) return;

        if (timeSinceLastShot >= currentWeaponSO.FireRate && CanShoot())
        {
            currentWeapon.Shoot(currentWeaponSO);
            animator.Play(SHOOT_STRING, 0, 0f);
            timeSinceLastShot = 0f;

            weaponStates[currentWeaponIndex].ammo--;

            if (weaponStates[currentWeaponIndex].ammo <= 0)
                StartReload(currentWeaponIndex);
        }
    }

    void HandleZoom()
    {
        if (currentWeaponSO == null)
            return;

        bool wantsZoom = IsZoomHeld() && currentWeaponSO.CanZoom;

        if (wantsZoom)
        {
            CinemachineLensHelper.SetVerticalFov(playerFollowCamera, currentWeaponSO.ZoomAmount);
            if (weaponCamera != null)
                weaponCamera.fieldOfView = currentWeaponSO.ZoomAmount;
            if (zoomVignette != null)
                zoomVignette.SetActive(true);
            firstPersonController?.ChangeRotationSpeed(currentWeaponSO.ZoomRotationSpeed);
        }
        else
        {
            CinemachineLensHelper.SetVerticalFov(playerFollowCamera, defaultFOV);
            if (weaponCamera != null)
                weaponCamera.fieldOfView = defaultFOV;
            if (zoomVignette != null)
                zoomVignette.SetActive(false);
            firstPersonController?.ChangeRotationSpeed(defaultRotationSpeed);
        }
    }

#if ENABLE_INPUT_SYSTEM
    bool WantsToShoot()
    {
        if (Mouse.current == null)
            return starterAssetsInputs != null && starterAssetsInputs.shoot;

        if (currentWeaponSO.isAutomatic)
            return Mouse.current.leftButton.isPressed;

        return Mouse.current.leftButton.wasPressedThisFrame;
    }

    bool IsZoomHeld()
    {
        if (Mouse.current == null)
            return starterAssetsInputs != null && starterAssetsInputs.zoom;

        return Mouse.current.rightButton.isPressed;
    }
#else
    bool WantsToShoot() => starterAssetsInputs != null && starterAssetsInputs.shoot;

    bool IsZoomHeld() => starterAssetsInputs != null && starterAssetsInputs.zoom;
#endif
}
