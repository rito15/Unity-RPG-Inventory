using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 날짜 : 2021-03-27 PM 11:34:48
// 작성자 : Rito

namespace Rito.InventorySystem
{
    /// <summary> 셀 수 있는 아이템 데이터 </summary>
    public abstract class CountableItemData : ItemData
    {
        public int MaxAmount => _maxAmount;
        [SerializeField] private int _maxAmount = 99;
    }
}