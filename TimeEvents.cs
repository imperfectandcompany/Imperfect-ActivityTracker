
namespace K4System
{
	using CounterStrikeSharp.API.Core;
	using CounterStrikeSharp.API.Modules.Utils;

	public partial class ModuleTime : IModuleTime
	{
		public void Initialize_Events(Plugin plugin)
		{
			plugin.RegisterEventHandler((EventPlayerSpawn @event, GameEventInfo info) =>
			{
				CCSPlayerController player = @event.Userid;

				if (player is null || !player.IsValid || !player.PlayerPawn.IsValid)
					return HookResult.Continue;

				if (player.IsBot || player.IsHLTV)
					return HookResult.Continue;

				if (!PlayerCache.Instance.ContainsPlayer(player))
					return HookResult.Continue;

				TimeData ? playerData = PlayerCache.Instance.GetPlayerData(player).timeData;

				if (playerData is null)
					return HookResult.Continue;
				playerData.Times["deaths"] = DateTime.UtcNow;

				return HookResult.Continue;
			});

		}
	}
}