using System;
using UnityEngine;

namespace Snowballers
{
    public class Health : MonoBehaviour
    {
        public event Action<float> HealthValueChangedCallback;
        public event Action NoHealthLeftCallback;
        
        [SerializeField] private float maxHealth = 2;

        public float HealthPercentage => _currentHealth / maxHealth;
        
        private float _currentHealth;

        public void ResetHealth()
        {
            _currentHealth = maxHealth;
            HealthValueChangedCallback?.Invoke(_currentHealth);
        }
        
        public void Reduce(float damage)
        {
            _currentHealth -= damage;

            if (_currentHealth <= 0)
            {
                NoHealthLeftCallback?.Invoke();
                return;
            }
            
            HealthValueChangedCallback?.Invoke(_currentHealth);
        }
    }
}

