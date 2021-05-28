using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 날짜 : 2021-03-28 PM 10:55:00
// 작성자 : Rito

namespace Rito.InventorySystem
{
    /// <summary> 장비 아이템</summary>
    public abstract class EquipmentItem : Item
    {
        public EquipmentItemData EquipmentData { get; private set; }

        /// <summary> 현재 내구도 </summary>
        public int Durability
        {
            get => _durability;
            set
            {
                if(value < 0) value = 0;
                if(value > EquipmentData.MaxDurability)
                    value = EquipmentData.MaxDurability;

                _durability = value;
            }
        }
        private int _durability;

        public EquipmentItem(EquipmentItemData data) : base(data)
        {
            EquipmentData = data;
            Durability = data.MaxDurability;
        }

        // Item Data 외의 필드값에 대한 매개변수를 갖는 생성자는 추가로 제공하지 않음
        // 자식들에서 모두 추가해줘야 하므로 유지보수면에서 불편
    }
}