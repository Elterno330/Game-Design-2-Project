using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The existing Player, NPCDialog and GameManager remain the world-facing components.
// This director handles the five-day investigation prototype and its session state.
public class MansionPrototype : MonoBehaviour
{
    [Serializable] public class CharacterFact
    {
        public string id, targetId, title, text;
    }
    [Serializable] public class ClueDialogue
    {
        public string saw, heard, found, inferred;
    }
    [Serializable] public class ClueDefinition
    {
        public string id, title, trait, evidence, sound, place, description;
    }
    [Serializable] public class Character
    {
        public string id, displayName, identity;
        public string[] traits, introduction, dailyGreetings, day2, accusation, werewolfAccusation;
        public ClueDialogue clueDialogue;
        public CharacterFact[] facts;
    }
    [Serializable] public class DialogueScript
    {
        public string[] intro, question, interrogation, win, lose;
        public Character[] characters;
        public ClueDefinition[] clues;
    }
    public TextAsset dialogueScript;
    public TMP_FontAsset dialogueFont;
    public Player player;
    public GameManager gameManager;
    public NPCDialog[] npcs;
    public Texture2D[] portraits;
    public Rect[] portraitRegions;
    public Transform[] clueSpots;
    public string[] clueIconIds;
    public Sprite[] clueIcons;
    [Tooltip("Optional Journal tools-clue image. Leave empty to use the temporary tool-bag icon.")]
    public Sprite journalToolsIcon;
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
    public bool CanOpenJournal => !DialogueOpen && !InChoice && !IsTransitioning && !Finished
        && !gameManager.gameOver && !gameManager.choosingSuspect
        && (!cluePopupRoot || !cluePopupRoot.activeSelf);

    readonly HashSet<string> introduced = new HashSet<string>();
    readonly HashSet<string> dayTwoHeard = new HashSet<string>();
    readonly HashSet<string> learnedFactIds = new HashSet<string>();
    readonly HashSet<string> deadIds = new HashSet<string>();
    readonly HashSet<string> usedClueIds = new HashSet<string>();
    readonly HashSet<string> clueKnowerIds = new HashSet<string>();
    readonly List<ClueDefinition> runClues = new List<ClueDefinition>();
    readonly List<GameObject> choices = new List<GameObject>();
    GameObject dialogueRoot, portraitRoot;
    GameObject objectiveRoot;
    TextMeshProUGUI objectiveText;
    // Interrogated tracks the point charge; completion happens after the last line.
    bool interrogationCompleted;
    RectTransform choiceRoot;
    RectTransform dialogueFrame;
    TextMeshProUGUI sentenceText, nameText, dayText, pointsText, promptText, hintText;
    RawImage portraitImage;
    Image portraitBackground;
    RectTransform portraitSlot;
    float portraitRatio = 1f;
    GameObject endingRoot;
    TextMeshProUGUI endingTitle, endingDescription;
    GameObject cluePopupRoot;
    TextMeshProUGUI cluePopupTitle, cluePopupDescription;
    Image cluePopupImage;
    PrototypeClockIcon clockIcon;
    Button accuseButton;
    MansionJournal journal;
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
    bool transitionAdvancedDay;
    string lastVictimId;
    Vector3[] characterSpots;
    ClueDefinition currentClue;
    GameObject clueSparkle;
    Vector3 sparkleBaseScale;
    bool clueRevealed;
    string werewolfId;
    public string WerewolfId => werewolfId;

    const int FinalDay = 5;
    const int ActionsPerDay = 3;

    static readonly Color Ink = new Color(.11f, .10f, .14f);
    static readonly Color Cream = new Color(.96f, .92f, .83f);
    static readonly Color Gold = new Color(.79f, .64f, .38f);
    static readonly Color Plum = new Color(.16f, .13f, .19f);

    void Awake()
    {
        Script = JsonUtility.FromJson<DialogueScript>(dialogueScript.text);
        startingPosition = player.transform.position;
        characterSpots = npcs.Select(npc => npc.transform.position).ToArray();
        BuildUI();
        BuildClueSparkle();
        journal = gameObject.AddComponent<MansionJournal>();
        journal.Initialize(this, canvas.transform);
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
        cluePopupRoot.SetActive(false);
        dayTransition.gameObject.SetActive(false);
        Day = 1;
        Interrogated = EvidenceFound = Finished = false;
        interrogationCompleted = false;
        introduced.Clear(); dayTwoHeard.Clear(); learnedFactIds.Clear(); deadIds.Clear();
        usedClueIds.Clear(); clueKnowerIds.Clear(); clueRevealed = false;
        lastVictimId = null;
        werewolfId = Script.characters[UnityEngine.Random.Range(0, Script.characters.Length)].id;
        BuildRunClueOrder();
        clueSparkle.SetActive(false);
        journal.ResetRun();
        player.GetComponent<Rigidbody>().position = startingPosition;
        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        gameManager.numberOfActions = ActionsPerDay;
        gameManager.gameOver = gameManager.nightTime = gameManager.choosingSuspect = false;
        ShuffleLivingCharacters();
        PrepareDailyClue();
        RefreshNPCLabels();
        RefreshHUD();
        ShowSequence("Inner thoughts", Script.intro, CloseDialogue);
    }

    void Update()
    {
        if (clueSparkle.activeSelf)
        {
            clueSparkle.transform.Rotate(0f, 90f * Time.unscaledDeltaTime, 0f, Space.World);
            clueSparkle.transform.localScale = sparkleBaseScale * (1f + .15f * Mathf.Sin(Time.unscaledTime * 4f));
        }
        if (endingRoot.activeSelf) return;
        if (cluePopupRoot.activeSelf)
        {
            var popupKeys = Keyboard.current;
            bool dismissKey = popupKeys != null && (popupKeys.eKey.wasPressedThisFrame
                || popupKeys.spaceKey.wasPressedThisFrame || popupKeys.enterKey.wasPressedThisFrame
                || popupKeys.escapeKey.wasPressedThisFrame);
            bool dismissClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (dismissKey || dismissClick) CloseCluePopup();
            return;
        }
        if (IsTransitioning)
        {
            TickDayTransition(Time.unscaledDeltaTime);
            return;
        }
        if (journal.BlocksWorldInput) return;
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
        bool clueNear = clueSparkle.activeSelf && (clueSparkle.transform.position - playerPosition).sqrMagnitude <= nearestDistance;
        if (clueNear) nearestDistance = (clueSparkle.transform.position - playerPosition).sqrMagnitude;
        foreach (var npc in npcs)
        {
            if (!npc || !IsAlive(npc.prototypeCharacterId)) continue;
            float distance = (npc.transform.position - playerPosition).sqrMagnitude;
            if (distance <= nearestDistance) { nearest = npc; nearestDistance = distance; }
        }
        if (nearest != promptedNPC || Day != promptDay || clueNear)
        {
            promptText.text = clueNear && !nearest ? "E  Investigate the sparkling clue"
                : nearest ? "E  Talk to " + nearest.prototypeCharacterId
                : "WASD / ARROWS  Walk       E  Talk nearby       F  Accuse from day 2";
            promptedNPC = nearest; promptDay = Day;
        }
        if (keys != null && keys.eKey.wasPressedThisFrame && clueNear && !nearest) CollectClue();
        else if (keys != null && keys.eKey.wasPressedThisFrame && nearest) Interact(nearest.prototypeCharacterId);
        else if (keys != null && keys.fKey.wasPressedThisFrame && Day > 1) OpenAccusation();
    }

    public void Interact(string id)
    {
        if (journal.BlocksWorldInput || DialogueOpen || Finished || IsTransitioning || !IsAlive(id)) return;
        var character = Script.characters.First(c => c.id == id);
        bool firstMeeting = introduced.Add(id);
        string[] opening = firstMeeting ? character.introduction : DailyGreeting(character);
        ShowSequence(id, opening, () =>
        {
            journal.RecordIntroduction(id);
            RefreshHUD();
            ShowConversationMenu(id);
        });
    }

    void ShowConversationMenu(string id)
    {
        bool canSpend = gameManager.numberOfActions > 0;
        if (canSpend)
        {
            ShowChoices(id, "Ask for more information, or leave the conversation.",
                ("Ask more  -  1 time point", () => BeginAskMore(id), true),
                ("Leave", CloseDialogue, true));
        }
        else
        {
            string nextAction = Day < FinalDay ? "End day and continue" : "Make your final accusation [F]";
            ShowChoices(id, Day < FinalDay ? "No time points remain. End the day when you are ready."
                    : "This is the final day. Review your journal and accuse a suspect.",
                ("Ask more  -  no time points remaining", (Action)null, false),
                (nextAction, Day < FinalDay ? (Action)EndDay : OpenAccusation, interrogationCompleted),
                ("Leave", CloseDialogue, true));
        }
    }

    public void BeginAskMore(string id)
    {
        if (journal.BlocksWorldInput || Day < 1 || Day > FinalDay || !IsAlive(id) || gameManager.numberOfActions < 1) return;
        Interrogated = true;
        gameManager.actionUsed();
        RefreshHUD();
        ShowSequence("Hunter", Script.question, () =>
        {
            if (currentClue != null && !clueRevealed && clueKnowerIds.Contains(id))
            {
                var character = Script.characters.First(c => c.id == id);
                ShowSequence(id, new[] { FormatClueLine(character, currentClue) }, () =>
                {
                    RevealClueSparkle();
                    FinishAskMore(id);
                });
                return;
            }

            CharacterFact fact = SelectFact(id);
            string[] response = fact == null
                ? new[] { "I don't have anything else useful to add right now." }
                : new[] { fact.text };
            ShowSequence(id, response, () =>
            {
                if (fact != null)
                {
                    learnedFactIds.Add(fact.id);
                    var speaker = Script.characters.First(c => c.id == id);
                    var target = TargetForFact(fact, speaker);
                    journal.RecordFact(fact, target, TraitFromFact(fact, target), speaker.displayName, Day);
                }
                FinishAskMore(id);
            });
        });
    }

    // Kept for scene events and older tests that invoked the original method.
    public void BeginInterrogation() => BeginAskMore("Elara");

    void FinishAskMore(string id)
    {
        interrogationCompleted = true;
        RefreshHUD();
        ShowConversationMenu(id);
    }

    CharacterFact SelectFact(string speakerId)
    {
        var character = Script.characters.First(c => c.id == speakerId);
        var eligible = (character.facts ?? Array.Empty<CharacterFact>())
            .Where(fact => !learnedFactIds.Contains(fact.id)
                && (string.IsNullOrEmpty(fact.targetId) || IsAlive(fact.targetId))
                && !journal.KnowsTrait(TargetForFact(fact, character).id,
                    TraitFromFact(fact, TargetForFact(fact, character))))
            .ToArray();
        return eligible.Length == 0 ? null : eligible[UnityEngine.Random.Range(0, eligible.Length)];
    }

    Character TargetForFact(CharacterFact fact, Character speaker)
        => string.IsNullOrEmpty(fact.targetId)
            ? speaker : Script.characters.First(character => character.id == fact.targetId);

    static string TraitFromFact(CharacterFact fact, Character target)
    {
        string id = (fact.id ?? "").ToUpperInvariant();
        foreach (string trait in target.traits ?? Array.Empty<string>())
        {
            string token = trait switch
            {
                "AnimalSide" => "ANIMAL_SIDE",
                "HumanSide" => "HUMAN_SIDE",
                "NeedsMoney" => "NEEDS_MONEY",
                "CarryingTooMuch" => "CARRYING_TOO_MUCH",
                "WantsToLeave" => "WANTS_TO_LEAVE",
                "HasToStay" => "HAS_TO_STAY",
                _ => trait.ToUpperInvariant()
            };
            if (id.EndsWith("_" + token) || id.Contains("_" + token + "_")) return trait;
        }
        if (id.Contains("OVERBURDENED") && target.traits.Contains("CarryingTooMuch"))
            return "CarryingTooMuch";
        if (id.Contains("HAS_WANTS_TO_STAY") && target.traits.Contains("HasToStay"))
            return "HasToStay";
        if (id.Contains("CALM_WITHHOLDING") && target.traits.Contains("Withholding"))
            return "Withholding";
        return "Unknown";
    }

    bool IsAlive(string id) => npcs.Any(npc => npc && npc.prototypeCharacterId == id) && !deadIds.Contains(id);

    string[] DailyGreeting(Character character)
    {
        if (character.dailyGreetings != null && character.dailyGreetings.Length > 0)
            return new[] { character.dailyGreetings[Mathf.Clamp(Day - 1, 0, character.dailyGreetings.Length - 1)] };
        return character.introduction ?? Array.Empty<string>();
    }

    public void EndDay()
    {
        if (journal.BlocksWorldInput || IsTransitioning || Day >= FinalDay || !interrogationCompleted || gameManager.numberOfActions > 0) return;
        CloseDialogue();
        IsTransitioning = true;
        transitionAdvancedDay = false;
        player.inputLocked = true;
        gameManager.nightTime = true;
        transitionElapsed = 0f;
        transitionTitle.alpha = 0f;
        dayTransition.alpha = 0f;
        dayTransition.gameObject.SetActive(true);
        RefreshHUD();
    }

    public void BeginDayTwo() => EndDay();

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
        if (!transitionAdvancedDay)
        {
            transitionAdvancedDay = true;
            Day++;
            lastVictimId = SelectNightVictim();
            if (!string.IsNullOrEmpty(lastVictimId)) deadIds.Add(lastVictimId);
            gameManager.nightTime = false;
            gameManager.numberOfActions = ActionsPerDay;
            transitionTitle.text = "Day " + RomanDay(Day);
            ShuffleLivingCharacters();
            PrepareDailyClue();
            RefreshNPCLabels(); RefreshHUD();
            transitionTitle.alpha = 1f;
        }
        dayTransition.alpha = transitionElapsed < holdUntil ? 1f
            : 1f - Mathf.SmoothStep(0f, 1f, (transitionElapsed - holdUntil) / (finishedAt - holdUntil));
        if (transitionElapsed < finishedAt) return;
        dayTransition.gameObject.SetActive(false);
        IsTransitioning = false;
        RefreshObjective();
        string victimName = Script.characters.First(c => c.id == lastVictimId).displayName;
        var morning = new List<string> { "The next morning, " + victimName + " is missing." };
        morning.Add(Day < FinalDay
            ? "You have " + ActionsPerDay + " actions today. Speak to the survivors and decide what information matters."
            : "This is the fifth and final day. Gather what you can, then make your accusation.");
        ShowSequence("Inner thoughts", morning.ToArray(), () =>
        {
            journal.RecordDeath(lastVictimId);
            CloseDialogue();
        });
    }

    string SelectNightVictim()
    {
        var candidates = npcs.Where(npc => npc && IsAlive(npc.prototypeCharacterId)
                && npc.prototypeCharacterId != werewolfId)
            .Select(npc => npc.prototypeCharacterId).ToArray();
        return candidates.Length == 0 ? null : candidates[UnityEngine.Random.Range(0, candidates.Length)];
    }

    void ShuffleLivingCharacters()
    {
        if (characterSpots == null || characterSpots.Length == 0) return;
        var available = characterSpots.ToList();
        for (int i = available.Count - 1; i > 0; i--)
        {
            int swap = UnityEngine.Random.Range(0, i + 1);
            Vector3 temporary = available[i];
            available[i] = available[swap];
            available[swap] = temporary;
        }
        int spotIndex = 0;
        foreach (var npc in npcs.Where(npc => npc && IsAlive(npc.prototypeCharacterId)))
        {
            if (spotIndex >= available.Count) break;
            npc.transform.position = available[spotIndex++];
        }
        Physics.SyncTransforms();
        promptedNPC = nearest = null;
        promptDay = -1;
    }

    void PrepareDailyClue()
    {
        clueRevealed = false;
        clueSparkle.SetActive(false);
        clueKnowerIds.Clear();
        if (Day == 1)
        {
            currentClue = null;
            return;
        }
        int clueIndex = Day - 2;
        currentClue = clueIndex >= 0 && clueIndex < runClues.Count ? runClues[clueIndex] : null;
        if (currentClue == null) return;
        usedClueIds.Add(currentClue.id);
        foreach (string id in npcs.Where(npc => npc && IsAlive(npc.prototypeCharacterId) && npc.prototypeCharacterId != werewolfId)
                     .OrderBy(_ => UnityEngine.Random.value).Take(3).Select(npc => npc.prototypeCharacterId))
            clueKnowerIds.Add(id);
    }

    void BuildRunClueOrder()
    {
        runClues.Clear();
        var werewolf = Script.characters.First(character => character.id == werewolfId);
        foreach (string trait in werewolf.traits.Distinct())
        {
            var matches = (Script.clues ?? Array.Empty<ClueDefinition>())
                .Where(clue => clue.trait == trait).ToArray();
            if (matches.Length > 0)
                runClues.Add(matches[UnityEngine.Random.Range(0, matches.Length)]);
            else
                Debug.LogWarning("No clue is authored for werewolf trait: " + trait);
        }
        for (int i = runClues.Count - 1; i > 0; i--)
        {
            int swap = UnityEngine.Random.Range(0, i + 1);
            (runClues[i], runClues[swap]) = (runClues[swap], runClues[i]);
        }
    }

    string FormatClueLine(Character character, ClueDefinition clue)
    {
        string[] templates = { character.clueDialogue?.saw, character.clueDialogue?.heard,
            character.clueDialogue?.found, character.clueDialogue?.inferred };
        var usable = templates.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        string line = usable.Length == 0 ? "I noticed something unusual near " + clue.place + "." : usable[UnityEngine.Random.Range(0, usable.Length)];
        return line.Replace("[evidence]", clue.evidence).Replace("[sound]", clue.sound)
            .Replace("[sound or detail]", clue.sound).Replace("[detail]", clue.evidence)
            .Replace("[place]", clue.place).Replace("[other detail]", clue.evidence);
    }

    void RevealClueSparkle()
    {
        clueRevealed = true;
        // Only the first successful clue testimony is delivered. Everyone else
        // who was selected as a knower now falls back to character information.
        clueKnowerIds.Clear();
        Transform spot = clueSpots != null && clueSpots.Length > 0
            ? clueSpots[UnityEngine.Random.Range(0, clueSpots.Length)] : null;
        clueSparkle.transform.position = spot ? spot.position : player.transform.position + Vector3.forward * 2f;
        clueSparkle.SetActive(true);
        RefreshObjective();
    }

    void CollectClue()
    {
        if (!clueSparkle.activeSelf || currentClue == null) return;
        clueSparkle.SetActive(false);
        EvidenceFound = true;
        journal.RecordGeneratedClue(currentClue, Day);
        ShowCluePopup(currentClue);
        RefreshHUD();
    }

    void ShowCluePopup(ClueDefinition clue)
    {
        player.inputLocked = true;
        cluePopupTitle.text = "Clue Found: " + clue.title;
        // The discovery popup, like the Clues tab, presents only the physical
        // evidence. Trait knowledge is reserved for character profiles.
        cluePopupDescription.text = clue.description;
        cluePopupImage.sprite = TryGetClueIcon("evidence:" + clue.id, out var icon) ? icon : null;
        cluePopupImage.enabled = cluePopupImage.sprite;
        cluePopupRoot.SetActive(true);
        cluePopupRoot.transform.SetAsLastSibling();
        journal.RefreshAvailability();
    }

    void CloseCluePopup()
    {
        cluePopupRoot.SetActive(false);
        player.inputLocked = false;
        RefreshHUD();
    }

    void BuildClueSparkle()
    {
        clueSparkle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        clueSparkle.name = "Revealed Clue Sparkle";
        clueSparkle.transform.SetParent(transform, false);
        clueSparkle.transform.localScale = sparkleBaseScale = Vector3.one * .38f;
        var collider = clueSparkle.GetComponent<Collider>();
        if (collider) collider.enabled = false;
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = new Color(1f, .82f, .18f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(1f, .62f, .08f) * 3f);
        clueSparkle.GetComponent<Renderer>().material = material;
        var light = clueSparkle.AddComponent<Light>();
        light.type = LightType.Point; light.color = new Color(1f, .7f, .15f); light.range = 3f; light.intensity = 2f;
        clueSparkle.SetActive(false);
    }

    static string RomanDay(int day)
    {
        string[] values = { "", "I", "II", "III", "IV", "V" };
        return values[Mathf.Clamp(day, 1, FinalDay)];
    }

    public void OpenAccusation()
    {
        if (journal.BlocksWorldInput || Day <= 1 || Finished || IsTransitioning) return;
        if (!EvidenceFound)
        {
            ShowSequence("Hunter", new[] { "I need at least one solid clue before I accuse anyone." }, CloseDialogue);
            return;
        }
        OpenAccusationPage(0);
    }

    void OpenAccusationPage(int page)
    {
        var suspects = Script.characters.Where(character => IsAlive(character.id)).ToArray();
        const int pageSize = 3;
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(suspects.Length / (float)pageSize));
        page = Mathf.Clamp(page, 0, pageCount - 1);
        var options = new List<(string text, Action action, bool enabled)>();
        foreach (var suspect in suspects.Skip(page * pageSize).Take(pageSize))
        {
            string id = suspect.id;
            options.Add((suspect.displayName, () => ConfirmAccusation(id), true));
        }
        if (page > 0) options.Add(("Previous suspects", () => OpenAccusationPage(page - 1), true));
        if (page + 1 < pageCount) options.Add(("More suspects", () => OpenAccusationPage(page + 1), true));
        if (pageCount == 1 || options.Count < 5) options.Add(("Keep investigating", CloseDialogue, true));
        ShowChoices("Hunter", "Who do you accuse? Suspects " + (page * pageSize + 1) + "-" + Mathf.Min(suspects.Length, (page + 1) * pageSize)
            + " of " + suspects.Length + ".", options.ToArray());
    }

    public void ConfirmAccusation(string id)
    {
        if (journal.BlocksWorldInput || Day <= 1 || !EvidenceFound || Finished || !IsAlive(id)) return;
        ShowChoices(id, "Accuse " + Script.characters.First(c => c.id == id).displayName + "? This choice ends the investigation.",
            ("Confirm accusation", () => Accuse(id), true), ("Go back", OpenAccusation, true));
    }

    public void Accuse(string id)
    {
        if (journal.BlocksWorldInput || Day <= 1 || !EvidenceFound || Finished || !IsAlive(id)) return;
        var character = Script.characters.First(c => c.id == id);
        bool won = id == werewolfId;
        string[] authoredResponse = won ? character.werewolfAccusation : character.accusation;
        string[] response = authoredResponse != null && authoredResponse.Length > 0
            ? authoredResponse
            : new[] { "You're making a terrible mistake. I'm not the creature you're hunting." };
        ShowSequence(id, response, () =>
        {
            gameManager.checkAccusation(id, werewolfId);
            Finished = true;
            string[] ending = (won ? Script.win : Script.lose).Select(s => s
                .Replace("[Sebes]", character.displayName).Replace("[NAME]", character.displayName)).ToArray();
            ShowSequence("Hunter", ending, () => ShowEnding(won));
            RefreshHUD();
        });
    }

    public void Advance()
    {
        if (journal.BlocksWorldInput || IsTransitioning || InChoice || sentences == null || !DialogueOpen) return;
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
        if (journal.BlocksWorldInput || IsTransitioning || !InChoice || index < 0 || index >= choices.Count) return;
        var button = choices[index].GetComponent<Button>();
        if (button.interactable) button.onClick.Invoke();
    }

    void ShowSequence(string who, string[] text, Action complete)
    {
        ClearChoices(); InChoice = false;
        dialogueFrame.anchorMax = new Vector2(.965f, .32f);
        dialogueRoot.SetActive(true); player.inputLocked = true;
        journal.RefreshAvailability();
        speaker = who; sentences = DialogueBeats(text); sentenceIndex = 0; onComplete = complete;
        canAdvanceAt = Time.unscaledTime + .12f;
        DisplaySentence();
    }

    static string[] DialogueBeats(IEnumerable<string> blocks)
    {
        var beats = new List<string>();
        foreach (string block in blocks ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(block)) continue;
            foreach (Match match in Regex.Matches(block.Trim(), @".+?(?:[.!?…]+(?=\s|$)|$)"))
            {
                string beat = match.Value.Trim();
                if (!string.IsNullOrEmpty(beat)) beats.Add(beat);
            }
        }
        return beats.Count > 0 ? beats.ToArray() : new[] { "..." };
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
        journal.RefreshAvailability();
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
        int characterIndex = Array.FindIndex(Script.characters, c => c.id == id);
        int portraitIndex = Array.FindIndex(npcs, npc => npc && npc.prototypeCharacterId == id);
        nameText.text = characterIndex < 0 ? (id == "Hunter" ? "HUNTER" : "HUNTER  /  INNER THOUGHTS") : Script.characters[characterIndex].displayName.ToUpperInvariant();
        sentenceText.fontStyle = id == "Inner thoughts" ? FontStyles.Italic : FontStyles.Normal;
        portraitRoot.SetActive(portraitIndex >= 0);
        if (portraitIndex >= 0)
        {
            portraitImage.texture = portraits[portraitIndex];
            // Restore the original upper-body framing, without stretching it.
            Rect region = portraitRegions[portraitIndex];
            portraitImage.uvRect = region;
            portraitRatio = portraits[portraitIndex].width * region.width / (portraits[portraitIndex].height * region.height);
            portraitBackground.color = portraits[portraitIndex].isReadable ? portraits[portraitIndex].GetPixel(0, 0) : Plum;
        }
    }

    public bool TryGetPortrait(string id, out Texture2D texture, out Rect region)
    {
        texture = null;
        region = new Rect(0, 0, 1, 1);
        int index = Array.FindIndex(npcs, npc => npc && npc.prototypeCharacterId == id);
        if (index < 0 || portraits == null || index >= portraits.Length || !portraits[index]) return false;
        texture = portraits[index];
        if (portraitRegions != null && index < portraitRegions.Length && portraitRegions[index].width > 0 && portraitRegions[index].height > 0)
            region = portraitRegions[index];
        return true;
    }

    public bool TryGetClueIcon(string journalId, out Sprite sprite)
    {
        sprite = null;
        string id = journalId != null && journalId.StartsWith("evidence:")
            ? journalId.Substring("evidence:".Length) : journalId;
        int index = clueIconIds == null ? -1 : Array.IndexOf(clueIconIds, id);
        if (index < 0 || clueIcons == null || index >= clueIcons.Length || !clueIcons[index]) return false;
        sprite = clueIcons[index];
        return true;
    }

    void RefreshHUD()
    {
        dayText.text = "Day " + RomanDay(Day);
        pointsText.text = gameManager.numberOfActions.ToString();
        clockIcon.color = gameManager.numberOfActions > 0 ? Gold : new Color(.45f, .43f, .42f);
        accuseButton.interactable = Day > 1 && !Finished && !DialogueOpen && !IsTransitioning;
        RefreshObjective();
        journal.RefreshAvailability();
    }

    void RefreshObjective()
    {
        objectiveRoot.SetActive(!IsTransitioning && !Finished);
        if (IsTransitioning || Finished) return;

        // Later milestones take priority, so repeat conversations cannot regress the goal.
        if (clueSparkle.activeSelf)
            objectiveText.text = "Find and investigate the sparkling clue.";
        else if (gameManager.numberOfActions == 0 && Day == FinalDay)
            objectiveText.text = "Make your final accusation [F].";
        else if (interrogationCompleted && gameManager.numberOfActions == 0)
            objectiveText.text = "End the day from any survivor\u2019s conversation.";
        else if (introduced.Count > 0)
            objectiveText.text = "Ask guests for more information (" + gameManager.numberOfActions + " actions remaining).";
        else
            objectiveText.text = "Meet the guests, then ask for more information.";
    }

    void RefreshNPCLabels()
    {
        foreach (var npc in npcs)
        {
            var label = npc.GetComponentInChildren<TextMeshPro>();
            bool alive = IsAlive(npc.prototypeCharacterId);
            npc.gameObject.SetActive(alive);
            if (label) label.text = npc.prototypeCharacterId.ToUpperInvariant();
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
        if (action != null) button.onClick.AddListener(() =>
        {
            if (!journal || !journal.BlocksWorldInput) action();
        });
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
        var objective = Panel(root.transform, "Current Goal", new Vector2(.025f, .70f), new Vector2(.30f, .885f), new Color(.06f, .055f, .08f, .94f));
        objectiveRoot = objective.gameObject;
        objective.raycastTarget = false;
        Text(objective.transform, "Title", "Current Goal", new Vector2(.04f, .72f), new Vector2(.96f, .96f), 23, Cream);
        objectiveText = Text(objective.transform, "Objective", "", new Vector2(.04f, .06f), new Vector2(.96f, .69f), 22, Cream);
        // The static dialogue atlas lacks the em dash and curly double quotes.
        objectiveText.font = TMP_Settings.defaultFontAsset;
        objectiveText.alignment = TextAlignmentOptions.TopLeft;
        objectiveText.textWrappingMode = TextWrappingModes.Normal;
        objectiveText.enableAutoSizing = true;
        objectiveText.fontSizeMin = 16;
        objectiveText.fontSizeMax = 22;
        objectiveText.overflowMode = TextOverflowModes.Truncate;
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
        var clueModal = Panel(root.transform, "Clue Discovery Modal", Vector2.zero, Vector2.one,
            new Color(.035f, .025f, .045f, .68f));
        cluePopupRoot = clueModal.gameObject;
        var dismissBackdrop = clueModal.gameObject.AddComponent<Button>();
        dismissBackdrop.targetGraphic = clueModal;
        dismissBackdrop.onClick.AddListener(CloseCluePopup);
        var clueBorder = Panel(clueModal.transform, "Clue Border", new Vector2(.30f, .25f), new Vector2(.70f, .75f), Gold);
        var clueCard = Panel(clueBorder.transform, "Clue Card", new Vector2(.008f, .008f), new Vector2(.992f, .992f), Cream);
        cluePopupTitle = Text(clueCard.transform, "Clue Title", "", new Vector2(.08f, .77f), new Vector2(.92f, .93f), 34, Plum);
        cluePopupTitle.alignment = TextAlignmentOptions.Center;
        var clueImageFrame = Panel(clueCard.transform, "Clue Image Frame", new Vector2(.08f, .31f), new Vector2(.36f, .72f), Plum);
        var clueImageArea = Rect(clueImageFrame.transform, "Clue Image", new Vector2(.025f, .025f), new Vector2(.975f, .975f));
        cluePopupImage = clueImageArea.gameObject.AddComponent<Image>();
        cluePopupImage.preserveAspect = true;
        cluePopupImage.raycastTarget = false;
        cluePopupDescription = Text(clueCard.transform, "Clue Description", "", new Vector2(.41f, .31f), new Vector2(.92f, .72f), 24, Ink);
        cluePopupDescription.alignment = TextAlignmentOptions.TopLeft;
        cluePopupDescription.enableAutoSizing = true;
        cluePopupDescription.fontSizeMin = 18;
        cluePopupDescription.fontSizeMax = 24;
        var clueContinueArea = Rect(clueCard.transform, "Continue Area", new Vector2(.32f, .09f), new Vector2(.68f, .22f));
        var clueContinue = MakeButton(clueContinueArea, "Continue", "Continue", CloseCluePopup);
        clueContinue.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        cluePopupRoot.SetActive(false);
        var fade = Panel(root.transform, "Day Transition", Vector2.zero, Vector2.one, Color.black);
        dayTransition = fade.gameObject.AddComponent<CanvasGroup>();
        dayTransition.blocksRaycasts = true;
        transitionTitle = Text(fade.transform, "Next Day", "Day II", new Vector2(.2f, .4f), new Vector2(.8f, .6f), 54, Cream);
        transitionTitle.alignment = TextAlignmentOptions.Center;
        dayTransition.gameObject.SetActive(false);
    }
}
