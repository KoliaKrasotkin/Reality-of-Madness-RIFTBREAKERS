using UnityEngine;

namespace ROM.UI.Sprint01
{
    // Offline inventory. Kept entirely separate from premium currency/catalogs.
    public static class RiftVillageSupplies
    {
        public const int MaxStack = 99;
        public static bool Valid(int id) => id == 0 || id == 1;
        public static string Name(int id) => id == 0 ? "Poción de vida" : "Poción de vida mayor";
        public static int Price(int id) => id == 0 ? 25 : 60;
        public static int Resale(int id) => Price(id) / 2;
        public static float Healing(int id) => id == 0 ? .4f : .75f;
        public static string Key(int id) => RiftCharacterWallet.PotionKey(id);
        public static int Count(int id) => Valid(id) ? Mathf.Clamp(PlayerPrefs.GetInt(Key(id), 0), 0, MaxStack) : 0;
        public static bool Trade(int id, bool sell, out string message)
        {
            message = "Producto no disponible.";
            if (!Valid(id)) return false;
            int stock = Count(id), gold = RiftExplorationProgress.Gold;
            if (sell && stock == 0) { message = "No tenés esa poción para vender."; return false; }
            if (!sell && stock >= MaxStack) { message = "No podés llevar más pociones de ese tipo."; return false; }
            if (!sell && gold < Price(id)) { message = "Oro del juego insuficiente. Ganalo explorando y derrotando enemigos."; return false; }
            int nextGold = sell ? (int)System.Math.Min(1000000000L, (long)gold + Resale(id)) : gold - Price(id);
            PlayerPrefs.SetInt(RiftExplorationProgress.Key, nextGold);
            PlayerPrefs.SetInt(Key(id), stock + (sell ? -1 : 1));
            PlayerPrefs.Save();
            message = sell ? "Vendiste " + Name(id) + "." : "Compraste " + Name(id) + ". Usá H para beber.";
            return true;
        }
        public static bool Consume(int id)
        {
            if (!Valid(id) || Count(id) <= 0) return false;
            PlayerPrefs.SetInt(Key(id), Count(id) - 1);
            PlayerPrefs.Save();
            return true;
        }
    }
}
