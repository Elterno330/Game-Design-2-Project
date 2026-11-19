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
    readonly HashSet<string> deadCharacters = new HashSet<string>();
    readonly Dictionary<string, HashSet<string>> characterTraits = new Dictionary<string, HashSet<string>>();
    readonly HashSet<string> clueIds = new HashSet<string>();
    readonly List<Clue> clues = new List<Clue>();
    public IReadOnlyList<Clue> Clues => clues;
    public bool HaroldDeathKnown => deadCharacters.Contains("Harold");
    public bool KnowsCharacter(string id) => characters.Contains(id);
    public bool IsDead(string id) => deadCharacters.Contains(id);
    public bool KnowsTrait(string id, string trait)
        => characterTraits.TryGetValue(id, out var traits) && traits.Contains(trait);
    public IReadOnlyCollection<string> LearnedTraits(string id)
        => characterTraits.TryGetValue(id, out var traits) ? traits : (IReadOnlyCollection<string>)new string[0];
    public bool RecordIntroduction(string id) => characters.Add(id);
    public void RecordDeath(string id) => deadCharacters.Add(id);
    public void RecordHaroldDeath() => RecordDeath("Harold");

    public bool RecordBagInspection() => AddClue("tools",
        "Missing Clockmaking Tools", "Harold's bag - Day II", "Physical evidence",
        "Harold's bag had been emptied, and valuable clockmaking tools and parts were missing.");

    public bool RecordFact(string targetId, string trait)
    {
        if (!characterTraits.TryGetValue(targetId, out var traits))
            characterTraits[targetId] = traits = new HashSet<string>();
        // Character knowledge belongs exclusively to the Characters tab. It is
        // deliberately not duplicated as a clue-card entry.
        return traits.Add(trait);
    }

    public bool RecordGeneratedClue(string id, string title, string description, int day) => AddClue(
        "evidence:" + id, title, "Mansion - Day " + day,
        "Physical evidence", description);

    bool AddClue(string id, string title, string source, string kind, string description)
    {
        if (!clueIds.Add(id)) return false;
        clues.Add(new Clue(id, title, source, kind, description));
        return true;
    }

    public void Reset()
    {
        characters.Clear(); deadCharacters.Clear(); characterTraits.Clear(); clueIds.Clear(); clues.Clear();
    }
}
