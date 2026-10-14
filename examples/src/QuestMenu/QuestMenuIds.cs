namespace PanoramaRuleset.Examples.QuestMenu;

/// <summary>Ids, variable names and classes of quest_menu_v1.xml. The only place they are spelled.</summary>
public static class QuestMenuIds
{
    public const string Resource = "panorama/layout/custom_game/example/quest_menu_v1.vxml_c";
    public const string Root = "qm-root";
    public const string Close = "qm-close";
    public const string Prev = "qm-prev";
    public const string Next = "qm-next";
    public const string Empty = "qm-empty";
    public const int Rows = 4;
    public const int Tabs = 3;

    public static readonly string[] TabIds = Indexed("qm-tab-", Tabs);
    public static readonly string[] RowIds = Indexed("qm-row-", Rows);
    public static readonly string[] FillIds = Indexed("qm-fill-", Rows);
    public static readonly string[] Tones = ["tone-neutral", "tone-success", "tone-warn", "tone-danger"];

    private static string[] Indexed(string prefix, int count)
    {
        string[] ids = new string[count];
        for (int i = 0; i < count; i++)
        {
            ids[i] = prefix + i;
        }

        return ids;
    }
}

/// <summary>One list entry. <see cref="Tone"/> indexes <see cref="QuestMenuIds.Tones"/>.</summary>
public readonly record struct QuestRow(string Name, int Done, int Total, int Tone);

/// <summary>Localized static texts, resolved per player before opening.</summary>
public sealed record QuestMenuTexts(string Title, string Empty, string[] Tabs);
