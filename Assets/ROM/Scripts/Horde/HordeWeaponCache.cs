using Mirror;
using MultiFPS.Gameplay;
using UnityEngine;

namespace ROM.Horde
{
    /// <summary>
    /// Arsenal tirado en el centro de la plataforma. Cada slot tiene un arma en el
    /// piso; cuando alguien la levanta, el slot queda vacío y se repone después de
    /// un rato. Al terminar una oleada el gamemode pide reponer todo de golpe.
    ///
    /// Solo corre en servidor. Las armas son prefabs del kit ya registrados en el
    /// NetworkManager, así que alcanza con instanciarlas y hacer NetworkServer.Spawn,
    /// igual que ItemSpawner.
    /// </summary>
    public class HordeWeaponCache : MonoBehaviour
    {
        [Tooltip("Armas disponibles. El slot i usa el arma i (dando la vuelta si hay más slots que armas).")]
        [SerializeField] Item[] _weapons;
        [SerializeField] Transform[] _slots;
        [Tooltip("Segundos hasta que un slot vaciado vuelve a tener arma.")]
        [SerializeField] float _respawnDelay = 25f;

        Item[] _spawned;

        //momento en que el slot se repone; -1 mientras tenga arma
        float[] _respawnAt;

        void Awake()
        {
            int count = _slots != null ? _slots.Length : 0;
            _spawned = new Item[count];

            //en 0 para que el primer Update llene todos los slots
            _respawnAt = new float[count];
        }

        void Update()
        {
            if (!NetworkServer.active || _weapons == null || _weapons.Length == 0)
                return;

            for (int i = 0; i < _spawned.Length; i++)
            {
                if (!SlotIsEmpty(i))
                {
                    _respawnAt[i] = -1f;
                    continue;
                }

                if (_respawnAt[i] < 0f)
                    _respawnAt[i] = Time.time + _respawnDelay;

                if (Time.time >= _respawnAt[i])
                    SpawnInSlot(i);
            }
        }

        /// <summary>
        /// Repone en el acto todos los slots vacíos. Solo servidor.
        /// </summary>
        public void Server_Restock()
        {
            if (!NetworkServer.active || _weapons == null || _weapons.Length == 0)
                return;

            for (int i = 0; i < _spawned.Length; i++)
            {
                if (SlotIsEmpty(i))
                    SpawnInSlot(i);
            }
        }

        bool SlotIsEmpty(int slot)
        {
            Item item = _spawned[slot];

            //destruida, o la tiene alguien en la mano
            return item == null || item.MyOwner != null;
        }

        void SpawnInSlot(int slot)
        {
            Transform point = _slots[slot];
            Item prefab = _weapons[slot % _weapons.Length];

            if (!point || !prefab)
                return;

            Item item = Instantiate(prefab, point.position, point.rotation);
            NetworkServer.Spawn(item.gameObject);

            _spawned[slot] = item;
            _respawnAt[slot] = -1f;
        }
    }
}
