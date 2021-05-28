using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 날짜 : 2021-03-21 PM 11:01:27
// 작성자 : Rito

namespace Rito.InventorySystem
{
    /// <summary> 수량을 셀 수 있는 아이템 </summary>
    public abstract class CountableItem : Item
    {
        public CountableItemData CountableData { get; private set; }

        /// <summary> 현재 아이템 개수 </summary>
        public int Amount { get; protected set; }

        /// <summary> 하나의 슬롯이 가질 수 있는 최대 개수(기본 99) </summary>
        public int MaxAmount => CountableData.MaxAmount;

        /// <summary> 수량이 가득 찼는지 여부 </summary>
        public bool IsMax => Amount >= CountableData.MaxAmount;

        /// <summary> 개수가 없는지 여부 </summary>
        public bool IsEmpty => Amount <= 0;


        public CountableItem(CountableItemData data, int amount = 1) : base(data)
        {
            CountableData = data;
            SetAmount(amount);
        }

        /// <summary> 개수 지정(범위 제한) </summary>
        public void SetAmount(int amount)
        {
            Amount = Mathf.Clamp(amount, 0, MaxAmount);
        }

        /// <summary> 개수 추가 및 최대치 초과량 반환(초과량 없을 경우 0) </summary>
        public int AddAmountAndGetExcess(int amount)
        {
            int nextAmount = Amount + amount;
            SetAmount(nextAmount);

            return (nextAmount > MaxAmount) ? (nextAmount - MaxAmount) : 0;
        }

        /// <summary> 개수를 나누어 복제 </summary>
        public CountableItem SeperateAndClone(int amount)
        {
            // 수량이 한개 이하일 경우, 복제 불가
            if(Amount <= 1) return null;

            if(amount > Amount - 1)
                amount = Amount - 1;

            Amount -= amount;
            return Clone(amount);
        }

        protected abstract CountableItem Clone(int amount);
    }
}