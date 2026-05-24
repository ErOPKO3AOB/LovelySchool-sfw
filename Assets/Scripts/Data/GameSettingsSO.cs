using Assets.Scripts.Weapons;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(menuName = "Settings/Player Settings")]
    public class GameSettingsSO : ScriptableObject
    {
        [Header("Shared data")]
        public List<GameObject> soundPrefabs;
        public GameObject ammoPrefab;
        public LayerMask targetsLayer;
        public LayerMask penetratableTargetsLayer;
        public string reloadAnimName = "Reload";
        public string pickupAnimName = "Pickup";

        [Header("Concrete data")]
        [SerializeField] private PlayerData _playerData;
        [SerializeField] private WeaponConfigSO[] _weaponConfigs;

        public PlayerData PlayerData { get { return _playerData; } }
        public WeaponConfigSO[] WeaponConfigs { get { return _weaponConfigs; } }
    }

    [Serializable]
    public class PlayerData
    {
        public PlayerFacade playerPrefab;
        public CameraFacade cameraPrefab;

        [Header("Movement")]
        public float walkSpeed = 18f;
        public float acceleration = 500f;
        public float airControl = 0.15f;

        [Header("Jump")]
        public float jumpForce = 70f;
        public float groundCheckRadius = 0.25f;
        public LayerMask groundLayer;

        [Header("Dash")]
        public float dashSpeed = 30f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 0.5f;
        public float dashStaminaCost = 40f;

        [Header("Stamina")]
        public float maxStamina = 120f;
        public float staminaRegenRate = 30f;
        public float staminaRegenDelay = 0.5f;

        [Header("Camera")]
        public float mouseSensitivity = 80f;
        public float verticalClamp = 90f;
        public Vector3 cameraOffset = new Vector3(0, 0.7f, 0);
        public float tiltAmount = 2f;
        public float tiltSmoothTime = 0.1f;
        public bool enableArms = true;
    }
}