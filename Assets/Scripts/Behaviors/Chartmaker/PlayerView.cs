using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler
{
    public static PlayerView main;

    public Camera MainCamera;
    public Image BoundingBox;
    [Space]
    public ChartManager Manager;
    [Space]
    public Transform Holder;
    public CMLanePlayer LanePlayerSample;
    public List<CMLanePlayer> LanePlayers { get; private set; } = new();
    public CMHitPlayer HitPlayerSample;
    public MeshRenderer HoldMeshSample;
    [Space]
    public Mesh FreeFlickIndicator;
    public Mesh ArrowFlickIndicator;
    [Space]
    public PlayOptionsPanel PlayOptions;
    [Space]
    public AudioSource SoundPlayer;
    public AudioClip NormalHitSound;
    public AudioClip CatchHitSound;
    [Space]
    public Graphic NotificationText;
    public Graphic NotificationBox;
    [Space]
    public RectTransform CurrentLaneLine;
    public RectTransform SelectedItemLine;
    public RectTransform StartHandle;
    public RectTransform CenterHandle;
    public RectTransform EndHandle;
    [Space]
    public float[] GridSize = {0.5f};

    float CurrentTime;
    
    int[] HitObjectsRemaining = new [] { 0, 0 };

    public HandleDragMode CurrentDragMode;
    bool isDragged;

    public void Awake()
    {
        main = this;
    }

    public void Start()
    {
        InitMeshes();
    }


    public void Update()
    {
        RectTransform rt = (RectTransform)transform;
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        
        Rect bound = new(
            corners[0].x,
            corners[0].y,
            corners[2].x - corners[0].x,
            corners[2].y - corners[0].y
        );

        MainCamera.rect = new(
            bound.x / Screen.width,
            bound.y / Screen.height,
            bound.width / Screen.width,
            bound.height / Screen.height
        );

        Rect safeZone = new(
            bound.x + 12,
            bound.y + 12,
            bound.width - 24,
            bound.height - 24
        );

        if (safeZone.width / safeZone.height > 3 / 2f)
        {
            float width = safeZone.height * 3 / 2;
            safeZone.x += (safeZone.width - width) / 2;
            safeZone.width = width;
        }
        else
        {
            float height = safeZone.width / 3 * 2;
            safeZone.y += (safeZone.height - height) / 2;
            safeZone.height = height;
        }

        BoundingBox.rectTransform.sizeDelta = safeZone.size;
        float camRatio = safeZone.height / bound.height;
        MainCamera.fieldOfView = Mathf.Atan2(Mathf.Tan(30 * Mathf.Deg2Rad), camRatio) * 2 * Mathf.Rad2Deg;

        if (CurrentTime != Chartmaker.main.SongSource.time) UpdateObjects();
    }

    public void UpdateObjects()
    {
        CurrentTime = Chartmaker.main.SongSource.time;

        if (Chartmaker.main.CurrentChart != null)
        {
            if (Chartmaker.main.CurrentChart != Manager?.CurrentChart) 
            {
                Manager = new ChartManager(
                    Chartmaker.main.CurrentSong, Chartmaker.main.CurrentChart,
                    121, InformationBar.main.sec, InformationBar.main.beat
                );
            } else {
                Manager.Update(
                    InformationBar.main.sec, InformationBar.main.beat
                );
            }
            
            MainCamera.transform.position = Manager.Camera.CameraPivot;
            MainCamera.transform.eulerAngles = Manager.Camera.CameraRotation; 
            MainCamera.transform.Translate(Vector3.back * Manager.Camera.PivotDistance);

            RenderSettings.fogColor = MainCamera.backgroundColor = Manager.PalleteManager.CurrentPallete.BackgroundColor;
            BoundingBox.color = NotificationText.color = NotificationBox.color = Manager.PalleteManager.CurrentPallete.InterfaceColor;

            for (int a = 0; a < Manager.Lanes.Count; a++)
            {
                if (LanePlayers.Count <= a) LanePlayers.Add(Instantiate(LanePlayerSample, Holder));
                LanePlayers[a].UpdateObjects(Manager.Lanes[a]);
            }
            while (LanePlayers.Count > Manager.Lanes.Count)
            {
                Destroy(LanePlayers[Manager.Lanes.Count].gameObject);
                LanePlayers.RemoveAt(Manager.Lanes.Count);
            }
            
            if (!TimelinePanel.main.isDragged && PlayOptions.HitsoundsVolume > 0)
            {
                if (Manager.HitObjectsRemaining[0] < HitObjectsRemaining[0])
                {
                    SoundPlayer.PlayOneShot(NormalHitSound, PlayOptions.HitsoundsVolume);
                }
                if (Manager.HitObjectsRemaining[1] < HitObjectsRemaining[1])
                {
                    SoundPlayer.PlayOneShot(CatchHitSound, PlayOptions.HitsoundsVolume);
                }
            }
            HitObjectsRemaining = Manager.HitObjectsRemaining;
        }

        UpdateHandles();
    }

    public void UpdateHandles() 
    {
        CurrentLaneLine.gameObject.SetActive(false);
        SelectedItemLine.gameObject.SetActive(false);
        StartHandle.gameObject.SetActive(false);
        CenterHandle.gameObject.SetActive(false);
        EndHandle.gameObject.SetActive(false);

        if (Chartmaker.main.SongSource.isPlaying)
        {
            return;
        }

        if (Chartmaker.main.CurrentChart != null && InspectorPanel.main.CurrentLane != null)
        {
            int index = Chartmaker.main.CurrentChart.Lanes.IndexOf(InspectorPanel.main.CurrentLane);
            if (index < 0) goto endLane;
            LaneManager man = Manager.Lanes[index];
            if ((man.CurrentMesh?.vertexCount ?? 0) > 2)
            {
                Vector2 start = MainCamera.WorldToScreenPoint(man.StartPos);
                Vector2 end = MainCamera.WorldToScreenPoint(man.EndPos);
                CurrentLaneLine.gameObject.SetActive(true);
                CurrentLaneLine.position = (start + end) / 2;
                CurrentLaneLine.sizeDelta = new(Vector2.Distance(start, end), CurrentLaneLine.sizeDelta.y);
                CurrentLaneLine.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector2.left, end - start));
            }
        }

        endLane: 

        switch (InspectorPanel.main.CurrentObject)
        {
            case Lane lane: 
            {
                int index = Chartmaker.main.CurrentChart.Lanes.IndexOf(lane);
                if (index < 0) goto endSel;
                LaneManager man = Manager.Lanes[index];
                
                Vector2 center = MainCamera.WorldToScreenPoint(man.FinalPosition);
                CenterHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Center);
                CenterHandle.position = center;

                if ((man.CurrentMesh?.vertexCount ?? 0) > 2)
                {
                    Vector2 start = MainCamera.WorldToScreenPoint(man.StartPos);
                    Vector2 end = MainCamera.WorldToScreenPoint(man.EndPos);
                    SelectedItemLine.gameObject.SetActive(true);
                    SelectedItemLine.position = (start + end) / 2;
                    SelectedItemLine.sizeDelta = new(Vector2.Distance(start, end), SelectedItemLine.sizeDelta.y);
                    SelectedItemLine.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector3.left, end - start));
                    if (SelectedItemLine.sizeDelta.x > 20) 
                    {
                        StartHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Start);
                        StartHandle.position = start;
                        EndHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.End);
                        EndHandle.position = end;
                        EndHandle.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector2.up, end - start));
                    }
                }
            } break;
            case LaneStep step: 
            {
                int lindex = Chartmaker.main.CurrentChart.Lanes.IndexOf(InspectorPanel.main.CurrentLane);
                if (lindex < 0) goto endSel;
                LaneManager lman = Manager.Lanes[lindex];

                int index = InspectorPanel.main.CurrentLane.LaneSteps.IndexOf(step);
                if (index < 0) goto endSel;
                LaneStepManager man = lman.Steps[index];

                if (man.Offset >= Chartmaker.main.SongSource.time)
                {
                    Vector3 offset = lman.FinalRotation * Vector3.forward * (man.Distance - lman.CurrentDistance) + lman.FinalPosition;
                    Vector2 wcenter = (man.CurrentStep.StartPos + man.CurrentStep.EndPos) / 2;
                    Vector2 start = MainCamera.WorldToScreenPoint(lman.FinalRotation * man.CurrentStep.StartPos + offset);
                    Vector2 end = MainCamera.WorldToScreenPoint(lman.FinalRotation * man.CurrentStep.EndPos + offset);
                    Vector2 center = MainCamera.WorldToScreenPoint(lman.FinalRotation * wcenter + offset);
                    SelectedItemLine.gameObject.SetActive(true);
                    SelectedItemLine.position = (start + end) / 2;
                    SelectedItemLine.sizeDelta = new(Vector2.Distance(start, end), SelectedItemLine.sizeDelta.y);
                    SelectedItemLine.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector3.left, end - start));
                    CenterHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Center);
                    CenterHandle.position = center;
                    if (SelectedItemLine.sizeDelta.x > 20) 
                    {
                        StartHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Start);
                        StartHandle.position = start;
                        EndHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.End);
                        EndHandle.position = end;
                        EndHandle.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector2.up, end - start));
                    }
                }
            } break;
            case HitObject hit: 
            {
                int lindex = Chartmaker.main.CurrentChart.Lanes.IndexOf(InspectorPanel.main.CurrentLane);
                if (lindex < 0) goto endSel;
                LaneManager lman = Manager.Lanes[lindex];

                int index = InspectorPanel.main.CurrentLane.Objects.IndexOf(hit);
                if (index < 0) goto endSel;
                HitObjectManager man = lman.Objects[index];

                if (man.TimeEnd >= Chartmaker.main.SongSource.time)
                {
                    Vector2 start = MainCamera.WorldToScreenPoint(lman.FinalRotation * (man.StartPos + lman.CurrentDistance * Vector3.back) + lman.FinalPosition);
                    Vector2 end = MainCamera.WorldToScreenPoint(lman.FinalRotation * (man.EndPos + lman.CurrentDistance * Vector3.back) + lman.FinalPosition);
                    Vector2 center = MainCamera.WorldToScreenPoint(lman.FinalRotation * (man.Position + lman.CurrentDistance * Vector3.back) + lman.FinalPosition);
                    SelectedItemLine.gameObject.SetActive(true);
                    SelectedItemLine.position = (start + end) / 2;
                    SelectedItemLine.sizeDelta = new(Vector2.Distance(start, end), SelectedItemLine.sizeDelta.y);
                    SelectedItemLine.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector3.left, end - start));
                    CenterHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Center);
                    CenterHandle.position = center;
                    if (SelectedItemLine.sizeDelta.x > 20) 
                    {
                        StartHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Start);
                        StartHandle.position = start;
                        EndHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.End);
                        EndHandle.position = end;
                        EndHandle.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector2.up, end - start));
                    }
                }
            } break;
        }
        
        endSel: ;
    }

    public void InitMeshes() 
    {
        if (!FreeFlickIndicator) 
        {
            Mesh mesh = new();
            List<Vector3> verts = new();
            List<int> tris = new();

            verts.AddRange(new Vector3[] { new(0, 1.6f), new(1, 0), new(0, -1), new(0, -1.6f), new(-1, 0), new(0, 1) });
            tris.AddRange(new [] {0, 1, 2, 3, 4, 5});

            mesh.SetVertices(verts);
            mesh.SetUVs(0, verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            FreeFlickIndicator = mesh;
        }
        if (!ArrowFlickIndicator) 
        {
            Mesh mesh = new();
            List<Vector3> verts = new();
            List<int> tris = new();

            verts.AddRange(new Vector3[] { new(-1, 0), new(0, 2.2f), new(1, 0), new(.71f, -.71f), new(0, -1), new(-.71f, -.71f) });
            tris.AddRange(new [] {0, 1, 2, 2, 3, 0, 3, 4, 0, 4, 5, 0});

            mesh.SetVertices(verts);
            mesh.SetUVs(0, verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            ArrowFlickIndicator = mesh;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        bool contains(RectTransform rt) => RectTransformUtility.RectangleContainsScreenPoint(rt, eventData.pressPosition, eventData.pressEventCamera);

        CurrentDragMode = HandleDragMode.None;

        if (contains(StartHandle)) CurrentDragMode = HandleDragMode.Start;
        else if (contains(CenterHandle)) CurrentDragMode = HandleDragMode.Center;
        else if (contains(EndHandle)) CurrentDragMode = HandleDragMode.End;

        if (CurrentDragMode == HandleDragMode.None) return;

        switch (InspectorPanel.main.CurrentObject)
        {
            case Lane lane:
            {
                int index = Chartmaker.main.CurrentChart.Lanes.IndexOf(lane);
                if (index < 0) return;
                LaneManager man = Manager.Lanes[index];
                
                Vector3 inv(Vector3 x) => Quaternion.Inverse(Quaternion.Euler(man.CurrentLane.Rotation)) * (x - man.CurrentLane.Position);

                Func<Vector3> get = 
                    CurrentDragMode == HandleDragMode.Start ? (() => man.StartPos) : 
                    CurrentDragMode == HandleDragMode.Center ? (() => man.CurrentLane.Position) : 
                    CurrentDragMode == HandleDragMode.End ? (() => man.EndPos) : null;
                    
                Vector3 gizmoAnchor = get();
                
                OnDragEvent += (ev) => {
                    Vector3? dragPos = CurrentDragMode == HandleDragMode.Center ? 
                        RaycastScreenToPlane(ev.position, Vector3.forward * get().z, Quaternion.identity) :
                        RaycastScreenToPlane(ev.position, man.CurrentLane.Position, Quaternion.Euler(man.CurrentLane.Rotation));
                    if (dragPos != null)
                    {
                        if (CurrentDragMode is not HandleDragMode.Center) dragPos = inv((Vector3)dragPos);
                        if (GridSize[0] > 0)
                        {
                            Vector3 des = new Vector3();
                            for (int x = 0; x < 3; x++) des[x] = Mathf.Round((dragPos?[x] ?? 0) / GridSize[0]) * GridSize[0];
                            dragPos = des;
                        } 
                    }
                    else
                    {
                        dragPos = gizmoAnchor;
                    }
                
                    if (CurrentDragMode == HandleDragMode.Start) 
                        DoMove<ChartmakerMoveLaneStartAction, Lane>(lane, (Vector3)dragPos - get());
                    else if (CurrentDragMode == HandleDragMode.Center) 
                        DoMove<ChartmakerMoveLaneAction, Lane>(lane, (Vector3)dragPos - get());
                    else if (CurrentDragMode == HandleDragMode.End) 
                        DoMove<ChartmakerMoveLaneEndAction, Lane>(lane, (Vector3)dragPos - get());
                };                  
            } 
            break;
            
            case LaneStep step:
            {
                int lindex = Chartmaker.main.CurrentChart.Lanes.IndexOf(InspectorPanel.main.CurrentLane);
                if (lindex < 0) return;
                LaneManager lman = Manager.Lanes[lindex];

                int index = InspectorPanel.main.CurrentLane.LaneSteps.IndexOf(step);
                if (index < 0) return;
                LaneStepManager man = lman.Steps[index];

                Vector3 inv(Vector3 x) => Quaternion.Inverse(Quaternion.Euler(lman.CurrentLane.Rotation)) * (x - lman.CurrentLane.Position);

                Func<Vector3> get = 
                    CurrentDragMode == HandleDragMode.Start ? (() => man.CurrentStep.StartPos) : 
                    CurrentDragMode == HandleDragMode.Center ? (() => (man.CurrentStep.StartPos + man.CurrentStep.EndPos) / 2) : 
                    CurrentDragMode == HandleDragMode.End ? (() => man.CurrentStep.EndPos) : null;
                    
                Vector3 gizmoAnchor = get();

                OnDragEvent += (ev) => {
                    Vector3? dragPos = RaycastScreenToPlane(ev.position, lman.CurrentLane.Position + (man.Distance - lman.CurrentDistance) * Vector3.forward, Quaternion.Euler(lman.CurrentLane.Rotation));
                    if (dragPos != null)
                    {
                        dragPos = inv((Vector3)dragPos);
                        if (GridSize[0] > 0)
                        {
                            Vector3 des = new();
                            for (int x = 0; x < 3; x++) des[x] = Mathf.Round((dragPos?[x] ?? 0) / GridSize[0]) * GridSize[0];
                            dragPos = des;
                        } 
                    }
                    else
                    {
                        dragPos = gizmoAnchor;
                    }

                    Debug.Log(CurrentDragMode + " " + dragPos);
                
                    if (CurrentDragMode == HandleDragMode.Start) 
                        DoMove<ChartmakerMoveLaneStepStartAction, LaneStep>(step, (Vector3)dragPos - get());
                    else if (CurrentDragMode == HandleDragMode.Center) 
                        DoMove<ChartmakerMoveLaneStepAction, LaneStep>(step, (Vector3)dragPos - get());
                    else if (CurrentDragMode == HandleDragMode.End) 
                        DoMove<ChartmakerMoveLaneStepEndAction, LaneStep>(step, (Vector3)dragPos - get());
                };
            }
            break;
        }
        
        UpdateHandles();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragged)
        {
            OnEndDrag(eventData);
        }
    }

    public delegate void PointerEvent(PointerEventData eventData);

    public void OnDrag(PointerEventData eventData) 
    {
        if (CurrentDragMode != HandleDragMode.None)
        {
            isDragged = true;
            OnDragEvent?.Invoke(eventData);
            UpdateObjects();
        }
    }

    public PointerEvent OnDragEvent;

    public void OnEndDrag(PointerEventData eventData)
    {
        if (CurrentDragMode != HandleDragMode.None)
        {
            InspectorPanel.main.UpdateForm();
        }
        isDragged = false;
        OnDragEvent = null;
        CurrentDragMode = HandleDragMode.None;
        UpdateHandles();
    }
    
    public Vector3? RaycastScreenToPlane(Vector3 pos, Vector3 center, Quaternion rotation)
    {
        Plane plane = new (rotation * Vector3.back, center);
        Ray ray = MainCamera.ScreenPointToRay(new Vector2(pos.x, pos.y));
        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return null;
    }

    public void DoMove<TAction, TTarget>(TTarget item, Vector3 offset) where TAction : ChartmakerMoveAction<TTarget>, new()
    {
        if (offset == Vector3.zero) return;

        TAction action = null;
        ChartmakerHistory history = Chartmaker.main.History;

        if (history.ActionsBehind.Count > 0 && history.ActionsBehind.Peek() is TAction)
        {
            action = (TAction)history.ActionsBehind.Peek();
            if (!action.Item.Equals(item)) action = null;
        }

        if (action == null)
        {
            action = new()
            {
                Item = item
            };
            history.ActionsBehind.Push(action);
        }
        history.ActionsAhead.Clear();

        action.Undo();
        action.Offset += offset;
        action.Redo();

        Chartmaker.main.OnHistoryUpdate();
    }
}

public enum HandleDragMode
{
    None,
    Start,
    Center,
    End
}