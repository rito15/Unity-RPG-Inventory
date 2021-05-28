using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 날짜 : 2021-03-07 PM 8:45:55
// 작성자 : Rito

namespace Rito.InventorySystem
{
    /*
        [상속 구조]

        ItemData(abstract)
            - CountableItemData(abstract)
                - PortionItemData
            - EquipmentItemData(abstract)
                - WeaponItemData
                - ArmorItemData

    */

    public abstract class ItemData : ScriptableObject
    {
        public int ID => _id;
        public string Name => _name;
        public string Tooltip => _tooltip;
        public Sprite IconSprite => _iconSprite;

        [SerializeField] private int      _id;
        [SerializeField] private string   _name;    // 아이템 이름
        [Multiline]
        [SerializeField] private string   _tooltip; // 아이템 설명
        [SerializeField] private Sprite   _iconSprite; // 아이템 아이콘
        [SerializeField] private GameObject _dropItemPrefab; // 바닥에 떨어질 때 생성할 프리팹

        /// <summary> 타입에 맞는 새로운 아이템 생성 </summary>
        public abstract Item CreateItem();
    }
}