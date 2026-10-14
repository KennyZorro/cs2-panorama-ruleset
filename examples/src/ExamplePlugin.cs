using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using PanoramaRuleset.Examples.QuestMenu;

namespace PanoramaRuleset.Examples;

/// <summary>Wiring: lifecycle hooks, transmit filter, click routing and a staggered refresh budget.</summary>
public sealed class ExamplePlugin : BasePlugin
{
    /// <summary>Refresh budget: players repainted per second across the server.</summary>
    private const int RefreshPerSecond = 128;

    private static readonly QuestMenuTexts Texts = new("QUESTS", "Nothing here yet", ["ALL", "OPEN", "DONE"]);

    private readonly List<CCSPlayerController> _roster = [];
    private QuestMenuView _menu = null!;
    private MenuSessions _sessions = null!;
    private uint _tick;
    private int _ticksPerSecond = 64;

    public override string ModuleName => "Panorama ruleset example";
    public override string ModuleVersion => "1.0.0";

    public override void Load(bool hotReload)
    {
        _menu = new QuestMenuView((slot, tab) => []);
        _sessions = new MenuSessions(this, _menu);

        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterListener<Listeners.CheckTransmit>(_menu.Hud.FilterTransmit);
        RegisterListener<Listeners.OnCustomHudClicked>(_sessions.OnClicked);
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            _menu.Hud.ResetForMap();
            _tick = 0;
            _ticksPerSecond = Math.Max(1, (int)Math.Round(1.0 / Server.TickInterval));
            // Warm-up: spawn the entity a moment into the map, never inside a client command.
            AddTimer(1.0f, () => _menu.Hud.EnsureEntity());
        });
        RegisterListener<Listeners.OnClientDisconnect>(slot =>
        {
            _menu.Hud.Forget(slot);
            _sessions.Forget(slot);
        });
        RegisterEventHandler<EventRoundStart>((_, _) =>
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                _sessions.Close(player);
                _menu.Hud.RestoreDefaults(player);
            }

            return HookResult.Continue;
        });
    }

    public override void Unload(bool hotReload) => _menu.Hud.Shutdown();

    [ConsoleCommand("css_quests", "Toggle the quest menu")]
    public void OnQuests(CCSPlayerController? player, CommandInfo info)
    {
        if (player is not { IsValid: true, IsBot: false })
        {
            return;
        }

        if (_sessions.IsOpen(player.Slot))
        {
            _sessions.Close(player);
        }
        else
        {
            _sessions.RequestOpen(player, Texts);
        }
    }

    /// <summary>Roster once per second, then this tick's share of players. Painters exit early unless dirty.</summary>
    private void OnTick()
    {
        _menu.Hud.Tick();
        uint tick = _tick++;
        if (tick % (uint)_ticksPerSecond == 0)
        {
            _roster.Clear();
            foreach (CCSPlayerController p in Utilities.GetPlayers())
            {
                if (p is { IsBot: false, IsHLTV: false })
                {
                    _roster.Add(p);
                }
            }
        }

        int perTick = Math.Max(1, RefreshPerSecond / _ticksPerSecond);
        int gap = Math.Max(1, (_roster.Count + perTick - 1) / perTick);
        for (int i = (int)(tick % (uint)gap); i < _roster.Count; i += gap)
        {
            CCSPlayerController player = _roster[i];
            if (player.IsValid)
            {
                _menu.PaintIfDirty(player);
            }
        }
    }
}
