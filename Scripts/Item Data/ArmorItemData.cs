using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 날짜 : 2021-03-28 PM 10:42:17
// 작성자 : Rito

namespace Rito.InventorySystem
{
    /// <summary> 장비 - 방어구 아이템 </summary>
    [CreateAssetMenu(fileName = "Item_Armor_", menuName = "Inventory System/Item Data/Armor", order = 2)]
    public class ArmorItemData : EquipmentItemData
    {
        /// <summary> 방어력 </summary>
        public int Defence => _defence;

        [SerializeField] private int _defence = 1;
        public override Item CreateItem()
        {
            return new ArmorItem(this);
        }
    }
}