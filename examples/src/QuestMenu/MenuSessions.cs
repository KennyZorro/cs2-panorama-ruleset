using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using PanoramaRuleset.Examples.Hud;

namespace PanoramaRuleset.Examples.QuestMenu;

/// <summary>
/// Mouse menu session: opens one timer after the command, captures input, routes clicks, closes.
/// Pending opens are cancelled by a token, never by killing the native timer.
/// </summary>
public sealed class MenuSessions
{
    private const float OpenDelaySeconds = 0.2f;

    private readonly BasePlugin _plugin;
    private readonly QuestMenuView _view;
    private readonly int[] _token = new int[HudLayout.MaxSlots];
    private readonly bool[] _open = new bool[HudLayout.MaxSlots];

    public MenuSessions(BasePlugin plugin, QuestMenuView view)
    {
        _plugin = plugin;
        _view = view;
    }

    public bool IsOpen(int slot) => (uint)slot < HudLayout.MaxSlots && _open[slot];

    /// <summary>Call from a chat command. The open runs on a timer, never in the command's tick.</summary>
    public void RequestOpen(CCSPlayerController player, QuestMenuTexts texts)
    {
        int slot = player.Slot;
        if ((uint)slot >= HudLayout.MaxSlots)
        {
            return;
        }

        int token = ++_token[slot];
        _plugin.AddTimer(OpenDelaySeconds, () =>
        {
            if (_token[slot] != token || Utilities.GetPlayerFromSlot(slot) is not { IsValid: true } live
                || !_view.Hud.EnsureEntity())
            {
                return;
            }

            _open[slot] = true;
            _view.Open(live, texts);
            _view.Hud.SetInputCapture(live, true);
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    public void Close(CCSPlayerController player)
    {
        int slot = player.Slot;
        if ((uint)slot >= HudLayout.MaxSlots)
        {
            return;
        }

        _token[slot]++; // a pending open sees a stale token and does nothing
        if (!_open[slot])
        {
            return;
        }

        _open[slot] = false;
        _view.Hud.SetInputCapture(player, false);
        _view.Hud.Hide(player);
    }

    /// <summary>Listener OnCustomHudClicked.</summary>
    public void OnClicked(CCSPlayerController player, CCSCustomHudLayout layout, string buttonId)
    {
        if (player is not { IsValid: true } || !IsOpen(player.Slot) || !_view.Hud.Owns(layout))
        {
            return;
        }

        if (buttonId == QuestMenuIds.Close)
        {
            Close(player);
            return;
        }

        _view.OnButton(player, buttonId);
    }

    public void Forget(int slot)
    {
        if ((uint)slot < HudLayout.MaxSlots)
        {
            _token[slot]++;
            _open[slot] = false;
        }
    }
}
