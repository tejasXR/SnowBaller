using System;
using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkHealth : NetworkBehaviour
    {
        private event Action<float> HealthValueChangedCallback;
        public event Action<PlayerRef> NoHealthLeftCallback;
        
        [SerializeField] private float maxHealth = 2;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Color healthNormalColor;
        [SerializeField] private Color healthCriticalColor;
        
        public float HealthPercentage => _currentHealthLocal / maxHealth;
        
        // [Networked(OnChanged = nameof(OnRemoteCurrentHealthChanged))]
        // public float AnotherHealth { get; set; }

        
        [Networked] 
        public float CurrentHealth { get; set; }
        
        [Networked, OnChangedRender(nameof(OnRemoteColorChanged))]
        private Color NetworkedColor { get; set; }

        private float _currentHealthLocal;
        private ChangeDetector _changeDetector;
        
        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            
            ResetHealth();
            HealthValueChangedCallback += OnPlayerHealthValueChanged;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            HealthValueChangedCallback -= OnPlayerHealthValueChanged;
        }

        public override void FixedUpdateNetwork()
        {
            foreach (var property in _changeDetector.DetectChanges( this, out var previousBuffer, out var currentBuffer))
            {
                switch (property)
                {
                    case nameof(CurrentHealth):
                    {
                        var reader = GetPropertyReader<float>(property);
                        var (previous,current) = reader.Read(previousBuffer, currentBuffer);
                        _currentHealthLocal = current;
                        break;
                    }
                }
            }
        }

        private void ResetHealth()
        {
            ChangeLocalHealthValue(maxHealth);
        }
        
        public void Reduce(float damage)
        {
            var newHealth = CurrentHealth - damage;
            ChangeLocalHealthValue(newHealth);
        }

        private void OnPlayerHealthValueChanged(float healthValue)
        {
            var color = Color.Lerp(healthCriticalColor, healthNormalColor, HealthPercentage);
            ChangeLocalColor(color);
        }

        private void OnDead()
        {
            NoHealthLeftCallback?.Invoke(Runner.LocalPlayer);
            ResetHealth();
        }

        private void ChangeLocalColor(Color color)
        {
            NetworkedColor = color;
        }

        private void OnRemoteColorChanged()
        {
            meshRenderer.material.color = NetworkedColor;
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