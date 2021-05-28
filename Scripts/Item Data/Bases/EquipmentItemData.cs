using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 날짜 : 2021-03-27 PM 11:07:41
// 작성자 : Rito

namespace Rito.InventorySystem
{
    /// <summary> 장비 아이템 </summary>
    public abstract class EquipmentItemData : ItemData
    {
        /// <summary> 최대 내구도 </summary>
        public int MaxDurability => _maxDurability;

        [SerializeField] private int _maxDurability = 100;
    }
}