using System.Collections.Generic;
using MEC;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.EventSystem.Events;

namespace Gangwar
{
	internal class EventsHandler : IEventHandlerTeamRespawn, IEventHandlerCheckRoundEnd, IEventHandlerRoundStart, IEventHandlerRoundEnd, IEventHandlerPlayerJoin, IEventHandlerWaitingForPlayers,
		IEventHandlerRoundRestart
	{
		private readonly Gangwar plugin;

		public EventsHandler(Gangwar plugin) => this.plugin = plugin;

		public void OnPlayerJoin(PlayerJoinEvent ev)
		{
			if (!plugin.Enabled) return;
			if (plugin.RoundStarted) return;
			
			Server server = plugin.Server;
			server.Map.ClearBroadcasts();
			server.Map.Broadcast(25, "<color=#00FFFF> Gangwar Gamemode</color> is starting..", false);
		}

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			plugin.ReloadConfig();
		}

		public void OnRoundStart(RoundStartEvent ev)
		{
			if (!plugin.Enabled) return;
			
			plugin.RoundStarted = true;

			PlayerManager.localPlayer.GetComponent<AlphaWarheadController>().StartDetonation();
			plugin.Server.Map.ClearBroadcasts();
			plugin.Info("Gangwar Gamemode started!");
			List<Player> players = ev.Server.GetPlayers();
			int num = players.Count / 2;

			for (int i = 0; i < num; i++)
			{
				int random = plugin.Gen.Next(players.Count);
				Player player = players[random];
				players.Remove(player);
				Timing.RunCoroutine(plugin.Functions.SpawnNtf(player, 0));
			}

			foreach (Player player in players)
				if (player.TeamRole.Role != Role.NTF_COMMANDER && !plugin.Spawning.ContainsKey(player.SteamId))
					Timing.RunCoroutine(plugin.Functions.SpawnChaos(player, 0));

			string[] dList = new string[] { "GATE_A", "GATE_B" };

			foreach (string d in dList)
			foreach (Smod2.API.Door door in ev.Server.Map.GetDoors())
				if (d == door.Name)
				{
					plugin.Debug("Locking " + door.Name + ".");
					door.Open = false;
					door.Locked = true;
				}
		}

		public void OnRoundEnd(RoundEndEvent ev)
		{
			if (!plugin.RoundStarted) return;

			plugin.Info("Round Ended!");
			plugin.Functions.EndGamemodeRound();
		}

		public void OnRoundRestart(RoundRestartEvent ev)
		{
			if (!plugin.RoundStarted) return;

			plugin.Info("Round Restarted.");
			plugin.Functions.EndGamemodeRound();
		}

		public void OnCheckRoundEnd(CheckRoundEndEvent ev)
		{
			if (!plugin.RoundStarted) return;


			bool ciAlive = false;
			bool ntfAlive = false;

			foreach (Player player in ev.Server.GetPlayers())
				switch (player.TeamRole.Team)
				{
					case Smod2.API.Team.CHAOS_INSURGENCY:
						ciAlive = true;
						break;
					case Smod2.API.Team.NINETAILFOX:
						ntfAlive = true;
						break;
				}

			if (ev.Server.GetPlayers().Count <= 1) return;
			
			if (ciAlive && ntfAlive)
			{
				ev.Status = ROUND_END_STATUS.ON_GOING;
				ev.Server.Map.ClearBroadcasts();
				ev.Server.Map.Broadcast(10, "There are " + plugin.Round.Stats.CiAlive + " Chaos alive, and " + plugin.Round.Stats.NTFAlive + " NTF alive.", false);
			}
			else if (ciAlive && !ntfAlive)
			{
				ev.Status = ROUND_END_STATUS.OTHER_VICTORY; plugin.Functions.EndGamemodeRound();
			}
			else if (!ciAlive && ntfAlive)
			{
				ev.Status = ROUND_END_STATUS.MTF_VICTORY; plugin.Functions.EndGamemodeRound();
			}
		}

		public void OnTeamRespawn(TeamRespawnEvent ev)
		{
			if (!plugin.RoundStarted) return;

			plugin.Info("Gang Respawn.");

			if (plugin.Round.Stats.CiAlive >= plugin.Round.Stats.NTFAlive)
				ev.SpawnChaos = false;
			else if (plugin.Round.Stats.CiAlive < plugin.Round.Stats.NTFAlive)
				ev.SpawnChaos = true;
		}
	}
}