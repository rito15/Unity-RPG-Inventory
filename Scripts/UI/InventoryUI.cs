using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/*
    [기능 - 에디터 전용]
    - 게임 시작 시 동적으로 생성될 슬롯 미리보기(개수, 크기 미리보기 가능)

    [기능 - 유저 인터페이스]
    - 슬롯에 마우스 올리기
      - 사용 가능 슬롯 : 하이라이트 이미지 표시
      - 아이템 존재 슬롯 : 아이템 정보 툴팁 표시

    - 드래그 앤 드롭
      - 아이템 존재 슬롯 -> 아이템 존재 슬롯 : 두 아이템 위치 교환
      - 아이템 존재 슬롯 -> 아이템 미존재 슬롯 : 아이템 위치 변경
        - Shift 또는 Ctrl 누른 상태일 경우 : 셀 수 있는 아이템 수량 나누기
      - 아이템 존재 슬롯 -> UI 바깥 : 아이템 버리기

    - 슬롯 우클릭
      - 사용 가능한 아이템일 경우 : 아이템 사용

    - 기능 버튼(좌측 상단)
      - Trim : 앞에서부터 빈 칸 없이 아이템 채우기
      - Sort : 정해진 가중치대로 아이템 정렬

    - 필터 버튼(우측 상단)
      - [A] : 모든 아이템 필터링
      - [E] : 장비 아이템 필터링
      - [P] : 소비 아이템 필터링

      * 필터링에서 제외된 아이템 슬롯들은 조작 불가

    [기능 - 기타]
    - InvertMouse(bool) : 마우스 좌클릭/우클릭 반전 여부 설정
*/

// 날짜 : 2021-03-07 PM 7:34:31
// 작성자 : Rito

namespace Rito.InventorySystem
{
    public class InventoryUI : MonoBehaviour
    {
        /***********************************************************************
        *                               Option Fields
        ***********************************************************************/
        #region .
        [Header("Options")]
        [Range(0, 10)]
        [SerializeField] private int _horizontalSlotCount = 8;  // 슬롯 가로 개수
        [Range(0, 10)]
        [SerializeField] private int _verticalSlotCount = 8;      // 슬롯 세로 개수
        [SerializeField] private float _slotMargin = 8f;          // 한 슬롯의 상하좌우 여백
        [SerializeField] private float _contentAreaPadding = 20f; // 인벤토리 영역의 내부 여백
        [Range(32, 64)]
        [SerializeField] private float _slotSize = 64f;      // 각 슬롯의 크기

        [Space]
        [SerializeField] private bool _showTooltip = true;
        [SerializeField] private bool _showHighlight = true;
        [SerializeField] private bool _showRemovingPopup = true;

        [Header("Connected Objects")]
        [SerializeField] private RectTransform _contentAreaRT; // 슬롯들이 위치할 영역
        [SerializeField] private GameObject _slotUiPrefab;     // 슬롯의 원본 프리팹
        [SerializeField] private ItemTooltipUI _itemTooltip;   // 아이템 정보를 보여줄 툴팁 UI
        [SerializeField] private InventoryPopupUI _popup;      // 팝업 UI 관리 객체

        [Header("Buttons")]
        [SerializeField] private Button _trimButton;
        [SerializeField] private Button _sortButton;

        [Header("Filter Toggles")]
        [SerializeField] private Toggle _toggleFilterAll;
        [SerializeField] private Toggle _toggleFilterEquipments;
        [SerializeField] private Toggle _toggleFilterPortions;

        [Space(16)]
        [SerializeField] private bool _mouseReversed = false; // 마우스 클릭 반전 여부

        #endregion
        /***********************************************************************
        *                               Private Fields
        ***********************************************************************/
        #region .

        /// <summary> 연결된 인벤토리 </summary>
        private Inventory _inventory;

        private List<ItemSlotUI> _slotUIList = new List<ItemSlotUI>();
        private GraphicRaycaster _gr;
        private PointerEventData _ped;
        private List<RaycastResult> _rrList;

        private ItemSlotUI _pointerOverSlot; // 현재 포인터가 위치한 곳의 슬롯
        private ItemSlotUI _beginDragSlot; // 현재 드래그를 시작한 슬롯
        private Transform _beginDragIconTransform; // 해당 슬롯의 아이콘 트랜스폼

        private int _leftClick = 0;
        private int _rightClick = 1;

        private Vector3 _beginDragIconPoint;   // 드래그 시작 시 슬롯의 위치
        private Vector3 _beginDragCursorPoint; // 드래그 시작 시 커서의 위치
        private int _beginDragSlotSiblingIndex;
        
        /// <summary> 인벤토리 UI 내 아이템 필터링 옵션 </summary>
        private enum FilterOption
        {
            All, Equipment, Portion
        }
        private FilterOption _currentFilterOption = FilterOption.All;

        #endregion
        /***********************************************************************
        *                               Unity Events
        ***********************************************************************/
        #region .
        private void Awake()
        {
            Init();
            InitSlots();
            InitButtonEvents();
            InitToggleEvents();
        }

        private void Update()
        {
            _ped.position = Input.mousePosition;

            OnPointerEnterAndExit();
            if(_showTooltip) ShowOrHideItemTooltip();
            OnPointerDown();
            OnPointerDrag();
            OnPointerUp();
        }

        #endregion
        /***********************************************************************
        *                               Init Methods
        ***********************************************************************/
        #region .
        private void Init()
        {
            TryGetComponent(out _gr);
            if (_gr == null)
                _gr = gameObject.AddComponent<GraphicRaycaster>();

            // Graphic Raycaster
            _ped = new PointerEventData(EventSystem.current);
            _rrList = new List<RaycastResult>(10);

            // Item Tooltip UI
            if (_itemTooltip == null)
            {
                _itemTooltip = GetComponentInChildren<ItemTooltipUI>();
                EditorLog("인스펙터에서 아이템 툴팁 UI를 직접 지정하지 않아 자식에서 발견하여 초기화하였습니다.");
            }
        }

        /// <summary> 지정된 개수만큼 슬롯 영역 내에 슬롯들 동적 생성 </summary>
        private void InitSlots()
        {
            // 슬롯 프리팹 설정
            _slotUiPrefab.TryGetComponent(out RectTransform slotRect);
            slotRect.sizeDelta = new Vector2(_slotSize, _slotSize);

            _slotUiPrefab.TryGetComponent(out ItemSlotUI itemSlot);
            if (itemSlot == null)
                _slotUiPrefab.AddComponent<ItemSlotUI>();

            _slotUiPrefab.SetActive(false);

            // --
            Vector2 beginPos = new Vector2(_contentAreaPadding, -_contentAreaPadding);
            Vector2 curPos = beginPos;

            _slotUIList = new List<ItemSlotUI>(_verticalSlotCount * _horizontalSlotCount);

            // 슬롯들 동적 생성
            for (int j = 0; j < _verticalSlotCount; j++)
            {
                for (int i = 0; i < _horizontalSlotCount; i++)
                {
                    int slotIndex = (_horizontalSlotCount * j) + i;

                    var slotRT = CloneSlot();
                    slotRT.pivot = new Vector2(0f, 1f); // Left Top
                    slotRT.anchoredPosition = curPos;
                    slotRT.gameObject.SetActive(true);
                    slotRT.gameObject.name = $"Item Slot [{slotIndex}]";

                    var slotUI = slotRT.GetComponent<ItemSlotUI>();
                    slotUI.SetSlotIndex(slotIndex);
                    _slotUIList.Add(slotUI);

                    // Next X
                    curPos.x += (_slotMargin + _slotSize);
                }

                // Next Line
                curPos.x = beginPos.x;
                curPos.y -= (_slotMargin + _slotSize);
            }

            // 슬롯 프리팹 - 프리팹이 아닌 경우 파괴
            if(_slotUiPrefab.scene.rootCount != 0)
                Destroy(_slotUiPrefab);

            // -- Local Method --
            RectTransform CloneSlot()
            {
                GameObject slotGo = Instantiate(_slotUiPrefab);
                RectTransform rt = slotGo.GetComponent<RectTransform>();
                rt.SetParent(_contentAreaRT);

                return rt;
            }
        }

        private void InitButtonEvents()
        {
            _trimButton.onClick.AddListener(() => _inventory.TrimAll());
            _sortButton.onClick.AddListener(() => _inventory.SortAll());
        }

        private void InitToggleEvents()
        {
            _toggleFilterAll.onValueChanged.AddListener(       flag => UpdateFilter(flag, FilterOption.All));
            _toggleFilterEquipments.onValueChanged.AddListener(flag => UpdateFilter(flag, FilterOption.Equipment));
            _toggleFilterPortions.onValueChanged.AddListener(  flag => UpdateFilter(flag, FilterOption.Portion));

            // Local Method
            void UpdateFilter(bool flag, FilterOption option)
            {
                if (flag)
                {
                    _currentFilterOption = option;
                    UpdateAllSlotFilters();
                }
            }
        }

        #endregion
        /***********************************************************************
        *                               Mouse Event Methods
        ***********************************************************************/
        #region .
        private bool IsOverUI()
            => EventSystem.current.IsPointerOverGameObject();

        /// <summary> 레이캐스트하여 얻은 첫 번째 UI에서 컴포넌트 찾아 리턴 </summary>
        private T RaycastAndGetFirstComponent<T>() where T : Component
        {
            _rrList.Clear();

            _gr.Raycast(_ped, _rrList);
            
            if(_rrList.Count == 0)
                return null;

            return _rrList[0].gameObject.GetComponent<T>();
        }
        /// <summary> 슬롯에 포인터가 올라가는 경우, 슬롯에서 포인터가 빠져나가는 경우 </summary>
        private void OnPointerEnterAndExit()
        {
            // 이전 프레임의 슬롯
            var prevSlot = _pointerOverSlot;

            // 현재 프레임의 슬롯
            var curSlot = _pointerOverSlot = RaycastAndGetFirstComponent<ItemSlotUI>();

            if (prevSlot == null)
            {
                // Enter
                if (curSlot != null)
                {
                    OnCurrentEnter();
                }
            }
            else
            {
                // Exit
                if (curSlot == null)
                {
                    OnPrevExit();
                }

                // Change
                else if (prevSlot != curSlot)
                {
                    OnPrevExit();
                    OnCurrentEnter();
                }
            }

            // ===================== Local Methods ===============================
            void OnCurrentEnter()
            {
                if(_showHighlight)
                    curSlot.Highlight(true);
            }
            void OnPrevExit()
            {
                prevSlot.Highlight(false);
            }
        }
        /// <summary> 아이템 정보 툴팁 보여주거나 감추기 </summary>
        private void ShowOrHideItemTooltip()
        {
            // 마우스가 유효한 아이템 아이콘 위에 올라와 있다면 툴팁 보여주기
            bool isValid =
                _pointerOverSlot != null && _pointerOverSlot.HasItem && _pointerOverSlot.IsAccessible
                && (_pointerOverSlot != _beginDragSlot); // 드래그 시작한 슬롯이면 보여주지 않기

            if (isValid)
            {
                UpdateTooltipUI(_pointerOverSlot);
                _itemTooltip.Show();
            }
            else
                _itemTooltip.Hide();
        }
        /// <summary> 슬롯에 클릭하는 경우 </summary>
        private void OnPointerDown()
        {
            // Left Click : Begin Drag
            if (Input.GetMouseButtonDown(_leftClick))
            {
                _beginDragSlot = RaycastAndGetFirstComponent<ItemSlotUI>();

                // 아이템을 갖고 있는 슬롯만 해당
                if (_beginDragSlot != null && _beginDragSlot.HasItem && _beginDragSlot.IsAccessible)
                {
                    EditorLog($"Drag Begin : Slot [{_beginDragSlot.Index}]");

                    // 위치 기억, 참조 등록
                    _beginDragIconTransform = _beginDragSlot.IconRect.transform;
                    _beginDragIconPoint = _beginDragIconTransform.position;
                    _beginDragCursorPoint = Input.mousePosition;

                    // 맨 위에 보이기
                    _beginDragSlotSiblingIndex = _beginDragSlot.transform.GetSiblingIndex();
                    _beginDragSlot.transform.SetAsLastSibling();

                    // 해당 슬롯의 하이라이트 이미지를 아이콘보다 뒤에 위치시키기
                    _beginDragSlot.SetHighlightOnTop(false);
                }
                else
                {
                    _beginDragSlot = null;
                }
            }

            // Right Click : Use Item
            else if (Input.GetMouseButtonDown(_rightClick))
            {
                ItemSlotUI slot = RaycastAndGetFirstComponent<ItemSlotUI>();

                if (slot != null && slot.HasItem && slot.IsAccessible)
                {
                    TryUseItem(slot.Index);
                }
            }
        }
        /// <summary> 드래그하는 도중 </summary>
        private void OnPointerDrag()
        {
            if(_beginDragSlot == null) return;

            if (Input.GetMouseButton(_leftClick))
            {
                // 위치 이동
                _beginDragIconTransform.position =
                    _beginDragIconPoint + (Input.mousePosition - _beginDragCursorPoint);
            }
        }
        /// <summary> 클릭을 뗄 경우 </summary>
        private void OnPointerUp()
        {
            if (Input.GetMouseButtonUp(_leftClick))
            {
                // End Drag
                if (_beginDragSlot != null)
                {
                    // 위치 복원
                    _beginDragIconTransform.position = _beginDragIconPoint;

                    // UI 순서 복원
                    _beginDragSlot.transform.SetSiblingIndex(_beginDragSlotSiblingIndex);

                    // 드래그 완료 처리
                    EndDrag();

                    // 해당 슬롯의 하이라이트 이미지를 아이콘보다 앞에 위치시키기
                    _beginDragSlot.SetHighlightOnTop(true);

                    // 참조 제거
                    _beginDragSlot = null;
                    _beginDragIconTransform = null;
                }
            }
        }

        private void EndDrag()
        {
            ItemSlotUI endDragSlot = RaycastAndGetFirstComponent<ItemSlotUI>();

            // 아이템 슬롯끼리 아이콘 교환 또는 이동
            if (endDragSlot != null && endDragSlot.IsAccessible)
            {
                // 수량 나누기 조건
                // 1) 마우스 클릭 떼는 순간 좌측 Ctrl 또는 Shift 키 유지
                // 2) begin : 셀 수 있는 아이템 / end : 비어있는 슬롯
                // 3) begin 아이템의 수량 > 1
                bool isSeparatable = 
                    (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftShift)) &&
                    (_inventory.IsCountableItem(_beginDragSlot.Index) && !_inventory.HasItem(endDragSlot.Index));

                // true : 수량 나누기, false : 교환 또는 이동
                bool isSeparation = false;
                int currentAmount = 0;

                // 현재 개수 확인
                if (isSeparatable)
                {
                    currentAmount = _inventory.GetCurrentAmount(_beginDragSlot.Index);
                    if (currentAmount > 1)
                    {
                        isSeparation = true;
                    }
                }

                // 1. 개수 나누기
                if(isSeparation)
                    TrySeparateAmount(_beginDragSlot.Index, endDragSlot.Index, currentAmount);
                // 2. 교환 또는 이동
                else
                    TrySwapItems(_beginDragSlot, endDragSlot);

                // 툴팁 갱신
                UpdateTooltipUI(endDragSlot);
                return;
            }

            // 버리기(커서가 UI 레이캐스트 타겟 위에 있지 않은 경우)
            if (!IsOverUI())
            {
                // 확인 팝업 띄우고 콜백 위임
                int index = _beginDragSlot.Index;
                string itemName = _inventory.GetItemName(index);
                int amount = _inventory.GetCurrentAmount(index);

                // 셀 수 있는 아이템의 경우, 수량 표시
                if(amount > 1)
                    itemName += $" x{amount}";

                if(_showRemovingPopup)
                    _popup.OpenConfirmationPopup(() => TryRemoveItem(index), itemName);
                else
                    TryRemoveItem(index);
            }
            // 슬롯이 아닌 다른 UI 위에 놓은 경우
            else
            {
                EditorLog($"Drag End(Do Nothing)");
            }
        }

        #endregion
        /***********************************************************************
        *                               Private Methods
        ***********************************************************************/
        #region .

        /// <summary> UI 및 인벤토리에서 아이템 제거 </summary>
        private void TryRemoveItem(int index)
        {
            EditorLog($"UI - Try Remove Item : Slot [{index}]");

            _inventory.Remove(index);
        }

        /// <summary> 아이템 사용 </summary>
        private void TryUseItem(int index)
        {
            EditorLog($"UI - Try Use Item : Slot [{index}]");

            _inventory.Use(index);
        }

        /// <summary> 두 슬롯의 아이템 교환 </summary>
        private void TrySwapItems(ItemSlotUI from, ItemSlotUI to)
        {
            if (from == to)
            {
                EditorLog($"UI - Try Swap Items: Same Slot [{from.Index}]");
                return;
            }

            EditorLog($"UI - Try Swap Items: Slot [{from.Index} -> {to.Index}]");

            from.SwapOrMoveIcon(to);
            _inventory.Swap(from.Index, to.Index);
        }

        /// <summary> 셀 수 있는 아이템 개수 나누기 </summary>
        private void TrySeparateAmount(int indexA, int indexB, int amount)
        {
            if (indexA == indexB)
            {
                EditorLog($"UI - Try Separate Amount: Same Slot [{indexA}]");
                return;
            }

            EditorLog($"UI - Try Separate Amount: Slot [{indexA} -> {indexB}]");

            string itemName = $"{_inventory.GetItemName(indexA)} x{amount}";

            _popup.OpenAmountInputPopup(
                amt => _inventory.SeparateAmount(indexA, indexB, amt),
                amount, itemName
            );
        }

        /// <summary> 툴팁 UI의 슬롯 데이터 갱신 </summary>
        private void UpdateTooltipUI(ItemSlotUI slot)
        {
            if(!slot.IsAccessible || !slot.HasItem)
                return;

            // 툴팁 정보 갱신
            _itemTooltip.SetItemInfo(_inventory.GetItemData(slot.Index));

            // 툴팁 위치 조정
            _itemTooltip.SetRectPosition(slot.SlotRect);
        }

        #endregion
        /***********************************************************************
        *                               Public Methods
        ***********************************************************************/
        #region .

        /// <summary> 인벤토리 참조 등록 (인벤토리에서 직접 호출) </summary>
        public void SetInventoryReference(Inventory inventory)
        {
            _inventory = inventory;
        }

        /// <summary> 마우스 클릭 좌우 반전시키기 (true : 반전) </summary>
        public void InvertMouse(bool value)
        {
            _leftClick = value ? 1 : 0;
            _rightClick = value ? 0 : 1;

            _mouseReversed = value;
        }

        /// <summary> 슬롯에 아이템 아이콘 등록 </summary>
        public void SetItemIcon(int index, Sprite icon)
        {
            EditorLog($"Set Item Icon : Slot [{index}]");

            _slotUIList[index].SetItem(icon);
        }

        /// <summary> 해당 슬롯의 아이템 개수 텍스트 지정 </summary>
        public void SetItemAmountText(int index, int amount)
        {
            EditorLog($"Set Item Amount Text : Slot [{index}], Amount [{amount}]");

            // NOTE : amount가 1 이하일 경우 텍스트 미표시
            _slotUIList[index].SetItemAmount(amount);
        }

        /// <summary> 해당 슬롯의 아이템 개수 텍스트 지정 </summary>
        public void HideItemAmountText(int index)
        {
            EditorLog($"Hide Item Amount Text : Slot [{index}]");

            _slotUIList[index].SetItemAmount(1);
        }

        /// <summary> 슬롯에서 아이템 아이콘 제거, 개수 텍스트 숨기기 </summary>
        public void RemoveItem(int index)
        {
            EditorLog($"Remove Item : Slot [{index}]");

            _slotUIList[index].RemoveItem();
        }

        /// <summary> 접근 가능한 슬롯 범위 설정 </summary>
        public void SetAccessibleSlotRange(int accessibleSlotCount)
        {
            for (int i = 0; i < _slotUIList.Count; i++)
            {
                _slotUIList[i].SetSlotAccessibleState(i < accessibleSlotCount);
            }
        }

        /// <summary> 특정 슬롯의 필터 상태 업데이트 </summary>
        public void UpdateSlotFilterState(int index, ItemData itemData)
        {
            bool isFiltered = true;

            // null인 슬롯은 타입 검사 없이 필터 활성화
            if(itemData != null)
                switch (_currentFilterOption)
                {
                    case FilterOption.Equipment:
                        isFiltered = (itemData is EquipmentItemData);
                        break;

                    case FilterOption.Portion:
                        isFiltered = (itemData is PortionItemData);
                        break;
                }

            _slotUIList[index].SetItemAccessibleState(isFiltered);
        }

        /// <summary> 모든 슬롯 필터 상태 업데이트 </summary>
        public void UpdateAllSlotFilters()
        {
            int capacity = _inventory.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                ItemData data = _inventory.GetItemData(i);
                UpdateSlotFilterState(i, data);
            }
        }

        #endregion
        /***********************************************************************
        *                               Editor Only Debug
        ***********************************************************************/
        #region .
#if UNITY_EDITOR
        [Header("Editor Options")]
        [SerializeField] private bool _showDebug = true;
#endif
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void EditorLog(object message)
        {
            if (!_showDebug) return;
            UnityEngine.Debug.Log($"[InventoryUI] {message}");
        }

        #endregion
        /***********************************************************************
        *                               Editor Preview
        ***********************************************************************/
        #region .
#if UNITY_EDITOR
        [SerializeField] private bool __showPreview = false;

        [Range(0.01f, 1f)]
        [SerializeField] private float __previewAlpha = 0.1f;

        private List<GameObject> __previewSlotGoList = new List<GameObject>();
        private int __prevSlotCountPerLine;
        private int __prevSlotLineCount;
        private float __prevSlotSize;
        private float __prevSlotMargin;
        private float __prevContentPadding;
        private float __prevAlpha;
        private bool __prevShow = false;
        private bool __prevMouseReversed = false;

        private void OnValidate()
        {
            if (__prevMouseReversed != _mouseReversed)
            {
                __prevMouseReversed = _mouseReversed;
                InvertMouse(_mouseReversed);

                EditorLog($"Mouse Reversed : {_mouseReversed}");
            }

            if (Application.isPlaying) return;

            if (__showPreview && !__prevShow)
            {
                CreateSlots();
            }
            __prevShow = __showPreview;

            if (Unavailable())
            {
                ClearAll();
                return;
            }
            if (CountChanged())
            {
                ClearAll();
                CreateSlots();
                __prevSlotCountPerLine = _horizontalSlotCount;
                __prevSlotLineCount = _verticalSlotCount;
            }
            if (ValueChanged())
            {
                DrawGrid();
                __prevSlotSize = _slotSize;
                __prevSlotMargin = _slotMargin;
                __prevContentPadding = _contentAreaPadding;
            }
            if (AlphaChanged())
            {
                SetImageAlpha();
                __prevAlpha = __previewAlpha;
            }

            bool Unavailable()
            {
                return !__showPreview ||
                        _horizontalSlotCount < 1 ||
                        _verticalSlotCount < 1 ||
                        _slotSize <= 0f ||
                        _contentAreaRT == null ||
                        _slotUiPrefab == null;
            }
            bool CountChanged()
            {
                return _horizontalSlotCount != __prevSlotCountPerLine ||
                       _verticalSlotCount != __prevSlotLineCount;
            }
            bool ValueChanged()
            {
                return _slotSize != __prevSlotSize ||
                       _slotMargin != __prevSlotMargin ||
                       _contentAreaPadding != __prevContentPadding;
            }
            bool AlphaChanged()
            {
                return __previewAlpha != __prevAlpha;
            }
            void ClearAll()
            {
                foreach (var go in __previewSlotGoList)
                {
                    Destroyer.Destroy(go);
                }
                __previewSlotGoList.Clear();
            }
            void CreateSlots()
            {
                int count = _horizontalSlotCount * _verticalSlotCount;
                __previewSlotGoList.Capacity = count;

                // 슬롯의 피벗은 Left Top으로 고정
                RectTransform slotPrefabRT = _slotUiPrefab.GetComponent<RectTransform>();
                slotPrefabRT.pivot = new Vector2(0f, 1f);

                for (int i = 0; i < count; i++)
                {
                    GameObject slotGo = Instantiate(_slotUiPrefab);
                    slotGo.transform.SetParent(_contentAreaRT.transform);
                    slotGo.SetActive(true);
                    slotGo.AddComponent<PreviewItemSlot>();

                    slotGo.transform.localScale = Vector3.one; // 버그 해결

                    HideGameObject(slotGo);

                    __previewSlotGoList.Add(slotGo);
                }

                DrawGrid();
                SetImageAlpha();
            }
            void DrawGrid()
            {
                Vector2 beginPos = new Vector2(_contentAreaPadding, -_contentAreaPadding);
                Vector2 curPos = beginPos;

                // Draw Slots
                int index = 0;
                for (int j = 0; j < _verticalSlotCount; j++)
                {
                    for (int i = 0; i < _horizontalSlotCount; i++)
                    {
                        GameObject slotGo = __previewSlotGoList[index++];
                        RectTransform slotRT = slotGo.GetComponent<RectTransform>();

                        slotRT.anchoredPosition = curPos;
                        slotRT.sizeDelta = new Vector2(_slotSize, _slotSize);
                        __previewSlotGoList.Add(slotGo);

                        // Next X
                        curPos.x += (_slotMargin + _slotSize);
                    }

                    // Next Line
                    curPos.x = beginPos.x;
                    curPos.y -= (_slotMargin + _slotSize);
                }
            }
            void HideGameObject(GameObject go)
            {
                go.hideFlags = HideFlags.HideAndDontSave;

                Transform tr = go.transform;
                for (int i = 0; i < tr.childCount; i++)
                {
                    tr.GetChild(i).gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            void SetImageAlpha()
            {
                foreach (var go in __previewSlotGoList)
                {
                    var images = go.GetComponentsInChildren<Image>();
                    foreach (var img in images)
                    {
                        img.color = new Color(img.color.r, img.color.g, img.color.b, __previewAlpha);
                        var outline = img.GetComponent<Outline>();
                        if (outline)
                            outline.effectColor = new Color(outline.effectColor.r, outline.effectColor.g, outline.effectColor.b, __previewAlpha);
                    }
                }
            }
        }

        private class PreviewItemSlot : MonoBehaviour { }

        [UnityEditor.InitializeOnLoad]
        private static class Destroyer
        {
            private static Queue<GameObject> targetQueue = new Queue<GameObject>();

            static Destroyer()
            { 
                UnityEditor.EditorApplication.update += () =>
                {
                    for (int i = 0; targetQueue.Count > 0 && i < 100000; i++)
                    {
                        var next = targetQueue.Dequeue();
                        DestroyImmediate(next);
                    }
                };
            }
            public static void Destroy(GameObject go) => targetQueue.Enqueue(go);
        }
#endif

        #endregion
    }
}