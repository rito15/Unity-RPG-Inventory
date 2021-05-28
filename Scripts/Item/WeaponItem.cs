using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 날짜 : 2021-03-28 PM 11:02:03
// 작성자 : Rito

namespace Rito.InventorySystem
{
    /// <summary> 장비 - 무기 아이템 </summary>
    public class WeaponItem : EquipmentItem
    {
        public WeaponItem(WeaponItemData data) : base(data) { }
    }
}