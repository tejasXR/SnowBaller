using System;
using Fusion;
using Fusion.XR.Shared.Rig;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkHealth : NetworkBehaviour
    {
        public event Action<PlayerRef> NoHealthLeft;
        
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Color healthNormalColor;
        [SerializeField] private Color healthCriticalColor;
        
        [Networked, OnChangedRender(nameof(OnRemoteColorChanged))]
        private Color NetworkedColor { get; set; }
        
        private Health _playerHealth;
        
        public override void Spawned()
        {
            base.Spawned();

            if (!Object || !Object.HasStateAuthority)
            {
                return;
            }
            
            var hardwareRig = FindFirstObjectByType<HardwareRig>(FindObjectsInactive.Exclude);
            if (!hardwareRig)
            {
                return;
            }
            
            _playerHealth = hardwareRig.GetComponentInChildren<Health>();
            if (_playerHealth == null)
            {
                Debug.LogError($"Cannot find {nameof(Health)} component on local {nameof(HardwareRig)} object {hardwareRig.gameObject.name}");
                return;
            }

            _playerHealth.HealthValueChangedCallback += OnPlayerHealthValueChanged;
            _playerHealth.NoHealthLeftCallback += OnDead;
        }

        private void OnPlayerHealthValueChanged(float healthValue)
        {
            var color = Color.Lerp(healthNormalColor, healthCriticalColor, _playerHealth.HealthPercentage);
            ChangeLocalColor(color);
        }

        private void OnDead()
        {
            NoHealthLeft?.Invoke(Runner.LocalPlayer);
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