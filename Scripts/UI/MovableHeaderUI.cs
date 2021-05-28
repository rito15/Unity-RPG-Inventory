using UnityEngine;
using UnityEngine.EventSystems;

// 날짜 : 2021-03-18 PM 9:05:42
// 작성자 : Rito

namespace Rito
{
    /// <summary> 헤더 드래그 앤 드롭에 의한 UI 이동 </summary>
    public class MovableHeaderUI : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField]
        private Transform _targetTr; // 이동될 UI

        private Vector2 _beginPoint;
        private Vector2 _moveBegin;

        private void Awake()
        {
            // 이동 대상 UI를 지정하지 않은 경우, 자동으로 부모로 초기화
            if(_targetTr == null)
                _targetTr = transform.parent;
        }

        // 드래그 시작 위치 지정
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            _beginPoint = _targetTr.position;
            _moveBegin = eventData.position;
        }

        // 드래그 : 마우스 커서 위치로 이동
        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            _targetTr.position = _beginPoint + (eventData.position - _moveBegin);
        }
    }
}