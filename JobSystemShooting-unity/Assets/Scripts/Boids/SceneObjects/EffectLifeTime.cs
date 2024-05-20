using System;
using UnityEngine;

namespace Shitakami.Boids.SceneObjects
{
    public class EffectLifeTime : MonoBehaviour
    {
        [SerializeField] private float _lifeTime;
        private float _timeAlive;

        private Action<EffectLifeTime> _onObjectDestroyed;

        private void Update()
        {
            _timeAlive += Time.deltaTime;
            if (_timeAlive >= _lifeTime)
            {
                _onObjectDestroyed?.Invoke(this);
                gameObject.SetActive(false);
            }
        }

        public void SetEffectDestroyedEvent(Action<EffectLifeTime> onObjectDestroyed)
        {
            _onObjectDestroyed = onObjectDestroyed;
        }

        private void OnEnable()
        {
            _timeAlive = 0;
        }
    }
}