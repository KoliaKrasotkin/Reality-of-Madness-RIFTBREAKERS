/// ---------------------------------------------
/// Copyright (c) Sculptiq Studios. All Rights Reserved.
/// https://www.sculptiqstudios.com/
/// ---------------------------------------------

namespace SculptiqStudios
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class InfernalArchmage : MonoBehaviour
    {
        private bool UseDirt = false;
        private bool UseBlood = false;
        private bool UseDecal = false;

        private bool Rotate = false;
        private float _Speed = 0;

        private Coroutine _Coroutine;

        private List<Material> DissolveMaterials = new List<Material>();

        [Header("----- Characters -----")]
        [SerializeField] private GameObject Character;
        [SerializeField] private Animator CharacterAnimator;
        [SerializeField] private float _RotateSpeed = 30;


        [Header("----- Weapon -----")]
        [SerializeField] private MeshRenderer WeaponL_MeshRenderer;
        [SerializeField] private GameObject[] WeaponL_VFX;
        [SerializeField] private MeshRenderer WeaponR_MeshRenderer;
        [SerializeField] private GameObject[] WeaponR_VFX;


        [Header("----- Skinned Mesh Renders -----")]
        [SerializeField] private SkinnedMeshRenderer Body;
        [SerializeField] private SkinnedMeshRenderer Horns_Head;
        [SerializeField] private SkinnedMeshRenderer Head_Stones;
        [SerializeField] private SkinnedMeshRenderer Armor_HandLegs;
        [SerializeField] private SkinnedMeshRenderer Armor_Shoulder;

        private int Selected_Skin = 0;
        private int Selected_Outfit = 0;
        private int Selected_Animation = 0;


        [Header("----- Materials -----")]
        [SerializeField] private Material[] MS_Body;
        [SerializeField] private Material[] MS_Face;
        [SerializeField] private Material[] MS_Horns;
        [SerializeField] private Material[] MS_Weapon;


        [Header("----- UI -----")]
        [SerializeField] private Slider Breathing_Slider;

        [SerializeField] private TextMeshProUGUI UI_SkinName;

        [SerializeField] private TextMeshProUGUI UI_OutfitName;

        [SerializeField] private TextMeshProUGUI UI_AnimationName;

        [SerializeField] private GameObject UI_Select_Dirt;
        [SerializeField] private GameObject UI_Select_Blood;
        [SerializeField] private GameObject UI_Select_Decal;




        private void Start()
        {
            Selected_Skin = 0;
            Selected_Outfit = 0;
            Selected_Animation = 0;

            UseDirt = false;
            UI_Select_Dirt.SetActive(false);

            UseBlood = false;
            UI_Select_Blood.SetActive(false);

            UseDecal = false;
            UI_Select_Decal.SetActive(false);

            Change_Skin();
            Change_Outfit();
            Change_Animation();
            Update_DissolveMaterials();
        }

        private void Update()
        {
            if (Rotate)
            {
                _Speed += _RotateSpeed * Time.deltaTime;
                Character.transform.rotation = Quaternion.Euler(0, _Speed, 0);
            }
        }




        public void Button_Rotate()
        {
            Rotate = !Rotate;

            _Speed = 0;
            Character.transform.rotation = Quaternion.Euler(0, _Speed, 0);
        }


        public void Button_Next_Skin()
        {
            Selected_Skin = Next(Selected_Skin, 2);
            Change_Skin();
        }
        public void Button_Prev_Skin()
        {
            Selected_Skin = Previous(Selected_Skin, 2);
            Change_Skin();
        }
        public void Button_Next_Outfit()
        {
            Selected_Outfit = Next(Selected_Outfit, 3);
            Change_Outfit();
        }
        public void Button_Prev_Outfit()
        {
            Selected_Outfit = Previous(Selected_Outfit, 3);
            Change_Outfit();
        }

        public void Button_Next_Animation()
        {
            Selected_Animation = Next(Selected_Animation, 11);
            Change_Animation();
        }
        public void Button_Prev_Animation()
        {
            Selected_Animation = Previous(Selected_Animation, 11);
            Change_Animation();
        }




        private void Change_Skin()
        {
            Material[] materials = Body.materials;

            materials[0] = MS_Body[Selected_Skin];
            materials[1] = MS_Face[Selected_Skin];

            Body.materials = materials;

            Horns_Head.material = MS_Horns[Selected_Skin];
            Head_Stones.material = MS_Horns[Selected_Skin];
            Armor_HandLegs.material = MS_Horns[Selected_Skin];
            Armor_Shoulder.material = MS_Horns[Selected_Skin];

            WeaponL_MeshRenderer.material = MS_Weapon[Selected_Skin];
            WeaponR_MeshRenderer.material = MS_Weapon[Selected_Skin];

            foreach (var _weapon_VFX in WeaponL_VFX)
            {
                _weapon_VFX.SetActive(false);
            }
            WeaponL_VFX[Selected_Skin].SetActive(true);

            foreach (var _weapon_VFX in WeaponR_VFX)
            {
                _weapon_VFX.SetActive(false);
            }
            WeaponR_VFX[Selected_Skin].SetActive(true);

            UI_SkinName.text = "Skin " + (Selected_Skin + 1).ToString();

            Update_Dirt();
            Update_Blood();
            Update_Decal();

            Update_DissolveMaterials();
        }

        private void Change_Outfit()
        {
            switch (Selected_Outfit)
            {
                case 0:
                    UI_OutfitName.text = "1";

                    Body.enabled = true;
                    Horns_Head.enabled = true;

                    Head_Stones.enabled = false;
                    Armor_HandLegs.enabled = false;
                    Armor_Shoulder.enabled = false;

                    break;
                case 1:
                    UI_OutfitName.text = "2";

                    Body.enabled = true;
                    Horns_Head.enabled = true;

                    Head_Stones.enabled = true;
                    Armor_HandLegs.enabled = false;
                    Armor_Shoulder.enabled = false;

                    break;
                case 2:
                    UI_OutfitName.text = "3";

                    Body.enabled = true;
                    Horns_Head.enabled = true;

                    Head_Stones.enabled = true;
                    Armor_HandLegs.enabled = true;
                    Armor_Shoulder.enabled = false;

                    break;
                case 3:
                    UI_OutfitName.text = "4";

                    Body.enabled = true;
                    Horns_Head.enabled = true;

                    Head_Stones.enabled = true;
                    Armor_HandLegs.enabled = true;
                    Armor_Shoulder.enabled = true;

                    break;
                default:
                    break;
            }
        }

        private void Change_Animation()
        {
            switch (Selected_Animation)
            {
                case 0:
                    UI_AnimationName.text = "Idle 1";
                    CharacterAnimator.Play("A_Idle01_HandsClosed");
                    WeaponL_MeshRenderer.gameObject.SetActive(true);
                    WeaponR_MeshRenderer.gameObject.SetActive(true);

                    break;
                case 1:
                    UI_AnimationName.text = "Idle 2";
                    CharacterAnimator.Play("A_Idle02_HandsClosed");
                    WeaponL_MeshRenderer.gameObject.SetActive(true);
                    WeaponR_MeshRenderer.gameObject.SetActive(true);

                    break;
                case 2:
                    UI_AnimationName.text = "Walk 1";
                    CharacterAnimator.Play("A_Walk01");
                    WeaponL_MeshRenderer.gameObject.SetActive(false);
                    WeaponR_MeshRenderer.gameObject.SetActive(false);

                    break;
                case 3:
                    UI_AnimationName.text = "Walk 2";
                    CharacterAnimator.Play("A_Walk02");
                    WeaponL_MeshRenderer.gameObject.SetActive(false);
                    WeaponR_MeshRenderer.gameObject.SetActive(false);

                    break;
                case 4:
                    UI_AnimationName.text = "Attack 1";
                    CharacterAnimator.Play("A_Attack01");
                    WeaponL_MeshRenderer.gameObject.SetActive(false);
                    WeaponR_MeshRenderer.gameObject.SetActive(false);

                    break;
                case 5:
                    UI_AnimationName.text = "Attack 2";
                    CharacterAnimator.Play("A_Attack02");
                    WeaponL_MeshRenderer.gameObject.SetActive(true);
                    WeaponR_MeshRenderer.gameObject.SetActive(true);

                    break;
                case 6:
                    UI_AnimationName.text = "Attack 3";
                    CharacterAnimator.Play("A_Attack03");
                    WeaponL_MeshRenderer.gameObject.SetActive(false);
                    WeaponR_MeshRenderer.gameObject.SetActive(false);

                    break;
                case 7:
                    UI_AnimationName.text = "Attack 4";
                    CharacterAnimator.Play("A_Attack04");
                    WeaponL_MeshRenderer.gameObject.SetActive(true);
                    WeaponR_MeshRenderer.gameObject.SetActive(true);

                    break;
                case 8:
                    UI_AnimationName.text = "Hit 1";
                    CharacterAnimator.Play("A_Hit01");
                    WeaponL_MeshRenderer.gameObject.SetActive(false);
                    WeaponR_MeshRenderer.gameObject.SetActive(false);

                    break;
                case 9:
                    UI_AnimationName.text = "Death 1";
                    CharacterAnimator.Play("A_Death01");
                    WeaponL_MeshRenderer.gameObject.SetActive(false);
                    WeaponR_MeshRenderer.gameObject.SetActive(false);

                    break;
                case 10:
                    UI_AnimationName.text = "Death 2";
                    CharacterAnimator.Play("A_Death02");
                    WeaponL_MeshRenderer.gameObject.SetActive(false);
                    WeaponR_MeshRenderer.gameObject.SetActive(false);

                    break;
                case 11:
                    UI_AnimationName.text = "Emote Dual Weapon Slash";
                    CharacterAnimator.Play("A_Emote_DualWeaponSlash");
                    WeaponL_MeshRenderer.gameObject.SetActive(true);
                    WeaponR_MeshRenderer.gameObject.SetActive(true);

                    break;
                default:
                    break;
            }
        }




        public void Button_Breathing_Blendshape()
        {
            Body.SetBlendShapeWeight(0, Breathing_Slider.value);
        }




        public void Button_Dirt()
        {
            UseDirt = !UseDirt;
            UI_Select_Dirt.SetActive(UseDirt);

            Update_Dirt();
        }

        public void Button_Blood()
        {
            UseBlood = !UseBlood;
            UI_Select_Blood.SetActive(UseBlood);

            Update_Blood();
        }

        public void Button_Decal()
        {
            UseDecal = !UseDecal;
            UI_Select_Decal.SetActive(UseDecal);

            Update_Decal();
        }

        private void Update_Dirt()
        {
            SetDirt(Body.materials[0]);
            SetDirt(Body.materials[1]);
            
            SetDirt(Horns_Head.material);
            SetDirt(Head_Stones.material);
            SetDirt(Armor_HandLegs.material);
            SetDirt(Armor_Shoulder.material);
        }
        private void Update_Blood()
        {
            SetBlood(Body.materials[0]);
            SetBlood(Body.materials[1]);
            
            SetBlood(Horns_Head.material);
            SetBlood(Head_Stones.material);
            SetBlood(Armor_HandLegs.material);
            SetBlood(Armor_Shoulder.material);
        }
        private void Update_Decal()
        {
            SetDecal(Body.materials[0]);
        }
        private void Update_DissolveMaterials()
        {
            DissolveMaterials.Clear();

            DissolveMaterials.Add(Body.materials[0]);
            DissolveMaterials.Add(Body.materials[1]);

            DissolveMaterials.Add(Horns_Head.material);
            DissolveMaterials.Add(Head_Stones.material);
            DissolveMaterials.Add(Armor_HandLegs.material);
            DissolveMaterials.Add(Armor_Shoulder.material);

            DissolveMaterials.Add(WeaponL_MeshRenderer.material);
            DissolveMaterials.Add(WeaponR_MeshRenderer.material);
        }




        //------------------------------ Common Functions ------------------------------//

        private int Next(int Number, int MaxValue)
        {
            Number++;
            if (Number > MaxValue)
                Number = 0;

            return Number;
        }
        private int Previous(int Number, int MaxValue)
        {
            Number--;
            if (Number < 0)
                Number = MaxValue;

            return Number;
        }

        private void SetDirt(Material mat)
        {
            if (UseDirt)
            {
                mat.EnableKeyword("_ENABLE_DIRT");
            }
            else
            {
                mat.DisableKeyword("_ENABLE_DIRT");
            }
        }
        private void SetBlood(Material mat)
        {
            if (UseBlood)
            {
                mat.EnableKeyword("_ENABLE_BLOOD");
            }
            else
            {
                mat.DisableKeyword("_ENABLE_BLOOD");
            }
        }
        private void SetDecal(Material mat)
        {
            if (UseDecal)
            {
                mat.EnableKeyword("_ENABLE_DECAL");
                mat.SetFloat("_Decal", 1);
            }
            else
            {
                mat.DisableKeyword("_ENABLE_DECAL");
                mat.SetFloat("_Decal", 0);
            }
        }

        public void Dissolve_In()
        {
            if (_Coroutine != null)
            {
                StopCoroutine(_Coroutine);
                _Coroutine = null;
            }

            _Coroutine = StartCoroutine(DissolveIn(2, -2, 0));
        }
        public void Dissolve_Out()
        {
            if (_Coroutine != null)
            {
                StopCoroutine(_Coroutine);
                _Coroutine = null;
            }

            _Coroutine = StartCoroutine(DissolveOut(2, 0, -2));
        }

        private IEnumerator DissolveOut(float _Time, float start, float end)
        {
            foreach (Material mat in DissolveMaterials)
            {
                mat.EnableKeyword("_ENABLE_DISSOLVE");
                mat.SetFloat("_AlphaClip", 1);
                mat.EnableKeyword("_ALPHATEST_ON");
            }



            float progress = 0f;

            while (progress < 1f)
            {
                progress += Time.deltaTime / _Time;
                float value = Mathf.Lerp(start, end, progress);

                foreach (Material mat in DissolveMaterials)
                {
                    mat.SetFloat("_Dissolve", value);
                }

                yield return null;
            }

            foreach (var _weapon_VFX in WeaponL_VFX)
            {
                _weapon_VFX.SetActive(false);
            }
            foreach (var _weapon_VFX in WeaponR_VFX)
            {
                _weapon_VFX.SetActive(false);
            }
        }

        private IEnumerator DissolveIn(float _Time, float start, float end)
        {
            WeaponL_VFX[Selected_Skin].SetActive(true);
            WeaponR_VFX[Selected_Skin].SetActive(true);

            float progress = 0f;

            while (progress < 1f)
            {
                progress += Time.deltaTime / _Time;
                float value = Mathf.Lerp(start, end, progress);

                foreach (Material mat in DissolveMaterials)
                {
                    mat.SetFloat("_Dissolve", value);
                }

                yield return null;
            }



            foreach (Material mat in DissolveMaterials)
            {
                mat.SetFloat("_AlphaClip", 0);
                mat.DisableKeyword("_ENABLE_DISSOLVE");
            }
        }
    }
}
