# Warrior action rig

Every configured combat action now uses its own Animator state. The weapon grip
constraint evaluates after the clip, including special abilities. WarriorAvatar
no longer starts WarriorSpecialMotion's LateUpdate bone overrides.

| Gameplay slot | Action | Animator state | Pack clip |
| --- | --- | --- | --- |
| 0 | Autoattack | Attack0 | 2Hand-Sword-Attack1 |
| 1 | Furia cortante | Attack1 | 2Hand-Sword-Attack2 |
| 2 | Rompetierra | Attack2 | 2Hand-Sword-Attack3 |
| 3 | Rugido de honor | Attack3 | 2Hand-Sword-Attack4 |
| 4 | Salto de guerra | Attack4 | 2Hand-Sword-Attack5 |
| 5 | Torbellino | Attack5 | 2Hand-Sword-Attack6 |
| 6 | Última resistencia | Attack6 | 2Hand-Sword-Attack7 |
| 7 | Barrido | Attack7 | 2Hand-Sword-Attack8 |
| — | Prepared, no gameplay ability assigned | Attack8 | 2Hand-Sword-Attack9 |

Warrior.asset currently contains seven abilities plus the basic autoattack.
The hotbar supports seven ability slots. The ninth animation is prepared and
validated, but this change does not invent a ninth gameplay ability or keybinding.

AttackRate scales each clip to the existing ability CastDuration. Damage, hit
times, cooldowns and server-authoritative movement remain in AbilityController.
DefensiveHold preserves the Last Stand pose through its cast and active buff;
another action can interrupt the pose without removing the gameplay buff.

Preview all nine clips on WeaponIKController. The menu
`ROM > Animation Rigging > Validate Warrior Animation Grips` checks 61 poses per
clip, Animator transitions, defensive hold/release, and actual PresentAttack
routing for every configured gameplay action. Results: action-grip-validation.txt.
