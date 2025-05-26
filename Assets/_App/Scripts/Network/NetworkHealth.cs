using System;
using Fusion;
using Oculus.Haptics;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkHealth : NetworkBehaviour
    {
        private event Action<float> HealthValueChangedCallback;
        public event Action<PlayerRef> NoHealthLeftCallback;
        
        [SerializeField] private float maxHealth = 2;
        [SerializeField] private Transform meshChildren;
        [SerializeField] private Color healthNormalColor;
        [SerializeField] private Color healthCriticalColor;
        [SerializeField] private HapticSource hitHaptics;

        private MeshRenderer[] _meshRenderers;
        
        public float HealthPercentage => _currentHealthLocal / maxHealth;
        
        [Networked, OnChangedRender(nameof(OnRemoteCurrentHealthChanged))] 
        public float CurrentHealth { get; set; }
        
        [Networked, OnChangedRender(nameof(OnRemoteColorChanged))]
        private Color NetworkedColor { get; set; }

        private float _currentHealthLocal;
        private ChangeDetector _changeDetector;
        
        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            _meshRenderers = meshChildren.GetComponentsInChildren<MeshRenderer>();
            
            ResetHealthRpc();
            HealthValueChangedCallback += OnPlayerHealthValueChanged;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            HealthValueChangedCallback -= OnPlayerHealthValueChanged;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void ResetHealthRpc()
        {
            ChangeLocalHealthValue(maxHealth);
        }
        
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void ReduceHealthRpc(float damage)
        {
            var newHealth = CurrentHealth - damage;
            ChangeLocalHealthValue(newHealth);
        }

        private void OnPlayerHealthValueChanged(float healthValue)
        {
            var color = Color.Lerp(healthCriticalColor, healthNormalColor, HealthPercentage);
            ChangeLocalColor(color);
            hitHaptics.Play();
        }

        private void OnDead()
        {
            NoHealthLeftCallback?.Invoke(Runner.LocalPlayer);
            ResetHealthRpc();
        }

        private void ChangeLocalColor(Color color)
        {
            NetworkedColor = color;
        }

        private void OnRemoteColorChanged()
        {
            foreach (var meshRenderer in _meshRenderers)
            {
                meshRenderer.material.color = NetworkedColor;
            }
        }

        private void ChangeLocalHealthValue(float healthValue)
        {
            CurrentHealth = healthValue;
        }

        private void OnRemoteCurrentHealthChanged()
        {
            _currentHealthLocal = CurrentHealth;
            HealthValueChangedCallback?.Invoke(CurrentHealth);
            
            if (_currentHealthLocal <= 0)
            {
                OnDead();
            }
        }
    }
}