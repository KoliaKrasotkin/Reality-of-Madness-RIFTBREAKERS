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

    public class LittleDemon : MonoBehaviour
    {
        private bool UseDirt = false;
        private bool UseBlood = false;
        private bool UseDecal = false;

        private bool Rotate = false;
        private float _Speed = 0;

        private Vector3 minScale = new(0.75f, 0.75f, 0.75f);
        private Vector3 maxScale = new(1f, 1f, 1f);

        private Coroutine _Coroutine;
        private Coroutine _CoroutineTransform;

        private bool Transformed = false;

        private List<Material> DissolveMaterials = new List<Material>();

        [Header("----- Characters -----")]
        [SerializeField] private GameObject Character;
        [SerializeField] private Animator CharacterAnimator;
        [SerializeField] private float _RotateSpeed = 30;


        [Header("----- Skinned Mesh Renders -----")]
        [SerializeField] private SkinnedMeshRenderer Body;
        [SerializeField] private SkinnedMeshRenderer Eyebrows;
        [SerializeField] private SkinnedMeshRenderer Eyelashes;
        [SerializeField] private SkinnedMeshRenderer Hairs;
        [SerializeField] private SkinnedMeshRenderer Outfit_Hands;
        [SerializeField] private SkinnedMeshRenderer Outfit_Neck;
        [SerializeField] private SkinnedMeshRenderer Outfit_Pants;
        [SerializeField] private SkinnedMeshRenderer Outfit_Pouch;

        private int Selected_Outfit = 0;
        private int Selected_Animation = 0;


        [Header("----- UI -----")]
        [SerializeField] private TextMeshProUGUI UI_OutfitName;
        [SerializeField] private TextMeshProUGUI UI_AnimationName;

        [SerializeField] private GameObject UI_Select_Dirt;
        [SerializeField] private GameObject UI_Select_Blood;
        [SerializeField] private GameObject UI_Select_Decal;


        private void Start()
        {
            Selected_Outfit = 0;
            Selected_Animation = 0;

            UseDirt = false;
            UI_Select_Dirt.SetActive(false);

            UseBlood = false;
            UI_Select_Blood.SetActive(false);

            UseDecal = false;
            UI_Select_Decal.SetActive(false);


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


        public void Button_Next_Outfit()
        {
            Selected_Outfit = Next(Selected_Outfit, 7);
            Change_Outfit();
        }
        public void Button_Prev_Outfit()
        {
            Selected_Outfit = Previous(Selected_Outfit, 7);
            Change_Outfit();
        }

        public void Button_Next_Animation()
        {
            Selected_Animation = Next(Selected_Animation, 8);
            Change_Animation();
        }
        public void Button_Prev_Animation()
        {
            Selected_Animation = Previous(Selected_Animation, 8);
            Change_Animation();
        }




        private void Change_Outfit()
        {
            switch (Selected_Outfit)
            {
                case 0:
                    UI_OutfitName.text = "Outfit 1";

                    Outfit_Hands.enabled = false;
                    Outfit_Neck.enabled = false;
                    Outfit_Pouch.enabled = false;

                    break;
                case 1:
                    UI_OutfitName.text = "Outfit 2";

                    Outfit_Hands.enabled = true;
                    Outfit_Neck.enabled = false;
                    Outfit_Pouch.enabled = false;

                    break;
                case 2:
                    UI_OutfitName.text = "Outfit 3";

                    Outfit_Hands.enabled = false;
                    Outfit_Neck.enabled = true;
                    Outfit_Pouch.enabled = false;

                    break;
                case 3:
                    UI_OutfitName.text = "Outfit 4";

                    Outfit_Hands.enabled = false;
                    Outfit_Neck.enabled = false;
                    Outfit_Pouch.enabled = true;

                    break;
                case 4:
                    UI_OutfitName.text = "Outfit 5";

                    Outfit_Hands.enabled = true;
                    Outfit_Neck.enabled = true;
                    Outfit_Pouch.enabled = false;

                    break;
                case 5:
                    UI_OutfitName.text = "Outfit 6";

                    Outfit_Hands.enabled = true;
                    Outfit_Neck.enabled = false;
                    Outfit_Pouch.enabled = true;

                    break;
                case 6:
                    UI_OutfitName.text = "Outfit 7";

                    Outfit_Hands.enabled = false;
                    Outfit_Neck.enabled = true;
                    Outfit_Pouch.enabled = true;

                    break;
                case 7:
                    UI_OutfitName.text = "Outfit 8";

                    Outfit_Hands.enabled = true;
                    Outfit_Neck.enabled = true;
                    Outfit_Pouch.enabled = true;

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
                    CharacterAnimator.Play("Idle01");

                    break;
                case 1:
                    UI_AnimationName.text = "Idle 2";
                    CharacterAnimator.Play("Idle02");

                    break;
                case 2:
                    UI_AnimationName.text = "Walk 1";
                    CharacterAnimator.Play("Walk01");

                    break;
                case 3:
                    UI_AnimationName.text = "Attack 1";
                    CharacterAnimator.Play("Attack01");

                    break;
                case 4:
                    UI_AnimationName.text = "Attack 2";
                    CharacterAnimator.Play("Attack02");

                    break;
                case 5:
                    UI_AnimationName.text = "Attack 3";
                    CharacterAnimator.Play("Attack03");

                    break;
                case 6:
                    UI_AnimationName.text = "Hit 1";
                    CharacterAnimator.Play("Hit01");

                    break;
                case 7:
                    UI_AnimationName.text = "Death 1";
                    CharacterAnimator.Play("Death01");

                    break;
                case 8:
                    UI_AnimationName.text = "Death 2";
                    CharacterAnimator.Play("Death02");

                    break;
                default:
                    break;
            }
        }




        public void Button_Transform()
        {
            Transformed = !Transformed;

            if (_CoroutineTransform != null)
            {
                StopCoroutine(_CoroutineTransform);
                _CoroutineTransform = null;
            }

            if (Transformed)
            {
                _CoroutineTransform = StartCoroutine(Transform_Character(0, 1));
            }
            else
            {
                _CoroutineTransform = StartCoroutine(Transform_Character(1, 0));
            }
        }
        private IEnumerator Transform_Character(float start, float end)
        {
            float progress = 0f;

            CharacterAnimator.Play("Transform");

            yield return new WaitForSeconds(0.4f);

            float _Time = 1.85f - 0.4f; //Time Left to finish the animation

            while (progress < 1f)
            {
                progress += Time.deltaTime / _Time;
                float value = Mathf.Lerp(start, end, progress);

                Body.SetBlendShapeWeight(0, value * 100);
                Hairs.SetBlendShapeWeight(0, value * 100);
                Outfit_Hands.SetBlendShapeWeight(0, value * 100);
                Outfit_Neck.SetBlendShapeWeight(0, value * 100);
                Outfit_Pants.SetBlendShapeWeight(0, value * 100);
                Outfit_Pouch.SetBlendShapeWeight(0, value * 100);


                Body.materials[0].SetFloat("_Transform", value);
                Body.materials[1].SetFloat("_Transform", value);
                Body.materials[2].SetFloat("_Transform", value);

                Outfit_Hands.material.SetFloat("_Transform", value);
                Outfit_Neck.material.SetFloat("_Transform", value);
                Outfit_Pants.material.SetFloat("_Transform", value);
                Outfit_Pouch.material.SetFloat("_Transform", value);

                CharacterAnimator.transform.localScale = Vector3.Lerp(minScale, maxScale, value);

                yield return null;
            }
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

            SetDirt(Outfit_Hands.material);
            SetDirt(Outfit_Neck.material);
            SetDirt(Outfit_Pants.material);
            SetDirt(Outfit_Pouch.material);
        }
        private void Update_Blood()
        {
            SetBlood(Body.materials[0]);

            SetBlood(Outfit_Hands.material);
            SetBlood(Outfit_Neck.material);
            SetBlood(Outfit_Pants.material);
            SetBlood(Outfit_Pouch.material);
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
            DissolveMaterials.Add(Body.materials[2]);

            DissolveMaterials.Add(Eyebrows.material);
            DissolveMaterials.Add(Eyelashes.material);
            DissolveMaterials.Add(Hairs.material);

            DissolveMaterials.Add(Outfit_Hands.material);
            DissolveMaterials.Add(Outfit_Neck.material);
            DissolveMaterials.Add(Outfit_Pants.material);
            DissolveMaterials.Add(Outfit_Pouch.material);
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

        public void SetDirt(Material mat)
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
        public void SetBlood(Material mat)
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
        public void SetDecal(Material mat)
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
        }

        private IEnumerator DissolveIn(float _Time, float start, float end)
        {
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
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.SetFloat("_AlphaClip", 0);
                mat.DisableKeyword("_ENABLE_DISSOLVE");
            }
        }
    }
}
