using MultiFPS.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace MultiFPS.UI
{
    public class HudInventoryElement : MonoBehaviour
    {
        [SerializeField] Image _itemIcon;
        [SerializeField] Image _itemBackground;


        [SerializeField] Color _notInUsebackGroundColor;
        [SerializeField] Color _inUsebackGroundColor;

        [SerializeField] Text _itemIDtext;
        [SerializeField] Text _itemName;
        public Vector2 EmptySlotSize = new Vector2(250,54);
        [SerializeField] Color _inUseColor;
        [SerializeField] Color _notInUseColor;

        public void Draw(Item item, SlotType slotType, bool inUse, int slotID, SlotInput input)
        {
            gameObject.SetActive(slotType == SlotType.Normal || slotType == SlotType.PocketItem && item && item.CurrentAmmoSupply > 0);

            _itemIDtext.color = inUse ? _inUseColor : _notInUseColor;
            _itemIcon.color = inUse ? _inUseColor : _notInUseColor;

            _itemBackground.color = inUse ? _inUsebackGroundColor : _notInUsebackGroundColor;
            _itemIDtext.text = input.ToString()[2].ToString();
            if(_itemName)_itemName.text=item?item.ItemName:"VACÍO";
            if(_itemName){_itemName.gameObject.SetActive(true);_itemName.enabled=true;}
            _itemIcon.enabled=item!=null;



            if (!item)
            {
                transform.GetComponent<RectTransform>().sizeDelta = EmptySlotSize;
                _itemIcon.sprite = null;
                return;
            }


            _itemIcon.sprite = item.ItemIcon;

        }
    }
}
