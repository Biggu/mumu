using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace mumu
{
    /// <summary>
    /// おにわのカメラ操作（1本指で動かす・2本指で拡大縮小・ホイールで拡大縮小）
    /// </summary>
    public class GardenInputController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler, IDragHandler, IScrollHandler
    {
        /// <summary>ホイール1段あたりの倍率</summary>
        private const float ScrollZoomStep = 1.1f;
        private const float MaxScrollDelta = 3f;

        private MapViewController view = null;
        private Camera eventCamera = null;
        /// <summary>動かしている指（pointerId → スクリーン座標）</summary>
        private readonly Dictionary<int, Vector2> pointers = new Dictionary<int, Vector2>();

        /// <summary>
        /// カメラを受け取る
        /// </summary>
        /// <param name="_view"></param>
        public void Setup(MapViewController _view)
        {
            view = _view;
        }

        /// <summary>
        /// 押下（指を覚える。2本まで）
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnPointerDown(PointerEventData _eventData)
        {
            if (view == null)
            {
                return;
            }

            view.CancelFocus();
            eventCamera = _eventData.pressEventCamera;
            if (pointers.Count < 2)
            {
                pointers[_eventData.pointerId] = _eventData.position;
            }
        }

        /// <summary>
        /// ドラッグのしきい値を無くす（すぐ動かせるように）
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnInitializePotentialDrag(PointerEventData _eventData)
        {
            _eventData.useDragThreshold = false;
        }

        /// <summary>
        /// 動かす（1本指は移動、2本指は拡大縮小と移動）
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnDrag(PointerEventData _eventData)
        {
            if (view == null || !pointers.TryGetValue(_eventData.pointerId, out Vector2 previous))
            {
                return;
            }

            Vector2 current = _eventData.position;
            if (pointers.Count == 1)
            {
                view.PanBy(previous, current, eventCamera);
            }
            else
            {
                view.Pinch(previous, current, getOtherPointer(_eventData.pointerId), eventCamera);
            }
            pointers[_eventData.pointerId] = current;
        }

        /// <summary>
        /// 指を離した
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnPointerUp(PointerEventData _eventData)
        {
            pointers.Remove(_eventData.pointerId);
        }

        /// <summary>
        /// ホイールで拡大縮小
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnScroll(PointerEventData _eventData)
        {
            if (view == null)
            {
                return;
            }

            view.CancelFocus();
            eventCamera = _eventData.enterEventCamera;
            float delta = Mathf.Clamp(_eventData.scrollDelta.y, -MaxScrollDelta, MaxScrollDelta);
            view.ZoomAt(_eventData.position, view.Scale * Mathf.Pow(ScrollZoomStep, delta), eventCamera);
        }

        /// <summary>
        /// 無効になったら指を忘れる
        /// </summary>
        private void OnDisable()
        {
            pointers.Clear();
        }

        /// <summary>
        /// もう1本の指の位置
        /// </summary>
        /// <param name="_pointerId"></param>
        private Vector2 getOtherPointer(int _pointerId)
        {
            foreach (var pair in pointers)
            {
                if (pair.Key != _pointerId)
                {
                    return pair.Value;
                }
            }
            return Vector2.zero;
        }
    }
}
