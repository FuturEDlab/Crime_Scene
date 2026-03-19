using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SliderDragDetector : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public Action onDragStart;
    public Action onDragEnd;

    public void OnBeginDrag(PointerEventData eventData) => onDragStart?.Invoke();
    public void OnEndDrag(PointerEventData eventData) => onDragEnd?.Invoke();
}