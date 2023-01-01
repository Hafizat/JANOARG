using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlaylistScroll : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static PlaylistScroll main;

    public float Offset;
    public int ListOffset;
    public float Velocity;

    public Playlist Playlist;
    public PlaylistScrollItem ItemSample;
    public Dictionary<string, PlayableSong> Songs;
    public List<PlaylistScrollItem> Items;
    public int ListPadding = 10;
    public float ItemSize = 52;

    bool isDragging;
    RectTransform self;
    float oldOffset = float.NaN;
    float oldVelocity = float.NaN;
    float oldTime;

    public RectTransform MainCanvas;
    public RectTransform SafeArea;
    public RectTransform SelectedSongBox;
    public Button SelectedSongButton;
    public TMP_Text SongNameLabel;
    public TMP_Text ArtistNameLabel;
    public TMP_Text DataLabel;

    public RectTransform ProfileBar;
    public RectTransform ListActionBar;
    public RectTransform SongActionBar;

    public RectTransform DifficultyHolder;
    public LayoutGroup DifficultyHolderLayout;
    public List<DifficultyItem> DifficultyItems;
    public DifficultyItem DifficultySample;

    bool isScrolling = false;
    bool isSelectionShown = false;
    bool isAnimating = false;
    PlaylistScrollItem SelectedItem;
    int SelectedIndex = 0;

    public bool isSelected;
    public int SelectedDifficultyIndex;
    public DifficultyItem SelectedDifficulty;
    public List<Color> DifficultyColors;

    public bool isLaunching;
    public RectTransform ChartDetail;
    public CanvasGroup ChartDetailGroup;

    void Awake()
    {
        main = this;
        CommonScene.Load();
    }

    void OnDestroy()
    {
        main = main == this ? null : main;
    }
    
    void Start()
    {
        self = GetComponent<RectTransform>();

        StartCoroutine(GetSong());
    }

    IEnumerator GetSong()
    {
        Songs = new Dictionary<string, PlayableSong>();
        foreach (string path in Playlist.ItemPaths)
        {
            ResourceRequest req = Resources.LoadAsync(path);
            yield return new WaitUntil(() => req.isDone);
            Debug.Log(path + " " + req.asset);
            Songs.Add(path, (PlayableSong)req.asset);
        }

        InitItems();
    }

    void InitItems()
    {
        Items.Clear();
        for (int a = -ListPadding; a <= ListPadding; a++)
        {
            PlaylistScrollItem item = Instantiate(ItemSample, transform);
            string path = Playlist.ItemPaths[Modulo(a, Playlist.ItemPaths.Count)];
            item.SetSong(Songs[path], path);
            Items.Add(item);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isDragging)
        {
            if (Offset == oldOffset && Velocity == oldVelocity)
            {
                oldTime += Time.deltaTime;
                if (oldTime > .1f) Velocity = 0;
            }
            else
            {
                oldTime = 0;
            }
        }
        else
        {
            if (Mathf.Abs(Velocity) < 10)
            {
                Velocity = 0;
                Offset += (Mathf.Round(Offset / ItemSize) * ItemSize - Offset) * (1 - Mathf.Pow(.001f, Time.deltaTime));
                isScrolling = false;
            }
            else
            {
                Velocity *= Mathf.Pow(.3f * Mathf.Min(Mathf.Abs(Velocity / 100), 1), Time.deltaTime);
                Offset += Velocity * Time.deltaTime;
            }
        }

        if (Items.Count > 0)
        {
            if (oldOffset != Offset)
            {
                int pos = -Mathf.RoundToInt(Offset / ItemSize);

                while (ListOffset < pos)
                {
                    PlaylistScrollItem item = Items[0];
                    Items.RemoveAt(0);
                    Items.Add(item);
                    ListOffset++;
                    string path = Playlist.ItemPaths[Modulo(ListOffset + ListPadding, Playlist.ItemPaths.Count)];
                    item.SetSong(Songs[path], path);
                    isScrolling = true;
                }
                while (ListOffset > pos)
                {
                    PlaylistScrollItem item = Items[Items.Count - 1];
                    Items.RemoveAt(Items.Count - 1);
                    Items.Insert(0, item);
                    ListOffset--;
                    string path = Playlist.ItemPaths[Modulo(ListOffset - ListPadding, Playlist.ItemPaths.Count)];
                    item.SetSong(Songs[path], path);
                    isScrolling = true;
                }

                Offset = Modulo(Offset, ItemSize * Songs.Count);
                ListOffset = pos = -Mathf.RoundToInt(Offset / ItemSize);

                float ofs = Offset + (pos - ListPadding) * ItemSize;
                foreach (PlaylistScrollItem item in Items)
                {
                    RectTransform rt = (RectTransform)item.transform;
                    float realOfs = (ofs + Mathf.Clamp(ofs * .6f, -32, 32)) * Mathf.Min(Mathf.Abs(ofs) / ItemSize * 2 + .1f, 1);
                    rt.anchoredPosition = new Vector2(Ease.Get(Mathf.Abs(realOfs / self.rect.height * 2), "Circle", EaseMode.In) * self.rect.width / 2, -realOfs);
                    ofs += ItemSize;
                }

                if (isSelectionShown == true && isAnimating == false)
                {
                    SelectedSongBox.anchoredPosition = GetSelectionOffset();
                }
            }

            if (isSelectionShown == false && isScrolling == false)
            {
                ShowSelection();
            }
            else if (isSelectionShown == true && isScrolling == true)
            {
                HideSelection();
            }
            isSelectionShown = !isScrolling;

            oldOffset = Offset;
            oldVelocity = Velocity;
        }
    }

    Vector2 GetSelectionOffset()
    {
        float y = ((RectTransform)SelectedItem.transform).anchoredPosition.y;
        return Vector2.up * (y * Mathf.Min(Mathf.Abs(y) / ItemSize / 2, 1));
    }

    public void ShowSelection()
    {
        if (!isAnimating) StartCoroutine(ShowSelectionAnim());
    }

    IEnumerator ShowSelectionAnim()
    {
        SelectedIndex = -Mathf.RoundToInt(Offset / ItemSize);
        SelectedItem = Items[ListPadding];
        SongNameLabel.text = SelectedItem.SongNameLabel.text;
        ArtistNameLabel.text = SelectedItem.ArtistNameLabel.text;
        DataLabel.text = SelectedItem.DataText;

        SelectedSongBox.gameObject.SetActive(true);
        SelectedItem.CoverImage.gameObject.SetActive(false);
        isAnimating = true;

        void LerpSelection(float value)
        {
            float ease = Ease.Get(value, "Exponential", EaseMode.InOut);
            Rect coverRect = SelectedItem.CoverImage.rectTransform.rect;
            Vector2 coverPos = MainCanvas.InverseTransformPoint(SelectedItem.CoverImage.transform.position);

            SelectedSongBox.sizeDelta = Vector2.Lerp(coverRect.size, new Vector2(0, 100), ease);
            SelectedSongBox.anchorMin = Vector2.Lerp(new Vector2(.5f, .5f), new Vector2(0, .5f), ease);
            SelectedSongBox.anchorMax = Vector2.Lerp(new Vector2(.5f, .5f), new Vector2(1, .5f), ease);
            SelectedSongBox.anchoredPosition = Vector2.Lerp(coverPos, GetSelectionOffset(), ease);
            SelectedItem.SongNameLabel.rectTransform.anchoredPosition = new Vector2(9 + 200 * ease, SelectedItem.SongNameLabel.rectTransform.anchoredPosition.y);
            SelectedItem.ArtistNameLabel.rectTransform.anchoredPosition = new Vector2(10 + 200 * ease, SelectedItem.ArtistNameLabel.rectTransform.anchoredPosition.y);
            SelectedItem.SongNameLabel.alpha = SelectedItem.ArtistNameLabel.alpha = 1 - ease;

            float ease2 = Ease.Get(Mathf.Max(value * 2 - 1, 0), "Quadratic", EaseMode.Out);
            SongNameLabel.rectTransform.anchoredPosition = new Vector2(100 - 50 * ease2, SongNameLabel.rectTransform.anchoredPosition.y);
            ArtistNameLabel.rectTransform.anchoredPosition = new Vector2(101 - 50 * ease2, ArtistNameLabel.rectTransform.anchoredPosition.y);
            DataLabel.rectTransform.anchoredPosition = new Vector2(102 - 50 * ease2, DataLabel.rectTransform.anchoredPosition.y);
            SongNameLabel.alpha = ArtistNameLabel.alpha = DataLabel.alpha = ease2;

            float ease3 = Ease.Get(value, "Quintic", EaseMode.Out);
            ProfileBar.anchoredPosition = new Vector2(0, -40 * ease3);
            ListActionBar.anchoredPosition = new Vector2(0, 40 * ease3);
        }

        for (float a = 0; a < 1; a += Time.deltaTime / .6f)
        {
            LerpSelection(a);
            yield return null;
        }
        LerpSelection(1);

        isAnimating = false;
        if (isScrolling == true) StartCoroutine(HideSelectionAnim());
    }

    public void HideSelection()
    {
        if (!isAnimating) StartCoroutine(HideSelectionAnim());
    }

    IEnumerator HideSelectionAnim()
    {
        isAnimating = true;

        void LerpSelection(float value)
        {
            float ease = Ease.Get(value, "Exponential", EaseMode.Out);
            Rect coverRect = SelectedItem.CoverImage.rectTransform.rect;
            Vector2 coverPos = MainCanvas.InverseTransformPoint(SelectedItem.CoverImage.transform.position);

            SelectedSongBox.sizeDelta = Vector2.Lerp(new Vector2(0, 100), coverRect.size, ease);
            SelectedSongBox.anchorMin = Vector2.Lerp(new Vector2(0, .5f), new Vector2(.5f, .5f), ease);
            SelectedSongBox.anchorMax = Vector2.Lerp(new Vector2(1, .5f), new Vector2(.5f, .5f), ease);
            SelectedSongBox.anchoredPosition = Vector2.Lerp(GetSelectionOffset(), coverPos, ease);
            SelectedItem.SongNameLabel.rectTransform.anchoredPosition = new Vector2(209 - 200 * ease, SelectedItem.SongNameLabel.rectTransform.anchoredPosition.y);
            SelectedItem.ArtistNameLabel.rectTransform.anchoredPosition = new Vector2(210 - 200 * ease, SelectedItem.ArtistNameLabel.rectTransform.anchoredPosition.y);
            SelectedItem.SongNameLabel.alpha = SelectedItem.ArtistNameLabel.alpha = ease;

            float ease2 = Ease.Get(value, "Quadratic", EaseMode.Out);
            SongNameLabel.rectTransform.anchoredPosition = new Vector2(50 - 100 * ease2, SongNameLabel.rectTransform.anchoredPosition.y);
            ArtistNameLabel.rectTransform.anchoredPosition = new Vector2(51 - 100 * ease2, ArtistNameLabel.rectTransform.anchoredPosition.y);
            DataLabel.rectTransform.anchoredPosition = new Vector2(52 - 100 * ease2, DataLabel.rectTransform.anchoredPosition.y);
            SongNameLabel.alpha = ArtistNameLabel.alpha = DataLabel.alpha = 1 - ease2;

            float ease3 = 1 - Ease.Get(value, "Quintic", EaseMode.Out);
            ProfileBar.anchoredPosition = new Vector2(0, -40 * ease3);
            ListActionBar.anchoredPosition = new Vector2(0, 40 * ease3);
        }

        for (float a = 0; a < 1; a += Time.deltaTime / .3f)
        {
            LerpSelection(a);
            if (Mathf.Abs(SelectedIndex + Offset / ItemSize) > ListPadding) break;
            yield return null;
        }
        LerpSelection(1);

        SelectedSongBox.gameObject.SetActive(false);
        SelectedItem.CoverImage.gameObject.SetActive(true);
        isAnimating = false;
        if (isScrolling == false) StartCoroutine(ShowSelectionAnim());
    }

    public void SelectSong()
    {
        if (!isAnimating) StartCoroutine(SelectSongAnim());
    }

    public IEnumerator SelectSongAnim() 
    {
        isAnimating = true;
        isSelected = true;

        SelectedSongButton.interactable = false;

        foreach (DifficultyItem diff in DifficultyItems)
        {
            Destroy(diff.gameObject);
        }
        DifficultyItems.Clear();
        SelectedDifficulty = null;
        foreach (ExternalChartMeta chart in SelectedItem.Song.Charts)
        {
            DifficultyItem item = Instantiate(DifficultySample, DifficultyHolder);
            item.SetChart(chart);
            DifficultyItems.Add(item);
            if (SelectedDifficulty == null || Mathf.Abs(chart.DifficultyIndex - SelectedDifficultyIndex) < Mathf.Abs(SelectedDifficulty.Chart.DifficultyIndex - SelectedDifficultyIndex))
                SelectedDifficulty = item;
        }
        StartCoroutine(SelectedDifficulty.SelectAnim());

        void LerpSelection(float value)
        {
            float ease = Ease.Get(value, "Exponential", EaseMode.In);
            self.sizeDelta = Vector2.one * (1800 + 1800 * ease);

            float ease2 = Ease.Get(value, "Quintic", EaseMode.InOut);
            SelectedSongBox.sizeDelta = new Vector2(0, 100 + 60 * ease2);

            SongNameLabel.rectTransform.anchoredPosition = new Vector2(SongNameLabel.rectTransform.anchoredPosition.x, -29 + 24 * ease2);
            ArtistNameLabel.rectTransform.anchoredPosition = new Vector2(ArtistNameLabel.rectTransform.anchoredPosition.x, 5 + 24 * ease2);
            DataLabel.rectTransform.anchoredPosition = new Vector2(DataLabel.rectTransform.anchoredPosition.x, 27 + 24 * ease2);
            DifficultyHolder.anchoredPosition = new Vector2(DifficultyHolder.anchoredPosition.x, -52 + 70 * ease2);

            float ease3 = 1 - Ease.Get(value * 2, "Quintic", EaseMode.Out);
            ListActionBar.anchoredPosition = new Vector2(0, 40 * ease3);

            float ease4 = Ease.Get(value * 2 - 1, "Quintic", EaseMode.Out);
            SongActionBar.anchoredPosition = new Vector2(0, 40 * ease4);
        }

        for (float a = 0; a < 1; a += Time.deltaTime / .8f)
        {
            LerpSelection(a);
            oldOffset += 1e-5f;
            yield return null;
        }
        LerpSelection(1);

        isAnimating = false;
    }

    public void DeselectSong()
    {
        if (!isAnimating) StartCoroutine(DeselectSongAnim());
    }

    public IEnumerator DeselectSongAnim() 
    {
        isAnimating = true;
        isSelected = false;

        void LerpSelection(float value)
        {
            float ease = Ease.Get(value, "Exponential", EaseMode.Out);
            self.sizeDelta = Vector2.one * (1800 * ease);

            float ease2 = Ease.Get(value * 2, "Quintic", EaseMode.Out);
            SelectedSongBox.sizeDelta = new Vector2(0, 160 - 60 * ease2);

            SongNameLabel.rectTransform.anchoredPosition = new Vector2(SongNameLabel.rectTransform.anchoredPosition.x, -5 - 24 * ease2);
            ArtistNameLabel.rectTransform.anchoredPosition = new Vector2(ArtistNameLabel.rectTransform.anchoredPosition.x, 29 - 24 * ease2);
            DataLabel.rectTransform.anchoredPosition = new Vector2(DataLabel.rectTransform.anchoredPosition.x, 51 - 24 * ease2);
            DifficultyHolder.anchoredPosition = new Vector2(DifficultyHolder.anchoredPosition.x, 18 - 70 * ease2);
            
            float ease3 = 1 - Ease.Get(value * 2, "Quintic", EaseMode.Out);
            SongActionBar.anchoredPosition = new Vector2(0, 40 * ease3);

            float ease4 = Ease.Get(value * 2 - 1, "Quintic", EaseMode.Out);
            ListActionBar.anchoredPosition = new Vector2(0, 40 * ease4);
        }

        for (float a = 0; a < 1; a += Time.deltaTime / .8f)
        {
            LerpSelection(a);
            oldOffset += 1e-5f;
            yield return null;
        }
        LerpSelection(1);
        
        SelectedSongButton.interactable = true;

        isAnimating = false;
    }

    public void Launch()
    {
        if (!isAnimating) StartCoroutine(LaunchAnim());
    }

    public IEnumerator LaunchAnim() 
    {
        isAnimating = true;
        isLaunching = true;

        DifficultyHolderLayout.enabled = false;
        SelectedDifficulty.transform.SetAsLastSibling();
        float scale = MainCanvas.transform.localScale.x;
        foreach (DifficultyItem item in DifficultyItems) 
        {
            if (item != SelectedDifficulty)
            {
                Rigidbody2D rb = item.gameObject.AddComponent<Rigidbody2D>();
                RectTransform rt = rb.GetComponent<RectTransform>();

                rt.pivot = Vector2.one * .5f;
                rt.anchoredPosition += rt.rect.size / 2;


                rb.gravityScale = 50 * scale;
                Vector2 force = Random.insideUnitCircle * 100 * scale;
                force.y = Mathf.Abs(force.y) + 100 * scale;
                rb.velocity = force;
                rb.angularVelocity = Random.value * 20 - 10;
            }
        }
        
        RectTransform sdr = SelectedDifficulty.GetComponent<RectTransform>();
        float sdrX = sdr.anchoredPosition.x;

        void LerpSelection(float value)
        {
            float ease = Ease.Get(value, "Quintic", EaseMode.Out);
            SelectedSongBox.sizeDelta = new Vector2((-100 + SafeArea.sizeDelta.x) * ease, 160 - 20 * ease);

            float posX = Mathf.Lerp(50, 20, ease);
            SongNameLabel.rectTransform.anchoredPosition = new Vector2(posX, -5 + 10 * ease);
            ArtistNameLabel.rectTransform.anchoredPosition = new Vector2(posX + 1, 29 + 10 * ease);
            DataLabel.rectTransform.anchoredPosition = new Vector2(posX + 2, 51 + 30 * ease);
            DifficultyHolder.anchoredPosition = new Vector2(posX, 18);

            float ease2 = 1 - Ease.Get(value * 2, "Quintic", EaseMode.Out);
            ProfileBar.anchoredPosition = new Vector2(0, -40 * ease2);
            SongActionBar.anchoredPosition = new Vector2(0, 40 * ease2);
        }

        void LerpSelection2(float value)
        {
            float ease = Ease.Get(value, "Exponential", EaseMode.InOut);
            float ease2 = Ease.Get(value * 2, "Exponential", EaseMode.Out);
            sdr.anchoredPosition = new Vector3((sdrX + 100 * ease2) * (1 - ease), sdr.anchoredPosition.y);
            ChartDetail.anchoredPosition = new Vector3(235 + (sdrX + 200) * (1 - ease), ChartDetail.anchoredPosition.y);
            
            float ease3 = Ease.Get(value * 2 - 1, "Exponential", EaseMode.Out);
            ChartDetailGroup.alpha = ease3;
        }

        for (float a = 0; a < 1; a += Time.deltaTime / .8f)
        {
            LerpSelection(a);
            LerpSelection2(a / 3);
            yield return null;
        }
        LerpSelection(1);

        for (float a = 0; a < 2; a += Time.deltaTime / .8f)
        {
            LerpSelection2((a + 1) / 3);
            yield return null;
        }
        LerpSelection2(1);

        ChartPlayer.MetaSongPath = SelectedItem.Path;
        ChartPlayer.MetaChartPosition = SelectedItem.Song.Charts.IndexOf(SelectedDifficulty.Chart);


        AsyncOperation op = SceneManager.LoadSceneAsync("Player", LoadSceneMode.Additive);
        yield return new WaitUntil(() => op.isDone);
        yield return new WaitUntil(() => ChartPlayer.main.IsPlaying);
        
        void LerpSelection3(float value)
        {
            float ease = Ease.Get(value, "Exponential", EaseMode.In);
            SelectedSongBox.localScale = Vector3.one * (1 - ease);
        }

        for (float a = 0; a < 2; a += Time.deltaTime / .8f)
        {
            LerpSelection3(a);
            yield return null;
        }
        LerpSelection3(1);

        SceneManager.UnloadSceneAsync("Song Select");
        
        
        isAnimating = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isSelected)
        {
            isDragging = isScrolling || !isAnimating;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isSelected)
        {
            if (isDragging)
            {
                Offset -= eventData.delta.y;
                Velocity = -eventData.delta.y / Time.deltaTime;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    public int Modulo(int a, int b) => ((a % b) + b) % b;
    public float Modulo(float a, float b) => ((a % b) + b) % b;
}