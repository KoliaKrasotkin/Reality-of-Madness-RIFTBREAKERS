using System;
using System.Collections.Generic;
using UnityEngine;
using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
using Mirror;
using ROM.Progression;

namespace ROM.UI.Sprint01
{
    [Serializable] public sealed class RiftCharacterMetadataRecord
    {
        public int Version = 1;
        public double PlayedSeconds;
        public long FirstRecordedUtc, LastPlayedUtc;
    }
    // Secondary local display data only: never a source of XP, money, inventory or rewards.
    public static class RiftCharacterMetadata
    {
        static readonly Dictionary<string, RiftCharacterMetadataRecord> records = new Dictionary<string, RiftCharacterMetadataRecord>();
        static readonly HashSet<string> dirty = new HashSet<string>();
        public static string Key(string characterId) => "ROM.CharacterMetadata.v1." + characterId;
        public static RiftCharacterMetadataRecord Read(string characterId)
        {
            if (records.TryGetValue(characterId, out var data)) return data;
            try { data = JsonUtility.FromJson<RiftCharacterMetadataRecord>(PlayerPrefs.GetString(Key(characterId), "")); } catch { data = null; }
            if (data == null || data.Version != 1 || double.IsNaN(data.PlayedSeconds) || double.IsInfinity(data.PlayedSeconds) || data.PlayedSeconds < 0) data = new RiftCharacterMetadataRecord();
            records[characterId] = data; return data;
        }
        public static void RecordSessionSeconds(string characterId, double seconds)
        {
            if (!Guid.TryParse(characterId, out _) || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0 || seconds > 1) return;
            var data = Read(characterId); long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (data.FirstRecordedUtc == 0) data.FirstRecordedUtc = now;
            data.PlayedSeconds = Math.Min(3155760000d, data.PlayedSeconds + seconds); data.LastPlayedUtc = now; dirty.Add(characterId);
        }
        public static void Flush()
        {
            if (dirty.Count == 0) return;
            foreach (string id in dirty) PlayerPrefs.SetString(Key(id), JsonUtility.ToJson(records[id]));
            PlayerPrefs.Save(); dirty.Clear();
        }
        public static string PlayedLabel(string characterId)
        {
            var data = Read(characterId); if (data.FirstRecordedUtc == 0) return "";
            long minutes = (long)(data.PlayedSeconds / 60);
            string value = minutes == 0 ? "menos de 1 min" : minutes < 60 ? minutes + " min" : minutes / 60 + " h " + minutes % 60 + " min";
            return "Tiempo jugado: " + value;
        }
        public static string LevelLabel(string characterId)
        {
            if (!PlayerPrefs.HasKey(RiftJournalView.Key(characterId))) return "Nivel —";
            JournalState state;
            try { state = JsonUtility.FromJson<JournalState>(PlayerPrefs.GetString(RiftJournalView.Key(characterId), "")); } catch { state = null; }
            if (state == null || state.CharacterId != characterId || state.Experience < 0) return "Nivel —";
            var progression = WarriorProgressionDefinition.Load;
            return progression ? "Nivel " + progression.Level(state.Experience) : "Nivel no disponible";
        }
    }

}
