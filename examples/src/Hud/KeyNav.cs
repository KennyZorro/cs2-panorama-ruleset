using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace PanoramaRuleset.Examples.Hud;

public enum NavAction
{
    None,
    Up,
    Down,
    Select,
    Back,
}

/// <summary>
/// Keyboard navigation for menus without input capture. Poll once per tick, only for players with an
/// open keyboard menu. Each key fires once per press (rising edge), never while held.
/// </summary>
public sealed class KeyNav
{
    private const PlayerButtons Watched =
        PlayerButtons.Forward | PlayerButtons.Back | PlayerButtons.Moveleft | PlayerButtons.Moveright | PlayerButtons.Use;

    private readonly PlayerButtons[] _held = new PlayerButtons[HudLayout.MaxSlots];

    public NavAction Poll(CCSPlayerController player)
    {
        int slot = player.Slot;
        if ((uint)slot >= HudLayout.MaxSlots)
        {
            return NavAction.None;
        }

        PlayerButtons current = player.PlayerPawn.Value is { IsValid: true } ? player.Buttons & Watched : 0;
        PlayerButtons fresh = current & ~_held[slot];
        _held[slot] = current;

        if (fresh == 0)
        {
            return NavAction.None;
        }

        if ((fresh & PlayerButtons.Forward) != 0)
        {
            return NavAction.Up;
        }

        if ((fresh & PlayerButtons.Back) != 0)
        {
            return NavAction.Down;
        }

        if ((fresh & (PlayerButtons.Moveright | PlayerButtons.Use)) != 0)
        {
            return NavAction.Select;
        }

        return (fresh & PlayerButtons.Moveleft) != 0 ? NavAction.Back : NavAction.None;
    }

    /// <summary>Call on open (ignore keys already held) and on disconnect.</summary>
    public void Reset(CCSPlayerController? player, int slot)
    {
        if ((uint)slot < HudLayout.MaxSlots)
        {
            _held[slot] = player is { IsValid: true } ? player.Buttons & Watched : 0;
        }
    }
}
