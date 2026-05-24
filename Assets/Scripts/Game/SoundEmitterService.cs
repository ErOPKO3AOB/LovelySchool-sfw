using Assets.Scripts.Data;
using UnityEngine;

namespace Game
{
    public class SoundEmitterService
    {
        public SoundEmitterService(PooledSpawnService pooledSpawnService, GameSettingsSO playerSettingsSO)
        {
            _pooledSpawnService = pooledSpawnService;
            _playerSettingsSO = playerSettingsSO;
        }

        private readonly PooledSpawnService _pooledSpawnService;
        private readonly GameSettingsSO _playerSettingsSO;

        public void PlaySound(string name, Transform point, float lifeTime)
        {
            _pooledSpawnService.SpawnObject(
                _playerSettingsSO.soundPrefabs.Find(s => s.name == name),
                name,                
                point.position,
                point.eulerAngles,
                lifeTime);
        }
    }
}