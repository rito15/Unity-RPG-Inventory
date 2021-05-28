using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 날짜 : 2021-03-07 PM 7:34:39
// 작성자 : Rito

namespace Rito.InventorySystem
{
    /*
        [상속 구조]
        Item : 기본 아이템
            - EquipmentItem : 장비 아이템
            - CountableItem : 수량이 존재하는 아이템
    */
    public abstract class Item
    {
        public ItemData Data { get; private set; }

        public Item(ItemData data) => Data = data;
    }
}