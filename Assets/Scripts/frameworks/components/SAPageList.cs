﻿using UnityEngine;
using UnityEngine.UI;
using Sakura.core;
namespace Sakura
{
    public class SAPageList : AbstractPageList
    {
        protected RectTransform _layoutTransform;
        protected Vector2 _itemBound;
        protected Vector2 _itemSize;//Pagelist的render皮肤宽高
        protected Vector2 _itemPivot = new Vector2(0, 1);
        protected Vector2 _itemPivotOffset;
        protected Vector2 _spacing = new Vector2(2, 2);

        protected int _firstIndex = 0;
        protected bool _vertical;
        protected int _viewSize = -1;//计算pagelist在显示范围能显示几个
        protected int MaxColumn = -1;
        public int helpCacheSize = 1;

        protected Mask _rectMask;
        protected ScrollRect _scrollRect;
        protected RectTransform _scrollRectTransform;

        public SAPageList(IFactory itemFacotry, GameObject parentSkin, int itemWidth = 0, int itemHeight = 0,
            bool vertical = true, int maxColumn = -1)
        {
            _itemFacotry = itemFacotry;
            _vertical = vertical;

            this.MaxColumn = maxColumn;

            if (itemWidth == 0 && itemHeight == 0)
            {
                ISizeFactory a = itemFacotry as ISizeFactory;
                if (a != null)
                {
                    itemWidth = a.itemWidth;
                    itemHeight = a.itemHeight;
                }
            }

            _itemSize = new Vector2(itemWidth, itemHeight);
            updateItemBound();
            this.skin = parentSkin;
        }

        public virtual Vector2 spacing
        {
            get { return _spacing; }
            set
            {
                _spacing = value;
                updateItemBound();
            }
        }

        public Vector2 itemSize
        {
            get { return _itemSize; }
            set
            {
                _itemSize = value;
                updateItemBound();
            }
        }

        protected virtual void updateItemBound()
        {
            _itemBound = _itemSize + _spacing;

            //不加_spacing偏移,是不是感觉好神奇(只做自身偏移,不算间距)
            _itemPivotOffset.x = _itemSize.x * _itemPivot.x;
            _itemPivotOffset.y = _itemSize.y * (_itemPivot.y - 1.0f);
        }

        public ScrollRect getScrollRect()
        {
            return _scrollRect;
        }

        public void scrollLockDiction(bool isVertical)
        {
            _scrollRect.vertical = isVertical;
            _scrollRect.horizontal = !isVertical;
        }

        protected override void bindComponents()
        {
            _scrollRect = _skin.GetComponent<ScrollRect>();
            if (_scrollRect == null)
            {
                _scrollRect = _skin.AddComponent<ScrollRect>();
            }

            _scrollRectTransform = _skin.GetComponent<RectTransform>();

            _scrollRect.vertical = _vertical;
            _scrollRect.horizontal = !_vertical;

            GameObject layout = new GameObject("layout");
            _layoutTransform = layout.AddComponent<RectTransform>();
            _layoutTransform.anchorMax = new Vector2(0, 1);
            _layoutTransform.anchorMin = new Vector2(0, 1);
            _layoutTransform.pivot = new Vector2(0, 1);
            _layoutTransform.SetParent(_skin.transform, false);
            _scrollRect.content = _layoutTransform;

            _rectMask = _skin.GetComponent<Mask>();
            if (_rectMask == null)
            {
                _rectMask = _skin.AddComponent<Mask>();
            }

            Image imageForMask = _skin.GetComponent<Image>();
            if (imageForMask == null)
            {
                imageForMask = _skin.AddComponent<Image>();
                imageForMask.enabled = true;
                _rectMask.showMaskGraphic = false;
            }

            _scrollRect.onValueChanged.AddListener(onScrollChangHandle);

            RectTransform rectTransform = _skin.GetComponent<RectTransform>();
            Rect rect = rectTransform.rect;
            if (_viewSize == -1)
            {
                if ((_vertical && MaxColumn == -1) || (_vertical == false && MaxColumn != -1))
                {
                    _viewSize = Mathf.CeilToInt(rect.height / _itemBound.y) + 1;
                }
                else
                {
                    _viewSize = Mathf.CeilToInt(rect.width / _itemBound.x) + 1;
                }
            }
            else
            {
                if ((_vertical && MaxColumn == -1) || (_vertical == false && MaxColumn != -1))
                {
                    rect.width = _itemBound.x;
                    rect.height = _itemBound.y * _viewSize;
                }
                else
                {
                    rect.width = _viewSize * _itemBound.x;
                    rect.height = _itemBound.y;
                }
            }
        }


        public int viewSize
        {
            get
            {
                return _viewSize;
            }
            set { _viewSize = value; }
        }

        public bool showMaskGraphic
        {
            get
            {
                if (_rectMask != null)
                {
                    return _rectMask.showMaskGraphic;
                }
                return true;
            }
            set
            {
                if (_rectMask != null)
                {
                    _rectMask.showMaskGraphic = value;
                }
            }
        }


        protected override void onScrollChangHandle(Vector2 normalizedPosition)
        {
            float pos;
            int toIndex = 0;
            if ((_vertical && MaxColumn == -1) || (_vertical == false && MaxColumn != -1))
            {
                pos = _layoutTransform.anchoredPosition.y;
                toIndex = (int)(pos / _itemBound.y);
            }
            else
            {
                pos = _layoutTransform.anchoredPosition.x;
                toIndex = (int)(-pos / _itemBound.x);
            }
            toIndex = Mathf.Max(toIndex, 0);// Mathf.Abs(toIndex);
            int sourceCount = _sourceLength;
            if (MaxColumn != -1)
            {
                sourceCount = Mathf.CeilToInt(_sourceLength / (float)MaxColumn);
            }

            if (toIndex + _viewSize > sourceCount)
            {
                toIndex = sourceCount - _viewSize;
            }
            if (toIndex < 0)
            {
                toIndex = 0;
            }


            if (_firstIndex == toIndex)
            {
                return;
            }
            _firstIndex = toIndex;
            renderList();
        }

        /// <summary>
        /// 是否竖直滑动
        /// </summary>
        private bool isVertical
        {
            get { return getScrollRect().vertical; }
        }

        override public void scrollToBegin()
        {
            Vector2 v = _layoutTransform.anchoredPosition;
            if (isVertical)
            {
                v.y = 0;
            }
            else
            {
                v.x = 0;
            }
            _layoutTransform.anchoredPosition = v;
            onScrollChangHandle(v);
        }

        override public void scrollToEnd()
        {
            Vector2 v = _layoutTransform.anchoredPosition;
            if (isVertical)
            {
                float totalLength = _layoutTransform.sizeDelta.y;
                float showLength = skin.GetComponent<RectTransform>().sizeDelta.y;
                v.y = Mathf.Max(totalLength - showLength, 0);
            }
            else
            {
                float totalLength = _layoutTransform.sizeDelta.x;
                float showLength = skin.GetComponent<RectTransform>().sizeDelta.x;
                v.x = Mathf.Min(showLength - totalLength, 0);
            }
            _layoutTransform.anchoredPosition = v;
            onScrollChangHandle(v);
        }


        protected int currentStartChildIndex = 0;


        protected override IntVector2 getRenderListRange()
        {
            int end;
            int start = _firstIndex - helpCacheSize;
            if (start < 0)
            {
                start = 0;
            }
            end = start + _viewSize + 2 * helpCacheSize;

            if (MaxColumn != -1)
            {
                start *= MaxColumn;
                end *= MaxColumn;
            }

            if (_dataProvider != null)
            {
                _sourceLength = _dataProvider.Count;
            }
            else
            {
                _sourceLength = 0;
            }

            if (end > _sourceLength)
            {
                end = _sourceLength;
                if (MaxColumn != -1)
                {
                    int expEnd = Mathf.FloorToInt(end / (float)MaxColumn) * MaxColumn;
                    start = expEnd - (_viewSize + helpCacheSize) * MaxColumn;
                }
                else
                {
                    start = end - (_viewSize + helpCacheSize);
                }
                if (start < 0)
                {
                    start = 0;
                }
            }


            return new IntVector2(start, end);
        }

        protected override void addItemToContainer(IListItemRender item)
        {
            GameObject go = null;
            if (item is MonoBehaviour)
            {
                SAListItemRender view = item as SAListItemRender;
                go = view.skin;
            }
            else
            {
                SASkinBase view=item as SASkinBase;
                go = view.skin;
            }
            
            RectTransform rect;
            if (go != null)
            {
                rect = go.GetComponent<RectTransform>();
                if (rect == null)
                {
                    rect = go.AddComponent<RectTransform>();
                }

                rect.transform.SetParent(_layoutTransform, false);
                rect.anchorMax = new Vector2(0, 1);
                rect.anchorMin = new Vector2(0, 1);

                rect.pivot = _itemPivot;
                rect.sizeDelta = _itemSize;
            }
        }

        override protected void calculatorBound()
        {
            int row = 1;
            int col = 1;
            if (_vertical)
            {
                if (MaxColumn == -1)
                {
                    row = _sourceLength;
                    col = 1;
                }
                else
                {
                    row = Mathf.CeilToInt((float)_sourceLength / MaxColumn);
                    col = Mathf.Min(_sourceLength, MaxColumn);
                }
            }
            else
            {
                if (MaxColumn == -1)
                {
                    col = _sourceLength;
                    row = 1;
                }
                else
                {
                    row = Mathf.Min(_sourceLength, MaxColumn);
                    col = Mathf.CeilToInt((float)_sourceLength / MaxColumn);
                }
            }

            Vector2 newSize = new Vector2(_itemBound.x * col - _spacing.x, _itemBound.y * row - _spacing.y);
            if (_layoutTransform.sizeDelta.x != newSize.x || _layoutTransform.sizeDelta.y != newSize.y)
            {
                _layoutTransform.sizeDelta = newSize;
                //  this.dispatchEvent(new EventX(EventX.RESIZE));
            }
        }

        public void setLayoutPivot(Vector2 value)
        {
            _layoutTransform.pivot = value;
        }
        public void setItemPivot(Vector2 value)
        {
            _itemPivot = value;
            updateItemBound();
        }

        override protected void layout(IListItemRender render, int index = 0)
        {
            GameObject skin = null;
            if (render is MonoBehaviour)
            {
                SAListItemRender skinBase = render as SAListItemRender;
                skin = skinBase.skin;
            }
            else
            {
                SASkinBase skinBase=render as SASkinBase;
                skin = skinBase.skin;
            }

            RectTransform rect = skin.GetComponent<RectTransform>();
            rect.anchoredPosition = _itemPivotOffset + indexToPosition(index);
        }

        protected virtual Vector2 indexToPosition(int index)
        {
            int col = 1;
            int row = 1;
            if (_vertical)
            {
                if (MaxColumn == -1)
                {
                    col = 0;
                    row = index;
                }
                else
                {
                    row = index / MaxColumn;
                    col = index % MaxColumn;
                }
            }
            else
            {
                if (MaxColumn == -1)
                {
                    row = 0;
                    col = index;
                }
                else
                {
                    row = index % MaxColumn;
                    col = index / MaxColumn;
                }
            }

            return new Vector2(_itemBound.x * col, -_itemBound.y * row);
        }


        public override void scrollToIndex(int index)
        {
            if (index == -1)
            {
                index = 0;
            }

            Vector2 v = _layoutTransform.anchoredPosition;
            if (isVertical)
            {
                v.y = -indexToPosition(index).y;
            }
            else
            {
                v.x = -indexToPosition(index).x;
            }
            _layoutTransform.anchoredPosition = v;
            onScrollChangHandle(v);
        }
    }
}