using Player;
using UnityEngine;
using System.Collections.Generic;
using Game;
using Assets.Scripts.Input.UI;

namespace Data
{
    [CreateAssetMenu(menuName = "Settings/Player Settings")]
    public class GameSettingsSO : ScriptableObject
    {
        public List<GameObject> soundPrefabs;
        public EnemyOutliner enemyOutlinerPrefab;

        public PlayerFacade playerPrefab;
        public CameraFacade cameraPrefab;  // <- добавил префаб камеры

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
        public Vector3 cameraOffset = new Vector3(0, 0.7f, 0); // позиция камеры относительно игрока
        public float tiltAmount = 2f;                          // сила наклона при повороте
        public float tiltSmoothTime = 0.1f;
        public bool enableArms = true;

        [Header("Mobile input")]
        public ButtonsCanvasController buttonsCanvasPrefab;
        public GameObject inventorySlotPrefab;
    }
}