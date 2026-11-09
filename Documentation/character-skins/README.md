# Character skins and shared animation

CharacterSkinController is an optional component on the gameplay player root. Its Skins list accepts CharacterSkinDefinition assets and an ExistingAnimator/ExistingVisualRoot already within that prefab. Alternatively, the definition's VisualPrefab is instantiated below VisualParent (or the player root). A visual prefab contains its own humanoid Animator, skeleton, renderers and optional RigBuilder/constraints; never assign a whole network player prefab as a visual. Keep all rig references within that visual. Do not share skeleton transforms between models.

WarriorPlayer.prefab and PaladinPrototypePlayer.prefab are now configured with their own initial skin slot and visible Animator. Model FBXs, skin weights and discarded calibration are untouched. The setup menu ROM > Characters > Configure Warrior and Paladin Skins reassigns these known bindings; it preserves additional skin slots and unrelated open-stage edits without saving those edits.

## Quick start

1. Open WarriorPlayer or PaladinPrototypePlayer in Prefab Mode. Character Skin Controller is already assigned on the player root.
2. Expand Skins > Element 0. Definition selects WarriorSkin or PaladinSkin; ExistingAnimator and ExistingVisualRoot point to that character's visible model. BuiltInWeapon is the existing axe for Warrior and empty for the Paladin's instantiated sword. The Assign starter profile button is intended for a newly added component, not for replacing a customized list.
3. Expand Skins to assign more profiles and visual roots/prefabs. SkinId is the stable PlayerInfo identity; InitialIndex is a list index, not that identity.
4. In Play Mode, use the inspector's Use buttons to change the local presentation. SelectSkin(index) and SelectSkinId(id) are also public. Changes made through those methods are cosmetic/local; persistence and replication must use the game's existing PlayerInfo flow, which already calls WarriorAvatar.SetSkin.

Create profiles through Assets > Create > ROM > Characters > Skin. ROM > Characters > Create Starter Skin Profiles creates missing examples without overwriting existing ones.

## Shared controller

The starter WarriorAnimations and PaladinAnimations override controllers both reference WarriorLocomotion.controller. Movement, jump, forward roll, draw/sheath and standard action transitions remain common. Each override asset independently replaces clips. Keep the motor's existing parameter/state contract (MoveX, MoveY, Grounded, Dead, WeaponDrawn, DefensiveHold, Rolling, RollArmed, RollSpeed, AttackRate; Locomotion/UnarmedLocomotion and RollForward/UnarmedRollForward). Movement and damage remain owned by the existing motor/combat scripts, not by the skin.

An AnimatorOverrideController cannot add states or transitions. For a genuinely new flow, duplicate the common Animator Controller into a class-specific controller and retain its locomotion contract. Assign that controller or an override of it to the skin. Such a duplicate does not automatically inherit later edits to the original graph. Prefer a common optional action state and independent clips if the only difference is the animation.

## Actions

Each Actions entry maps an AbilityDefinition.Id to a full state path and layer. There is no slot-number requirement in the runtime skin lookup. TimingClip + SpeedParameter optionally matches the clip to the ability's CastDuration. State exit transitions remain authored in the Animator. Unknown/unmapped actions return false and do not fall back to a different class's attack. Starter mappings cover the current class definitions only; no new airborne ability, input, damage or movement has been invented.

For a Paladin-only aerial attack: author the state and its exit in the Paladin controller (or use a common optional action state); add an action with Id aerial-attack and that state path only to PaladinSkin. A presentation script can call:

```csharp
GetComponent<CharacterSkinController>().TryPlayAction("aerial-attack", 0.7f);
```

This call plays presentation only. Implement and authorize the actual ability in the server combat system if it needs damage, costs or cooldowns. Existing combat RPCs route learned abilities to the selected skin using their IDs.

## Weapons and rig data

DefaultWeapon is the initial presentation fallback, not a change to the player's combat loadout. Weapons entries match the original gameplay WeaponDefinition. Presentation optionally points at a separate visual definition calibrated to THIS skin: attachment offsets, hand/finger grips, per-clip IK and companion equipment. Combat still reads the original weapon stats. Controller optionally selects a skin-owned controller for that weapon combination; an empty value retains the skin controller. WeaponDefinition.Controller is used only by the legacy system when no skin is active.

WarriorSkin includes an independent sword/shield animation override copied from the existing sword controller. Default axe behavior and original weapon assets are retained. PaladinAnimations has an independent copy of the sword animation overrides. Its basic attack/Sacred Strike use sword clips rather than the QA pose animation. Healing Light and Prayer use a neutral placeholder pose because their class assets have no dedicated clip; Radiant Shield uses the block pose. PaladinSwordPresentation preserves the legacy hand attachment offsets with hand IK disabled and no experimental grip/finger calibration. These are presentation choices, not changes to damage, healing or defensive effects.

## Validation

Unity compilation and editor asset checks validate independent overrides sharing the base graph, unique action IDs and state/clip mappings for 13 current class actions. This is not a network gameplay/visual acceptance test. configuration-validation.txt records checks on the configured prefabs and their runtime binding path in isolated editor preview scenes. This does not repair the existing model's skinning and is not a network gameplay test.

Paladin model experiment cleanup: PaladinPracticeSword is restored to its original combat definition. PaladinSwordPresentation retains the skin's sword mount but its experimental hand/finger/per-animation IK entries are removed and hand IK is disabled pending a clean model. Shared skin/weapon preview support and animation profiles remain enabled. Model-rebuild work products were moved out of tracked documentation into Temp/paladin-model-cleanup/rebuild-work.
