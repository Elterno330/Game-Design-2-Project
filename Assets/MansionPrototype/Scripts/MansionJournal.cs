using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Process J before Player.Update, including a closing-frame lock against E/F/R.
[DefaultExecutionOrder(-200)]
public sealed class MansionJournal : MonoBehaviour
{
    public MansionJournalData Data { get; } = new MansionJournalData();
    public bool IsOpen { get; private set; }
    public bool BlocksWorldInput => blockedFrame == Time.frameCount;

    MansionPrototype story;
    GameObject overlay, notification;
    RectTransform content;
    ScrollRect scroll;
    Button entryButton, charactersTab, cluesTab;
    bool showingClues;
    int blockedFrame = -1;
    float notificationUntil = -1f;

    static readonly Color Ink = new Color(.11f, .10f, .14f);
    static readonly Color Cream = new Color(.96f, .92f, .83f);
    static readonly Color Plum = new Color(.16f, .13f, .19f);
    static readonly Color Gold = new Color(.79f, .64f, .38f);
    static readonly Color CardCream = new Color(.97f, .94f, .86f);
    static readonly Color Brown = new Color(.28f, .18f, .11f);

    public void Initialize(MansionPrototype director, Transform canvas)
    {
        story = director;
        BuildUI(canvas);
        ResetRun();
    }

    void Update()
    {
        if (!story) return;
        if (IsOpen && !story.CanOpenJournal) Close();

        var keys = Keyboard.current;
        if (keys != null && keys.jKey.wasPressedThisFrame)
        {
            BlockCurrentFrame(); // Consume even when the entry is disabled.
            Toggle();
        }
        else if (IsOpen && keys != null && keys.escapeKey.wasPressedThisFrame)
            Close();

        // UI Button.onClick runs on release, potentially after the director's Update.
        // Reserve both edge frames before Player/Story can react to other input.
        var mouse = Mouse.current;
        if (entryButton.gameObject.activeInHierarchy && mouse != null
            && (mouse.leftButton.wasPressedThisFrame || mouse.leftButton.wasReleasedThisFrame)
            && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)entryButton.transform, mouse.position.ReadValue()))
            BlockCurrentFrame();

        RefreshAvailability();
        story.player.inputLocked = BlocksWorldInput || !story.CanOpenJournal;
        notification.SetActive(Time.unscaledTime < notificationUntil && !story.IsTransitioning && !story.Finished);
    }

    public void RefreshAvailability()
    {
        if (IsOpen && !story.CanOpenJournal) Close();
        entryButton.interactable = story.CanOpenJournal;
        // The bottom-right entry must not cover the existing dialogue/portrait area.
        entryButton.gameObject.SetActive(story.CanOpenJournal);
    }

    void BlockCurrentFrame()
    {
        blockedFrame = Time.frameCount;
        story.player.inputLocked = true;
    }

    void Toggle()
    {
        if (IsOpen) { Close(); return; }
        if (!story.CanOpenJournal) return;
        BlockCurrentFrame();
        IsOpen = true;
        overlay.SetActive(true);
        ClearSelection();
        RefreshContent();
    }

    void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        overlay.SetActive(false);
        BlockCurrentFrame();
        ClearSelection();
    }

    static void ClearSelection()
    {
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }

    public void ResetRun()
    {
        Data.Reset();
        IsOpen = showingClues = false;
        blockedFrame = -1;
        notificationUntil = -1f;
        overlay.SetActive(false);
        notification.SetActive(false);
        ClearSelection();
        RefreshContent();
    }

    public void RecordIntroduction(string id)
    {
        if (Data.RecordIntroduction(id) && IsOpen) RefreshContent();
    }

    public void RecordHaroldDeath()
    {
        Data.RecordHaroldDeath();
        if (IsOpen) RefreshContent();
    }

    public void RecordInterrogation() => AnnounceClue(Data.RecordInterrogation());
    public void RecordBagInspection() => AnnounceClue(Data.RecordBagInspection());

    void AnnounceClue(bool added)
    {
        if (!added) return;
        notificationUntil = Time.unscaledTime + 3f;
        notification.SetActive(true);
        if (IsOpen) RefreshContent();
    }

    void SelectTab(bool clues)
    {
        showingClues = clues;
        RefreshContent();
    }

    void RefreshContent()
    {
        scroll.StopMovement();
        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        charactersTab.interactable = showingClues;
        cluesTab.interactable = !showingClues;
        content.GetComponent<VerticalLayoutGroup>().spacing = showingClues ? 14 : 20;
        int count = 0;
        if (showingClues)
        {
            foreach (var clue in Data.Clues)
            {
                BuildClueCard(clue);
                count++;
            }
        }
        else
        {
            for (int i = 0; i < story.Script.characters.Length; i++)
            {
                var character = story.Script.characters[i];
                if (!Data.KnowsCharacter(character.id)) continue;
                var row = Rect(content, "Character");
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 12; layout.padding = new RectOffset(8, 8, 8, 8);
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                var imageRect = Rect(row, "Portrait");
                var image = imageRect.gameObject.AddComponent<RawImage>();
                image.texture = story.portraits[i]; image.uvRect = story.portraitRegions[i];
                image.raycastTarget = false;
                float ratio = image.texture.width * image.uvRect.width / (image.texture.height * image.uvRect.height);
                var size = imageRect.gameObject.AddComponent<LayoutElement>();
                size.minWidth = size.preferredWidth = 90; size.preferredHeight = 90 / ratio;
                var column = Column(row, "Profile");
                column.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                Label(column, "Name", character.displayName, 23, Ink);
                Label(column, "Identity", character.identity, 20, Ink);
                if (character.id == "Harold" && Data.HaroldDeathKnown)
                    Label(column, "Status", "Status: Deceased (found on Day II).", 20, Plum);
                count++;
            }
        }
        if (count == 0)
            Label(content, "Empty", showingClues
                ? "No clues recorded yet. Investigate to discover evidence."
                : "No characters recorded yet. Finish a guest's introduction to add their profile.", 21, Ink);
        // Layout settles before the next rendered frame, including wrapped TMP heights.
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scroll.verticalNormalizedPosition = 1f;
    }

    void BuildClueCard(MansionJournalData.Clue clue)
    {
        var card = Rect(content, "Clue Card - " + clue.Id);
        BorderedPanel(card, CardCream);
        var layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        BuildClueImage(card, clue.Id);

        var column = Column(card, "Clue Text");
        var width = column.gameObject.AddComponent<LayoutElement>();
        // Reserve only the image width; all remaining width belongs to wrapped text.
        // Height still comes from the vertical layout and TMP preferred heights.
        width.minWidth = width.preferredWidth = 0;
        width.flexibleWidth = 1;
        var title = Label(column, "Title", clue.Title, 23, Ink);
        title.fontStyle = FontStyles.Bold;
        var kind = Label(column, "Type", clue.Kind, 18, Brown);
        var source = Label(column, "Source", "Source: " + clue.Source, 18, Brown);
        var description = Label(column, "Description", clue.Description, 20, Ink);
        foreach (var label in new[] { title, kind, source, description })
            label.gameObject.AddComponent<LayoutElement>().minWidth = 0;
    }

    void BuildClueImage(Transform card, string clueId)
    {
        var frame = Rect(card, "Clue Image Frame");
        BorderedPanel(frame, Cream);
        var size = frame.gameObject.AddComponent<LayoutElement>();
        size.minWidth = size.preferredWidth = 84;
        size.minHeight = size.preferredHeight = 84;
        var imageArea = Area(frame, "Image Area", Vector2.zero, Vector2.one);
        imageArea.offsetMin = new Vector2(4, 4);
        imageArea.offsetMax = new Vector2(-4, -4);

        // Match a stable clue ID, not its editable title/source text.
        if (clueId == "safe" && TryGetCluePortrait("Elara", out var texture, out var region))
        {
            var raw = Area(imageArea, "Elara Portrait", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
            raw.texture = texture; raw.uvRect = region; raw.raycastTarget = false;
            var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = texture.width * region.width / (texture.height * region.height);
        }
        else if (clueId == "tools" && story.journalToolsIcon)
        {
            var image = Area(imageArea, "Tools Sprite", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            image.sprite = story.journalToolsIcon;
            image.preserveAspect = true; image.raycastTarget = false;
        }
        else
            BuildFallbackClueIcon(imageArea, clueId == "tools");
    }

    bool TryGetCluePortrait(string id, out Texture2D texture, out Rect region)
    {
        texture = null;
        region = new Rect(0, 0, 1, 1);
        var characters = story.Script?.characters;
        int index = characters == null ? -1 : Array.FindIndex(characters, character => character.id == id);
        if (index < 0 || story.portraits == null || index >= story.portraits.Length || !story.portraits[index])
            return false;
        texture = story.portraits[index];
        if (story.portraitRegions != null && index < story.portraitRegions.Length)
        {
            var crop = story.portraitRegions[index];
            if (crop.width > 0 && crop.height > 0) region = crop;
        }
        return true;
    }

    static Image BorderedPanel(RectTransform rect, Color color)
    {
        var background = Panel(rect, color);
        background.raycastTarget = false;
        var border = rect.gameObject.AddComponent<Outline>();
        border.effectColor = Brown; border.effectDistance = new Vector2(1, -1);
        return background;
    }

    static void BuildFallbackClueIcon(Transform parent, bool tools)
    {
        // Temporary code-drawn icons; all strokes let ScrollRect receive pointer input.
        if (tools)
        {
            BorderedPanel(Area(parent, "Bag Handle", new Vector2(.34f, .66f), new Vector2(.66f, .87f)), Cream);
            BorderedPanel(Area(parent, "Tool Bag", new Vector2(.12f, .18f), new Vector2(.88f, .70f)), CardCream);
            IconStroke(parent, "Flap", new Vector2(.13f, .52f), new Vector2(.87f, .55f));
            IconStroke(parent, "Left Strap", new Vector2(.26f, .19f), new Vector2(.29f, .69f));
            IconStroke(parent, "Right Strap", new Vector2(.71f, .19f), new Vector2(.74f, .69f));
            IconStroke(parent, "Clasp", new Vector2(.45f, .45f), new Vector2(.55f, .59f));
        }
        else
        {
            var page = Area(parent, "Default Clue Icon", new Vector2(.22f, .14f), new Vector2(.78f, .86f));
            BorderedPanel(page, CardCream);
            for (int i = 0; i < 3; i++)
                IconStroke(page, "Note Line", new Vector2(.17f, .68f - i * .20f), new Vector2(.83f, .71f - i * .20f));
        }
    }

    static void IconStroke(Transform parent, string name, Vector2 min, Vector2 max)
        => Panel(Area(parent, name, min, max), Brown).raycastTarget = false;

    static RectTransform Rect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static RectTransform Area(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = Rect(parent, name);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    static Image Panel(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>(); image.color = color;
        return image;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color)
    {
        var label = Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text; label.fontSize = size; label.color = color;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false;
        return label;
    }

    static RectTransform Column(Transform parent, string name)
    {
        var rect = Rect(parent, name);
        var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 7;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        return rect;
    }

    static Button Button(Transform parent, string name, string text, Vector2 min, Vector2 max, Action action)
    {
        var area = Area(parent, name, min, max);
        var image = Panel(area, Plum);
        var button = area.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.disabledColor = new Color(.55f, .51f, .58f); button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var label = Label(area, "Label", text, 22, Cream);
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true; label.fontSizeMin = 14; label.fontSizeMax = 22;
        label.overflowMode = TextOverflowModes.Truncate;
        label.rectTransform.anchorMin = new Vector2(.03f, .05f); label.rectTransform.anchorMax = new Vector2(.97f, .95f);
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        button.onClick.AddListener(() => action());
        return button;
    }

    void BuildUI(Transform canvas)
    {
        var blocker = Area(canvas, "Journal Overlay", Vector2.zero, Vector2.one);
        Panel(blocker, new Color(0, 0, 0, .12f)).raycastTarget = false; overlay = blocker.gameObject;
        var panel = Area(blocker, "Journal Panel", new Vector2(.62f, .15f), new Vector2(.98f, .80f));
        Panel(panel, Cream);
        var title = Label(panel, "Title", "Journal", 29, Ink);
        title.rectTransform.anchorMin = new Vector2(.04f, .895f); title.rectTransform.anchorMax = new Vector2(.66f, .98f);
        title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
        title.enableAutoSizing = true; title.fontSizeMin = 18; title.fontSizeMax = 29;
        Button(panel, "Close", "Close [Esc]", new Vector2(.67f, .90f), new Vector2(.97f, .98f), Close);
        charactersTab = Button(panel, "Characters Tab", "Characters", new Vector2(.035f, .80f), new Vector2(.49f, .88f), () => SelectTab(false));
        cluesTab = Button(panel, "Clues Tab", "Clues", new Vector2(.51f, .80f), new Vector2(.965f, .88f), () => SelectTab(true));

        var scrollArea = Area(panel, "Scroll View", new Vector2(.025f, .025f), new Vector2(.975f, .78f));
        scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35f;
        var viewport = Area(scrollArea, "Viewport", Vector2.zero, new Vector2(.95f, 1));
        Panel(viewport, Cream); viewport.gameObject.AddComponent<RectMask2D>();
        content = Column(viewport, "Content");
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
        var contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 20; contentLayout.padding = new RectOffset(8, 8, 8, 8);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content;
        var barArea = Area(scrollArea, "Scrollbar", new Vector2(.96f, 0), Vector2.one);
        Panel(barArea, new Color(.72f, .68f, .60f));
        var bar = barArea.gameObject.AddComponent<Scrollbar>();
        var sliding = Area(barArea, "Sliding Area", Vector2.zero, Vector2.one);
        var handle = Area(sliding, "Handle", Vector2.zero, Vector2.one);
        bar.targetGraphic = Panel(handle, Gold); bar.handleRect = handle;
        bar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = bar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        entryButton = Button(canvas, "Journal Entry", "Journal [J]", new Vector2(.84f, .075f), new Vector2(.98f, .135f), Toggle);

        var toast = Area(canvas, "Journal Notification", new Vector2(.34f, .90f), new Vector2(.70f, .97f));
        Panel(toast, Plum).raycastTarget = false; notification = toast.gameObject;
        var message = Label(toast, "Message", "New clue added to Journal", 22, Cream);
        message.alignment = TextAlignmentOptions.Center;
        message.enableAutoSizing = true; message.fontSizeMin = 14; message.fontSizeMax = 22;
        message.rectTransform.anchorMin = new Vector2(.025f, .05f); message.rectTransform.anchorMax = new Vector2(.975f, .95f);
        message.rectTransform.offsetMin = message.rectTransform.offsetMax = Vector2.zero;
    }
}
