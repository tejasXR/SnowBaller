using System;
using Fusion;
using Fusion.XR.Shared.Rig;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Snowballers.Network
{
    public class NetworkPlayerHealth : NetworkBehaviour
    {
        public event Action<PlayerRef> PlayerDeadCallback;
        
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Color healthNormalColor;
        [SerializeField] private Color healthCriticalColor;
        
        [Networked, OnChangedRender(nameof(OnRemoteColorChanged))]
        private Color NetworkedColor { get; set; }
        
        private PlayerHealth _playerHealth;

        private void Update()
        {
            if (HasStateAuthority && Input.GetKeyDown(KeyCode.E))
            {
                // Changing the material color here directly does not work since this code is only executed on the client pressing the button and not on every client.
               var newColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
               ChangeLocalColor(newColor);
            }
        }
        
        public override void Spawned()
        {
            base.Spawned();

            if (!Object || !Object.HasStateAuthority)
            {
                return;
            }
            
            var hardwareRig = FindFirstObjectByType<HardwareRig>(FindObjectsInactive.Exclude);
            _playerHealth = hardwareRig.GetComponentInChildren<PlayerHealth>();
                
            if (_playerHealth == null)
            {
                Debug.LogError($"Cannot find {nameof(PlayerHealth)} component on local {nameof(HardwareRig)} object {hardwareRig.gameObject.name}");
                return;
            }

            _playerHealth.HealthValueChangedCallback += OnPlayerHealthValueChanged;
            _playerHealth.PlayerDeadCallback += OnPlayerDead;
        }

        private void OnPlayerHealthValueChanged(float healthValue)
        {
            if (_playerHealth.HealthPercentage <= .5F)
            {
                ChangeLocalColor(healthCriticalColor);
            }

            if (_playerHealth.HealthPercentage > .5F)
            {
                ChangeLocalColor(healthNormalColor);
            }
        }

        private void OnPlayerDead()
        {
            PlayerDeadCallback?.Invoke(Runner.LocalPlayer);
        }

        private void ChangeLocalColor(Color color)
        {
            NetworkedColor = color;
        }

        private void OnRemoteColorChanged()
        {
            meshRenderer.material.color = NetworkedColor;
        }
    }
}


