﻿using UnityEngine;
using UnityEngine.EventSystems;

public class WindowHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    public static WindowHandler main;

    [Header("Objects")]
    public GameObject WindowControls;
    public RectTransform NavBar;
    public RectTransform ContentHolder;
    public RectTransform ModalHolder;
    public RectTransform LoaderHolder;
    public GameObject MenuButton;
    public RectTransform SongDetails;
    [Space]
    public TooltipTarget ResizeTooltip;
    public RectTransform ResizeIcon1;
    public RectTransform ResizeIcon2;
    [Header("Window")]
    public Vector2Int defaultWindowSize;
    public Vector2Int borderSize;
    public Vector2Int windowMargin;

    Vector2 mousePos = Vector2.zero;
    public bool maximized { get; private set; }
    bool isFullScreen;

    bool framed;

    float clickTime = float.NegativeInfinity;

    public void Awake()
    {
        main = this;
    }

    public void Start()
    {
    }

    public void Quit()
    {
        #if !UNITY_EDITOR && UNITY_STANDALONE_WIN 
            BorderlessWindow.UnhookWindowProc();
        #endif
    }

    public void Update() 
    {
        if (Screen.fullScreen != isFullScreen) 
        {
            isFullScreen = Screen.fullScreen;
            WindowControls.SetActive(!isFullScreen);
            if (!isFullScreen) BorderlessWindow.InitializeWindow();
        }
        

        if (maximized != BorderlessWindow.IsMaximized) 
        {
            maximized = BorderlessWindow.IsMaximized;
            ResizeTooltip.Text = maximized ? "Restore" : "Maximize";
            ResizeIcon1.sizeDelta = ResizeIcon2.sizeDelta = maximized ? new(8, 8) : new(10, 10);
        }
        if (framed != BorderlessWindow.IsFramed) 
        {
            framed = BorderlessWindow.IsFramed;
            ContentHolder.sizeDelta = ModalHolder.sizeDelta = LoaderHolder.sizeDelta = NavBar.anchoredPosition = Vector2.up * (framed ? 0 : -28);
            MenuButton.SetActive(framed);
            SongDetails.anchoredPosition = Vector2.right * (framed ? 32 : 4);
        }
    }

    public void ResetWindowSize()
    {
        BorderlessWindow.ResizeWindow(defaultWindowSize.x, defaultWindowSize.y);
    }

    public void CloseWindow()
    {
        EventSystem.current.SetSelectedGameObject(null);
        Application.Quit();
    }

    public void MinimizeWindow()
    {
        EventSystem.current.SetSelectedGameObject(null);
        BorderlessWindow.MinimizeWindow();
    }

    public void ResizeWindow()
    {
        EventSystem.current.SetSelectedGameObject(null);

        maximized = !maximized;

        if (maximized) BorderlessWindow.MaximizeWindow();
        else BorderlessWindow.RestoreWindow();
        
        var rect = BorderlessWindow.GetWindowRect();
        if (!maximized && rect.yMin < 0) BorderlessWindow.MoveWindowDelta(Vector2.up * rect.yMin);

        ResizeTooltip.Text = maximized ? "Restore" : "Maximize";
        ResizeIcon1.sizeDelta = ResizeIcon2.sizeDelta = maximized ? new(8, 8) : new(10, 10);
    }

    public void FinalizeDrag() 
    {
        if (!maximized) 
        {
            var rect = BorderlessWindow.GetWindowRect();
            if (rect.yMin - Input.mousePosition.y + Screen.height < 1 && !maximized) ResizeWindow();
            else if (rect.yMin < 0) BorderlessWindow.MoveWindowDelta(Vector2.up * rect.yMin);
        }
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (Time.time - clickTime < .5f)
        {
            ResizeWindow();
            clickTime = float.NegativeInfinity;
            mousePos = Vector2.zero * float.NaN;
        }
        else 
        {
            clickTime = Time.time;
            mousePos = Input.mousePosition;
        }
    }

    public void OnDrag(PointerEventData data)
    {
        if (float.IsNaN(mousePos.x) || BorderlessWindow.IsFramed) return;

        if (maximized) {
            ResizeWindow();
            var rect = BorderlessWindow.GetWindowRect();
            BorderlessWindow.MoveWindow(new Vector2(Mathf.Clamp(Input.mousePosition.x - rect.width / 2 + 7, 0, Screen.width), Screen.height * 2 - rect.height - Input.mousePosition.y - 28));
            mousePos = new Vector2(rect.width / 2 + 7, rect.height - 30);
        } else {
        }

        if (data.dragging)
        {
            BorderlessWindow.MoveWindowDelta((Vector2)Input.mousePosition - mousePos);
        }
    }

    public void OnEndDrag(PointerEventData data)
    {
        FinalizeDrag();
    }
}
