namespace K4System
{
	using CounterStrikeSharp.API.Core;
	using CounterStrikeSharp.API.Modules.Commands;
	using CounterStrikeSharp.API.Modules.Utils;

	public partial class ModuleTime : IModuleTime
	{
		public void Initialize_Commands(Plugin plugin)
		{
			CommandSettings commands = Config.CommandSettings;

			commands.TimeCommands.ForEach(commandString =>
			{
				plugin.AddCommand($"css_{commandString}", "Check your playtime", plugin.CallbackAnonymizer(OnCommandTime));
			});
		}

		public void OnCommandTime(CCSPlayerController? player, CommandInfo info)
		{
			Plugin plugin = (this.PluginContext.Plugin as Plugin)!;

			if (!plugin.CommandHelper(player, info, CommandUsage.CLIENT_ONLY))
				return;

			if (!PlayerCache.Instance.ContainsPlayer(player!))
			{
				info.ReplyToCommand($" {plugin.Localizer["k4.general.prefix"]} {plugin.Localizer["k4.general.loading"]}");
				return;
			}

			TimeData? playerData = PlayerCache.Instance.GetPlayerData(player!).timeData;

			if (playerData is null)
				return;

			DateTime now = DateTime.UtcNow;

			playerData.TimeFields["all"] += (int)Math.Round((now - playerData.Times["Connect"]).TotalSeconds);
			
			info.ReplyToCommand($" {plugin.Localizer["k4.general.prefix"]} {plugin.Localizer["k4.times.title", player.PlayerName]}");
			info.ReplyToCommand($" {plugin.Localizer["k4.times.line1", FormatPlaytime(playerData.TimeFields["all"])]}");
			playerData.Times = new Dictionary<string, DateTime>
			{
				{ "Connect", now },
				{ "Death", now }
			};
		}
	}
}