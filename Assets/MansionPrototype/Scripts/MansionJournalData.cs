using System.Collections.Generic;

// Session-only discoveries. No Unity dependency, persistence, or deductions about suspects.
public sealed class MansionJournalData
{
    public sealed class Clue
    {
        public string Id { get; }
        public string Title { get; }
        public string Source { get; }
        public string Kind { get; }
        public string Description { get; }

        public Clue(string id, string title, string source, string kind, string description)
        {
            Id = id; Title = title; Source = source; Kind = kind; Description = description;
        }
    }

    readonly HashSet<string> characters = new HashSet<string>();
    readonly HashSet<string> clueIds = new HashSet<string>();
    readonly List<Clue> clues = new List<Clue>();
    public IReadOnlyList<Clue> Clues => clues;
    public bool HaroldDeathKnown { get; private set; }
    public bool KnowsCharacter(string id) => characters.Contains(id);
    public bool RecordIntroduction(string id) => characters.Add(id);
    public void RecordHaroldDeath() => HaroldDeathKnown = true;

    public bool RecordInterrogation() => AddClue("safe",
        "Attempted Safe Break-in", "Elara Venn - Day I", "Character testimony",
        "Elara reported that Daisy tried to pick the master's safe.\n" +
        "Sebes offered to help in exchange for a share of its contents.\n" +
        "They failed to open it.");

    public bool RecordBagInspection() => AddClue("tools",
        "Missing Clockmaking Tools", "Harold's bag - Day II", "Scene discovery",
        "Harold's bag had been emptied, and valuable clockmaking tools and parts were missing.");

    bool AddClue(string id, string title, string source, string kind, string description)
    {
        if (!clueIds.Add(id)) return false;
        clues.Add(new Clue(id, title, source, kind, description));
        return true;
    }

    public void Reset()
    {
        characters.Clear(); clueIds.Clear(); clues.Clear();
        HaroldDeathKnown = false;
    }
}
