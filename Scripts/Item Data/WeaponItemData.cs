using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 날짜 : 2021-03-28 PM 10:38:33
// 작성자 : Rito

namespace Rito.InventorySystem
{
    /// <summary> 장비 - 무기 아이템 </summary>
    [CreateAssetMenu(fileName = "Item_Weapon_", menuName = "Inventory System/Item Data/Weaopn", order = 1)]
    public class WeaponItemData : EquipmentItemData
    {
        /// <summary> 공격력 </summary>
        public int Damage => _damage;

        [SerializeField] private int _damage = 1;

        public override Item CreateItem()
        {
            return new WeaponItem(this);
        }
    }
}