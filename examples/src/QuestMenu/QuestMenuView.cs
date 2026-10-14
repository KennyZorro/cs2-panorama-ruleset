using CounterStrikeSharp.API.Core;
using PanoramaRuleset.Examples.Hud;

namespace PanoramaRuleset.Examples.QuestMenu;

/// <summary>Paints the quest menu from a model. Knows ids only through <see cref="QuestMenuIds"/>.</summary>
public sealed class QuestMenuView
{
    private readonly HudLayout _hud;
    private readonly TextSlot _title;
    private readonly TextSlot _empty;
    private readonly TextSlot _page;
    private readonly ClassSlot _prevDisabled;
    private readonly ClassSlot _nextDisabled;
    private readonly ClassSlot _emptyOff;
    private readonly TextSlot[] _tabLabel = new TextSlot[QuestMenuIds.Tabs];
    private readonly ClassSlot[] _tabActive = new ClassSlot[QuestMenuIds.Tabs];
    private readonly ClassSlot[] _rowOff = new ClassSlot[QuestMenuIds.Rows];
    private readonly ClassSlot[] _rowTone = new ClassSlot[QuestMenuIds.Rows];
    private readonly ClassSlot[] _rowFill = new ClassSlot[QuestMenuIds.Rows];
    private readonly TextSlot[] _rowName = new TextSlot[QuestMenuIds.Rows];
    private readonly TextSlot[] _rowValue = new TextSlot[QuestMenuIds.Rows];

    private readonly int[] _pageOf = new int[HudLayout.MaxSlots];
    private readonly int[] _tabOf = new int[HudLayout.MaxSlots];
    private readonly bool[] _dirty = new bool[HudLayout.MaxSlots];
    private readonly Func<int, int, IReadOnlyList<QuestRow>> _rows;

    /// <param name="rows">(slot, tab) → rows. Must be cheap: called only for dirty, visible players.</param>
    public QuestMenuView(Func<int, int, IReadOnlyList<QuestRow>> rows)
    {
        _rows = rows;
        _hud = new HudLayout(QuestMenuIds.Resource, QuestMenuIds.Root);
        _title = _hud.Text(QuestMenuIds.Root, "qm_title");
        _empty = _hud.Text(QuestMenuIds.Root, "qm_empty");
        _page = _hud.Text(QuestMenuIds.Root, "qm_page");
        _prevDisabled = _hud.Flag(QuestMenuIds.Prev, "is-disabled", authoredOn: true);
        _nextDisabled = _hud.Flag(QuestMenuIds.Next, "is-disabled", authoredOn: true);
        _emptyOff = _hud.Flag(QuestMenuIds.Empty, "is-off", authoredOn: true);
        for (int t = 0; t < QuestMenuIds.Tabs; t++)
        {
            _tabActive[t] = _hud.Flag(QuestMenuIds.TabIds[t], "is-active", authoredOn: t == 0);
            _tabLabel[t] = _hud.Text(QuestMenuIds.Root, "qm_tab_" + t);
        }

        for (int r = 0; r < QuestMenuIds.Rows; r++)
        {
            _rowOff[r] = _hud.Flag(QuestMenuIds.RowIds[r], "is-off", authoredOn: true);
            _rowTone[r] = _hud.Group(QuestMenuIds.RowIds[r], QuestMenuIds.Tones, authored: 0);
            _rowFill[r] = _hud.Ladder(QuestMenuIds.FillIds[r], "p", step: 10);
            _rowName[r] = _hud.Text(QuestMenuIds.Root, "qm_r" + r + "_name");
            _rowValue[r] = _hud.Text(QuestMenuIds.Root, "qm_r" + r + "_value");
        }
    }

    public HudLayout Hud => _hud;

    public void MarkDirty(int slot)
    {
        if ((uint)slot < HudLayout.MaxSlots)
        {
            _dirty[slot] = true;
        }
    }

    public void Open(CCSPlayerController player, QuestMenuTexts texts)
    {
        _pageOf[player.Slot] = 0;
        _hud.SetText(player, _title, texts.Title);
        _hud.SetText(player, _empty, texts.Empty);
        for (int t = 0; t < QuestMenuIds.Tabs; t++)
        {
            _hud.SetText(player, _tabLabel[t], texts.Tabs[t]);
        }

        _dirty[player.Slot] = true;
        Paint(player);
        _hud.Show(player);
    }

    /// <summary>Scheduler entry: early exit unless shown and dirty.</summary>
    public void PaintIfDirty(CCSPlayerController player)
    {
        int slot = player.Slot;
        if (!_hud.IsVisible(slot) || !_dirty[slot])
        {
            return;
        }

        Paint(player);
    }

    /// <summary>Click on this layout. Returns true when handled.</summary>
    public bool OnButton(CCSPlayerController player, string id)
    {
        int slot = player.Slot;
        int tab = Array.IndexOf(QuestMenuIds.TabIds, id);
        if (id == QuestMenuIds.Prev || id == QuestMenuIds.Next)
        {
            _pageOf[slot] += id == QuestMenuIds.Next ? 1 : -1;
        }
        else if (tab >= 0)
        {
            _tabOf[slot] = tab;
            _pageOf[slot] = 0;
        }
        else
        {
            return false;
        }

        // Feedback within the same tick: paint now, not on the next scheduler pass.
        _dirty[slot] = true;
        Paint(player);
        return true;
    }

    private void Paint(CCSPlayerController player)
    {
        int slot = player.Slot;
        _dirty[slot] = false;
        IReadOnlyList<QuestRow> rows = _rows(slot, _tabOf[slot]);
        int pages = Math.Max(1, (rows.Count + QuestMenuIds.Rows - 1) / QuestMenuIds.Rows);
        int page = _pageOf[slot] = Math.Clamp(_pageOf[slot], 0, pages - 1);

        _hud.SetExclusive(player, _tabActive, _tabOf[slot]);
        _hud.SetFlag(player, _prevDisabled, page == 0);
        _hud.SetFlag(player, _nextDisabled, page >= pages - 1);
        _hud.SetText(player, _page, NumberText.Of(page + 1) + " / " + NumberText.Of(pages));
        _hud.SetFlag(player, _emptyOff, rows.Count != 0);

        for (int r = 0; r < QuestMenuIds.Rows; r++)
        {
            int i = page * QuestMenuIds.Rows + r;
            if (i >= rows.Count)
            {
                // Hide only; stale text under is-off is invisible and saves writes.
                _hud.SetFlag(player, _rowOff[r], true);
                continue;
            }

            QuestRow row = rows[i];
            _hud.SetText(player, _rowName[r], row.Name);
            _hud.SetText(player, _rowValue[r], NumberText.Of(row.Done) + " / " + NumberText.Of(row.Total));
            _hud.SetPercent(player, _rowFill[r], row.Total == 0 ? 0 : row.Done * 100 / row.Total);
            _hud.SetGroup(player, _rowTone[r], row.Tone);
            _hud.SetFlag(player, _rowOff[r], false);
        }
    }
}
