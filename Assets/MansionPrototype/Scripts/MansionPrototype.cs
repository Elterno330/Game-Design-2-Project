using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The existing Player, NPCDialog and GameManager remain the world-facing components.
// This director handles the PDF's short, deliberately paced two-day prototype.
public class MansionPrototype : MonoBehaviour
{
    [Serializable] public class Character
    {
        public string id, displayName;
        public string[] day1, day2, accusation;
    }
    [Serializable] public class DialogueScript
    {
        public string[] intro, question, interrogation, win, lose;
        public Character[] characters;
    }
    public TextAsset dialogueScript;
    public TMP_FontAsset dialogueFont;
    public Player player;
    public GameManager gameManager;
    public NPCDialog[] npcs;
    public Texture2D[] portraits;
    public Rect[] portraitRegions;
    public DialogueScript Script { get; private set; }
    public int Day { get; private set; } = 1;
    public bool Interrogated { get; private set; }
    public bool EvidenceFound { get; private set; }
    public bool Finished { get; private set; }
    public bool IsTransitioning { get; private set; }
    public bool DialogueOpen => dialogueRoot && dialogueRoot.activeSelf;
    public bool InChoice { get; private set; }
    public int Introductions => introduced.Count;
    public int DayTwoConversations => dayTwoHeard.Count;
    public string CurrentSentence => sentenceText ? sentenceText.text : "";
    public int SentenceIndex => sentenceIndex;

    readonly HashSet<string> introduced = new HashSet<string>();
    readonly HashSet<string> dayTwoHeard = new HashSet<string>();
    readonly List<GameObject> choices = new List<GameObject>();
    GameObject dialogueRoot, portraitRoot;
    RectTransform choiceRoot;
    RectTransform dialogueFrame;
    TextMeshProUGUI sentenceText, nameText, dayText, pointsText, promptText, hintText;
    RawImage portraitImage;
    Image portraitBackground;
    RectTransform portraitSlot;
    float portraitRatio = 1f;
    GameObject endingRoot;
    TextMeshProUGUI endingTitle, endingDescription;
    PrototypeClockIcon clockIcon;
    Button accuseButton;
    string[] sentences;
    string speaker;
    int sentenceIndex;
    Action onComplete;
    NPCDialog nearest;
    NPCDialog promptedNPC;
    int promptDay = -1;
    Canvas canvas;
    float canAdvanceAt;
    Vector3 startingPosition;
    CanvasGroup dayTransition;
    TextMeshProUGUI transitionTitle;
    float transitionElapsed;

    static readonly Color Ink = new Color(.11f, .10f, .14f);
    static readonly Color Cream = new Color(.96f, .92f, .83f);
    static readonly Color Gold = new Color(.79f, .64f, .38f);
    static readonly Color Plum = new Color(.16f, .13f, .19f);

    void Awake()
    {
        Script = JsonUtility.FromJson<DialogueScript>(dialogueScript.text);
        startingPosition = player.transform.position;
        BuildUI();
        foreach (var npc in npcs)
        {
            npc.prototype = this;
            npc.gameManager = gameManager;
            npc.dialog = sentenceText;
            npc.backgroundBox = dialogueRoot;
        }
        gameManager.NPCDialog = npcs.First(n => n.prototypeCharacterId == "Elara");
    }

    void Start() => RestartRun();

    public void RestartRun()
    {
        IsTransitioning = false;
        endingRoot.SetActive(false);
        dayTransition.gameObject.SetActive(false);
        Day = 1;
        Interrogated = EvidenceFound = Finished = false;
        introduced.Clear(); dayTwoHeard.Clear();
        player.GetComponent<Rigidbody>().position = startingPosition;
        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        gameManager.numberOfActions = 1;
        gameManager.gameOver = gameManager.nightTime = gameManager.choosingSuspect = false;
        RefreshNPCLabels();
        RefreshHUD();
        ShowSequence("Inner thoughts", Script.intro, CloseDialogue);
    }

    void Update()
    {
        if (endingRoot.activeSelf) return;
        if (IsTransitioning)
        {
            TickDayTransition(Time.unscaledDeltaTime);
            return;
        }
        var keys = Keyboard.current;
        if (DialogueOpen)
        {
            if (Time.unscaledTime < canAdvanceAt) return;
            bool keyboardContinue = keys != null && (keys.eKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame);
            bool mouseContinue = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (!InChoice && (keyboardContinue || mouseContinue)) Advance();
            if (InChoice && keys != null)
            {
                if (keys.digit1Key.wasPressedThisFrame) Choose(0);
                else if (keys.digit2Key.wasPressedThisFrame) Choose(1);
                else if (keys.digit3Key.wasPressedThisFrame) Choose(2);
                else if (keys.digit4Key.wasPressedThisFrame) Choose(3);
                else if (keys.digit5Key.wasPressedThisFrame) Choose(4);
            }
            return;
        }
        nearest = null;
        float nearestDistance = 2.8f * 2.8f;
        Vector3 playerPosition = player.transform.position;
        foreach (var npc in npcs)
        {
            if (!npc) continue;
            float distance = (npc.transform.position - playerPosition).sqrMagnitude;
            if (distance <= nearestDistance) { nearest = npc; nearestDistance = distance; }
        }
        if (nearest != promptedNPC || Day != promptDay)
        {
            promptText.text = nearest ? "E  " + (Day == 2 && nearest.prototypeCharacterId == "Harold" ? "Inspect Harold's bag" : "Talk to " + nearest.prototypeCharacterId)
                : "WASD / ARROWS  Walk       E  Talk nearby       F  Accuse on day 2";
            promptedNPC = nearest; promptDay = Day;
        }
        if (keys != null && keys.eKey.wasPressedThisFrame && nearest) Interact(nearest.prototypeCharacterId);
        else if (keys != null && keys.fKey.wasPressedThisFrame && Day == 2) OpenAccusation();
    }

    public void Interact(string id)
    {
        if (DialogueOpen || Finished || IsTransitioning) return;
        var character = Script.characters.First(c => c.id == id);
        if (Day == 1)
        {
            ShowSequence(id, character.day1, () =>
            {
                introduced.Add(id);
                RefreshHUD();
                ShowConversationMenu(id);
            });
        }
        else
        {
            ShowSequence(id == "Harold" ? "Inner thoughts" : id, character.day2, () =>
            {
                if (id == "Harold") EvidenceFound = true;
                else dayTwoHeard.Add(id);
                RefreshHUD();
                CloseDialogue();
            });
        }
    }

    void ShowConversationMenu(string id)
    {
        if (id == "Elara")
        {
            bool canSpend = !Interrogated && gameManager.numberOfActions >= 1;
            string reason = Interrogated ? "Interrogation complete" : "No time points remaining";
            ShowChoices(id, "Do you want to question Elara about the guests?",
                (canSpend ? "Interrogate Elara  -  1 time point" : "Interrogate Elara  -  1 point  |  " + reason, (Action)BeginInterrogation, canSpend),
                ("Leave", CloseDialogue, true));
        }
        else
        {
            ShowChoices(id, "Conversation complete. Introductions cost no time points.",
                ("Interrogate  -  not included in this prototype", (Action)null, false),
                ("Leave", CloseDialogue, true));
        }
    }

    public void BeginInterrogation()
    {
        if (Day != 1 || Interrogated || gameManager.numberOfActions < 1) return;
        Interrogated = true;
        gameManager.actionUsed();
        RefreshHUD();
        ShowSequence("Hunter", Script.question, () => ShowSequence("Elara", Script.interrogation, () =>
            ShowChoices("Hunter", "Continue to the next morning when you are ready.",
                ("Continue to day 2", (Action)BeginDayTwo, true))));
    }

    public void BeginDayTwo()
    {
        if (IsTransitioning || Day != 1 || !Interrogated) return;
        CloseDialogue();
        IsTransitioning = true;
        player.inputLocked = true;
        gameManager.nightTime = true;
        transitionElapsed = 0f;
        transitionTitle.alpha = 0f;
        dayTransition.alpha = 0f;
        dayTransition.gameObject.SetActive(true);
        RefreshHUD();
    }

    void TickDayTransition(float seconds)
    {
        transitionElapsed += Mathf.Max(0f, seconds);
        const float fadeOut = .6f, holdUntil = 1.4f, finishedAt = 2f;
        if (transitionElapsed < fadeOut)
        {
            dayTransition.alpha = Mathf.SmoothStep(0f, 1f, transitionElapsed / fadeOut);
            return;
        }
        // Change the scene's story state only once it is completely hidden.
        if (Day == 1)
        {
            Day = 2;
            gameManager.nightTime = false;
            gameManager.numberOfActions = 0;
            RefreshNPCLabels(); RefreshHUD();
            transitionTitle.alpha = 1f;
        }
        dayTransition.alpha = transitionElapsed < holdUntil ? 1f
            : 1f - Mathf.SmoothStep(0f, 1f, (transitionElapsed - holdUntil) / (finishedAt - holdUntil));
        if (transitionElapsed < finishedAt) return;
        dayTransition.gameObject.SetActive(false);
        IsTransitioning = false;
        ShowSequence("Inner thoughts", new[] { "The next morning, Harold Palmer is found dead.", "Inspect his bag for evidence, and speak to any guests you want to question before making an accusation." }, CloseDialogue);
    }

    public void OpenAccusation()
    {
        if (Day != 2 || Finished || IsTransitioning) return;
        if (!EvidenceFound)
        {
            ShowSequence("Hunter", new[] { "I should inspect Harold's bag before I accuse anyone." }, CloseDialogue);
            return;
        }
        ShowChoices("Hunter", "Who do you accuse? Choose a suspect to review, then confirm your accusation.",
            ("Daisy Finn", () => ConfirmAccusation("Daisy"), true),
            ("Sebes Yorke", () => ConfirmAccusation("Sebes"), true),
            ("Percy Norn", () => ConfirmAccusation("Percy"), true),
            ("Elara Venn", () => ConfirmAccusation("Elara"), true),
            ("Keep investigating", CloseDialogue, true));
    }

    public void ConfirmAccusation(string id)
    {
        if (Day != 2 || !EvidenceFound || Finished) return;
        ShowChoices(id, "Accuse " + Script.characters.First(c => c.id == id).displayName + "? This choice ends the prototype.",
            ("Confirm accusation", () => Accuse(id), true), ("Go back", OpenAccusation, true));
    }

    public void Accuse(string id)
    {
        if (Day != 2 || !EvidenceFound || Finished) return;
        var character = Script.characters.First(c => c.id == id);
        if (character.accusation.Length == 0) return;
        ShowSequence(id, character.accusation, () =>
        {
            gameManager.checkAccusation(id);
            Finished = true;
            bool won = id == "Sebes";
            string[] ending = (won ? Script.win : Script.lose).Select(s => s.Replace("[Sebes]", "Sebes").Replace("[NAME]", character.displayName)).ToArray();
            ShowSequence("Hunter", ending, () => ShowEnding(won));
            RefreshHUD();
        });
    }

    public void Advance()
    {
        if (IsTransitioning || InChoice || sentences == null || !DialogueOpen) return;
        if (++sentenceIndex < sentences.Length) DisplaySentence();
        else
        {
            var complete = onComplete;
            onComplete = null;
            sentences = null;
            complete?.Invoke();
        }
    }

    void ShowEnding(bool won)
    {
        CloseDialogue();
        player.inputLocked = true;
        endingTitle.text = won ? "You Win" : "You Lose";
        endingDescription.text = won ? "The werewolf is revealed.\nYou saved the remaining guests."
            : "The wrong suspect was accused.\nThe real werewolf is still alive.";
        endingRoot.SetActive(true);
        endingRoot.transform.SetAsLastSibling();
    }

    void LateUpdate()
    {
        if (!portraitRoot.activeInHierarchy) return;
        // Fit the frame to the image itself, so letterboxing cannot leave a
        // strip of background below the cropped character's feet/body.
        Vector2 available = portraitSlot.rect.size;
        const float border = 8f;
        float width = Mathf.Min(available.x - border, (available.y - border) * portraitRatio);
        var frame = (RectTransform)portraitRoot.transform;
        frame.sizeDelta = new Vector2(width + border, width / portraitRatio + border);
    }

    public void Choose(int index)
    {
        if (IsTransitioning || !InChoice || index < 0 || index >= choices.Count) return;
        var button = choices[index].GetComponent<Button>();
        if (button.interactable) button.onClick.Invoke();
    }

    void ShowSequence(string who, string[] text, Action complete)
    {
        ClearChoices(); InChoice = false;
        dialogueFrame.anchorMax = new Vector2(.965f, .32f);
        dialogueRoot.SetActive(true); player.inputLocked = true;
        speaker = who; sentences = text; sentenceIndex = 0; onComplete = complete;
        canAdvanceAt = Time.unscaledTime + .12f;
        DisplaySentence();
    }

    void DisplaySentence()
    {
        SetSpeaker(speaker);
        sentenceText.text = sentences[sentenceIndex];
        hintText.text = "Click or press E / Space to continue  >";
    }

    void ShowChoices(string who, string message, params (string text, Action action, bool enabled)[] options)
    {
        ClearChoices(); InChoice = true; sentences = null; onComplete = null;
        dialogueFrame.anchorMax = new Vector2(.965f, options.Length > 3 ? .40f : .32f);
        sentenceText.fontSizeMin = 18; sentenceText.fontSizeMax = 25;
        dialogueRoot.SetActive(true); player.inputLocked = true;
        SetSpeaker(who); sentenceText.text = message; hintText.text = "Click a choice or press its number";
        for (int i = 0; i < options.Length; i++)
        {
            var option = options[i];
            var button = MakeButton(choiceRoot, "Choice " + i, (i + 1) + "   " + option.text, option.action);
            button.interactable = option.enabled;
            var element = button.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = options.Length > 3 ? 28 : 34;
            choices.Add(button.gameObject);
        }
        sentenceText.rectTransform.anchorMin = new Vector2(.035f, .72f);
        sentenceText.rectTransform.anchorMax = new Vector2(.74f, .87f);
        canAdvanceAt = Time.unscaledTime + .12f;
    }

    void ClearChoices()
    {
        foreach (var choice in choices) { choice.SetActive(false); Destroy(choice); }
        choices.Clear();
        sentenceText.fontSizeMin = 23; sentenceText.fontSizeMax = 30;
        sentenceText.rectTransform.anchorMin = new Vector2(.035f, .25f);
        sentenceText.rectTransform.anchorMax = new Vector2(.74f, .83f);
    }

    void CloseDialogue()
    {
        ClearChoices(); dialogueRoot.SetActive(false); InChoice = false;
        sentences = null; onComplete = null; player.inputLocked = false;
        RefreshHUD();
    }

    void SetSpeaker(string id)
    {
        int index = Array.FindIndex(Script.characters, c => c.id == id);
        nameText.text = index < 0 ? (id == "Hunter" ? "HUNTER" : "HUNTER  /  INNER THOUGHTS") : Script.characters[index].displayName.ToUpperInvariant();
        sentenceText.fontStyle = id == "Inner thoughts" ? FontStyles.Italic : FontStyles.Normal;
        portraitRoot.SetActive(index >= 0);
        if (index >= 0)
        {
            portraitImage.texture = portraits[index];
            // Restore the original upper-body framing, without stretching it.
            Rect region = portraitRegions[index];
            portraitImage.uvRect = region;
            portraitRatio = portraits[index].width * region.width / (portraits[index].height * region.height);
            portraitBackground.color = portraits[index].isReadable ? portraits[index].GetPixel(0, 0) : Plum;
        }
    }

    void RefreshHUD()
    {
        dayText.text = Day == 1 ? "Day I" : "Day II";
        pointsText.text = gameManager.numberOfActions.ToString();
        clockIcon.color = gameManager.numberOfActions > 0 ? Gold : new Color(.45f, .43f, .42f);
        accuseButton.interactable = Day == 2 && !Finished && !DialogueOpen && !IsTransitioning;
    }

    void RefreshNPCLabels()
    {
        foreach (var npc in npcs)
        {
            var label = npc.GetComponentInChildren<TextMeshPro>();
            if (label) label.text = npc.prototypeCharacterId == "Harold" && Day == 2 ? "HAROLD'S BAG\n<color=#E9C980>Inspect evidence</color>" : npc.prototypeCharacterId.ToUpperInvariant();
            if (npc.prototypeCharacterId == "Harold")
            {
                var body = npc.transform.Find("Body");
                if (body) body.localScale = Day == 2 ? new Vector3(.7f, .3f, .7f) : Vector3.one;
            }
        }
    }

    static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }
    static Image Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var rt = Rect(parent, name, min, max); var image = rt.gameObject.AddComponent<Image>(); image.color = color; return image;
    }
    TextMeshProUGUI Text(Transform parent, string name, string text, Vector2 min, Vector2 max, float size, Color color)
    {
        var rt = Rect(parent, name, min, max); var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text; label.fontSize = size; label.color = color; label.raycastTarget = false;
        if (dialogueFont) label.font = dialogueFont;
        label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.MidlineLeft; return label;
    }
    Button MakeButton(Transform parent, string name, string text, Action action)
    {
        var image = Panel(parent, name, Vector2.zero, Vector2.one, Plum);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(.72f, .62f, .82f); colors.disabledColor = new Color(.45f, .43f, .47f); button.colors = colors;
        Text(image.transform, "Label", text, new Vector2(.035f, .03f), new Vector2(.97f, .97f), 22, Cream);
        if (action != null) button.onClick.AddListener(() => action());
        return button;
    }

    void BuildUI()
    {
        var root = new GameObject("Prototype UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
        var scale = root.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1600, 900); scale.matchWidthOrHeight = .5f;
        var hud = Panel(root.transform, "Day and Time", new Vector2(.025f, .90f), new Vector2(.225f, .98f), new Color(.06f, .055f, .08f, .94f));
        Panel(hud.transform, "Gold Rule", Vector2.zero, new Vector2(1, .025f), Gold).raycastTarget = false;
        dayText = Text(hud.transform, "Day", "", new Vector2(.07f, .12f), new Vector2(.46f, .88f), 29, Cream);
        Panel(hud.transform, "Divider", new Vector2(.48f, .23f), new Vector2(.484f, .77f), Gold).raycastTarget = false;
        var clock = Rect(hud.transform, "Time Point Clock", new Vector2(.56f, .24f), new Vector2(.70f, .76f));
        clockIcon = clock.gameObject.AddComponent<PrototypeClockIcon>(); clockIcon.raycastTarget = false;
        pointsText = Text(hud.transform, "Time Points", "", new Vector2(.76f, .12f), new Vector2(.94f, .88f), 30, Cream);
        var accuseArea = Rect(root.transform, "Accuse Area", new Vector2(.84f, .915f), new Vector2(.98f, .98f));
        accuseButton = MakeButton(accuseArea, "Accuse", "Accuse  [F]", OpenAccusation);
        var footer = Panel(root.transform, "World Prompt", Vector2.zero, new Vector2(1, .065f), new Color(.06f, .055f, .08f, .94f));
        promptText = Text(footer.transform, "Prompt", "", new Vector2(.025f, .05f), new Vector2(.98f, .95f), 21, Cream);
        // A screen-sized click blocker prevents world/HUD actions beneath an open conversation.
        var modal = Panel(root.transform, "Dialogue Modal", Vector2.zero, Vector2.one, new Color(0, 0, 0, .13f)); dialogueRoot = modal.gameObject;
        var box = Panel(modal.transform, "Dialogue Frame", new Vector2(.035f, .055f), new Vector2(.965f, .32f), Plum);
        dialogueFrame = box.rectTransform;
        var inner = Panel(box.transform, "Parchment", new Vector2(.004f, .012f), new Vector2(.996f, .988f), Cream);
        var tab = Panel(inner.transform, "Nameplate", new Vector2(.025f, .88f), new Vector2(.42f, 1.045f), Plum); tab.raycastTarget = false;
        nameText = Text(tab.transform, "Speaker", "", new Vector2(.04f, .05f), new Vector2(.96f, .95f), 24, Cream);
        nameText.characterSpacing = 1.5f;
        sentenceText = Text(inner.transform, "Sentence", "", new Vector2(.035f, .25f), new Vector2(.74f, .83f), 30, Ink);
        sentenceText.enableAutoSizing = true; sentenceText.fontSizeMin = 23; sentenceText.fontSizeMax = 30;
        hintText = Text(inner.transform, "Continue Hint", "", new Vector2(.035f, .045f), new Vector2(.68f, .13f), 16, Plum);
        choiceRoot = Rect(inner.transform, "Choices", new Vector2(.035f, .12f), new Vector2(.74f, .68f));
        var layout = choiceRoot.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 5; layout.childControlHeight = true; layout.childForceExpandHeight = false; layout.childControlWidth = true; layout.childForceExpandWidth = true;
        // Anchor the portrait to the screen so shrinking the text box doesn't
        // shrink the character. These bounds match the original large portrait.
        portraitSlot = Rect(modal.transform, "Portrait Area", new Vector2(.758f, .083f), new Vector2(.947f, .565f));
        var portrait = Panel(portraitSlot, "Portrait Frame", new Vector2(.5f, 0), new Vector2(.5f, 0), Gold); portrait.raycastTarget = false; portraitRoot = portrait.gameObject;
        portrait.rectTransform.pivot = new Vector2(.5f, 0);
        portraitBackground = Panel(portrait.transform, "Portrait Background", Vector2.zero, Vector2.one, Plum); portraitBackground.raycastTarget = false;
        portraitBackground.rectTransform.offsetMin = new Vector2(4, 4);
        portraitBackground.rectTransform.offsetMax = new Vector2(-4, -4);
        var raw = Rect(portraitBackground.transform, "Character Portrait", Vector2.zero, Vector2.one); portraitImage = raw.gameObject.AddComponent<RawImage>(); portraitImage.raycastTarget = false;
        dialogueRoot.SetActive(false);
        var endingModal = Panel(root.transform, "Ending Modal", Vector2.zero, Vector2.one, new Color(.035f, .025f, .045f, .82f));
        endingRoot = endingModal.gameObject;
        var endingBorder = Panel(endingModal.transform, "Ending Border", new Vector2(.28f, .30f), new Vector2(.72f, .70f), Gold);
        var endingCard = Panel(endingBorder.transform, "Ending Card", Vector2.zero, Vector2.one, Cream);
        endingCard.rectTransform.offsetMin = new Vector2(3, 3);
        endingCard.rectTransform.offsetMax = new Vector2(-3, -3);
        endingTitle = Text(endingCard.transform, "Result", "", new Vector2(.08f, .67f), new Vector2(.92f, .91f), 48, Plum);
        endingTitle.alignment = TextAlignmentOptions.Center;
        Panel(endingCard.transform, "Rule", new Vector2(.30f, .61f), new Vector2(.70f, .615f), Gold).raycastTarget = false;
        endingDescription = Text(endingCard.transform, "Outcome", "", new Vector2(.08f, .33f), new Vector2(.92f, .57f), 25, Ink);
        endingDescription.alignment = TextAlignmentOptions.Center;
        var replayArea = Rect(endingCard.transform, "Replay Area", new Vector2(.25f, .10f), new Vector2(.75f, .25f));
        var replay = MakeButton(replayArea, "Play Again", "Play Again", RestartRun);
        replay.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        endingRoot.SetActive(false);
        var fade = Panel(root.transform, "Day Transition", Vector2.zero, Vector2.one, Color.black);
        dayTransition = fade.gameObject.AddComponent<CanvasGroup>();
        dayTransition.blocksRaycasts = true;
        transitionTitle = Text(fade.transform, "Next Day", "Day II", new Vector2(.2f, .4f), new Vector2(.8f, .6f), 54, Cream);
        transitionTitle.alignment = TextAlignmentOptions.Center;
        dayTransition.gameObject.SetActive(false);
    }
}
