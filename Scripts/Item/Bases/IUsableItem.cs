using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 날짜 : 2021-04-15 PM 2:59:59
// 작성자 : Rito

namespace Rito.InventorySystem
{
    /// <summary> 사용 가능한 아이템(착용/소모) </summary>
    public interface IUsableItem
    {
        /// <summary> 아이템 사용하기(사용 성공 여부 리턴) </summary>
        bool Use();
    }
}