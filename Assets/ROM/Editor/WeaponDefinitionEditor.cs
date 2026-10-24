using System.Collections.Generic;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEngine;

namespace ROM.EditorTools
{
    [CustomEditor(typeof(WeaponDefinition))]
    public sealed class WeaponDefinitionEditor : UnityEditor.Editor
    {
        static readonly HashSet<string> OffensiveFields = new HashSet<string>
        { "DamageMultiplier", "AttackSpeed", "Reach", "Attack", "ImpactVfx" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var role = serializedObject.FindProperty("Role");
            EditorGUILayout.PropertyField(role);
            bool shield = role.enumValueIndex == (int)EquipmentRole.Shield;
            var property = serializedObject.GetIterator();
            bool children = true;
            while (property.NextVisible(children))
            {
                children = false;
                if (property.name == "Role" || (shield && OffensiveFields.Contains(property.name))) continue;
                using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                    EditorGUILayout.PropertyField(property, true);
            }
            if (shield)
                EditorGUILayout.HelpBox("Shield equipment has no attack damage, attack speed or reach. Its defensive effects are currently configured by the character's combat system.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("For the Warrior, Attack Speed and Reach are currently reserved fields. Attack timing and range come from abilities and AutoAttackProfile.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
