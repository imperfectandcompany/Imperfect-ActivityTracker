namespace K4System
{
	using System.Text;

	using CounterStrikeSharp.API.Core;
	using CounterStrikeSharp.API.Modules.Utils;

	public partial class ModuleTime : IModuleTime
	{
		public void BeforeDisconnect(CCSPlayerController player)
		{
			DateTime now = DateTime.UtcNow;

			TimeData? playerData = PlayerCache.Instance.GetPlayerData(player).timeData;

			if (playerData is null)
				return;

			playerData.TimeFields["all"] += (int)Math.Round((now - playerData.Times["Connect"]).TotalSeconds);
		}
		public string FormatPlaytime(int totalSeconds)
		{
			string[] units = { "k4.phrases.shortyear", "k4.phrases.shortmonth", "k4.phrases.shortday", "k4.phrases.shorthour", "k4.phrases.shortminute", "k4.phrases.shortsecond" };
			int[] values = { totalSeconds / 31536000, totalSeconds % 31536000 / 2592000, totalSeconds % 2592000 / 86400, totalSeconds % 86400 / 3600, totalSeconds % 3600 / 60, totalSeconds % 60 };

			StringBuilder formattedTime = new StringBuilder();

			bool addedValue = false;

			Plugin plugin = (this.PluginContext.Plugin as Plugin)!;

			for (int i = 0; i < units.Length; i++)
			{
				if (values[i] > 0)
				{
					formattedTime.Append($"{values[i]}{plugin.Localizer[units[i]]}, ");
					addedValue = true;
				}
			}

			if (!addedValue)
			{
				formattedTime.Append("0s");
			}

			return formattedTime.ToString().TrimEnd(' ', ',');
		}
	}
}
