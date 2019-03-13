using System;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.EventSystem.Events;
using System.Collections.Generic;
using Smod2.Events;
using scp4aiur;

namespace Mystery
{
	public class EventsHandler : IEventHandlerTeamRespawn, IEventHandlerCheckRoundEnd, IEventHandlerRoundStart, IEventHandlerPlayerJoin, IEventHandlerRoundEnd, IEventHandlerWaitingForPlayers, IEventHandlerPlayerDie
	{
		private readonly Mystery plugin;
		public EventsHandler(Mystery plugin) => this.plugin = plugin;

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			Mystery.civ_health = this.plugin.GetConfigInt("myst_civ_health");
			Mystery.det_health = this.plugin.GetConfigInt("myst_det_health");
			Mystery.murder_health = this.plugin.GetConfigInt("myst_murd_health");
			Mystery.detective_num = this.plugin.GetConfigInt("myst_det_num");
			Mystery.monster_num = this.plugin.GetConfigInt("myst_monster_num");
			Mystery.murderer_num = this.plugin.GetConfigInt("myst_murd_num");
			Mystery.det_respawn = this.plugin.GetConfigBool("myst_det_respawn");
			Mystery.murd_respawn = this.plugin.GetConfigBool("myst_murd_respawn");
		}
		public void OnPlayerJoin(PlayerJoinEvent ev)
		{
			if (Mystery.enabled)
			{
				if (!Mystery.roundstarted)
				{
					Server server = plugin.pluginManager.Server;
					server.Map.ClearBroadcasts();
					server.Map.Broadcast(25, "<color=#c50000>Murder Mystery</color> gamemode is starting...", false);
				}
			}
		}
		public void OnRoundStart(RoundStartEvent ev)
		{
			if (Mystery.enabled)
			{
				Mystery.roundstarted = true;
				plugin.pluginManager.Server.Map.ClearBroadcasts();
				plugin.Info("Mystery Gamemode started!");

				List<Player> players = ev.Server.GetPlayers();
				List<string> murds = new List<string>();
				List<string> dets = new List<string>();

				for (int i = 0; i < Mystery.murderer_num; i++)
				{
					if (players.Count == 0) break;
					int random = Mystery.gen.Next(players.Count);
					Player ranplayer = players[random];
					players.Remove(ranplayer);
					Timing.Run(Functions.singleton.SpawnMurd(ranplayer));
					Mystery.murds.Add(ranplayer.Name);
				}
				for (int i = 0; i < Mystery.detective_num; i++)
				{
					if (players.Count == 0) break;
					int random = Mystery.gen.Next(players.Count);
					Player ranplayer = players[random];
					players.Remove(ranplayer);
					Timing.Run(Functions.singleton.SpawnDet(ranplayer));
				}
				foreach (Player player in players)
				{
					Timing.Run(Functions.singleton.SpawnCiv(player));
				}
			}
		}
		public void OnRoundEnd(RoundEndEvent ev)
		{
			if (Mystery.enabled || Mystery.roundstarted)
			{
				plugin.Info("Round Ended!");
				Functions.singleton.EnableGamemode();
			}
		}
		public void OnPlayerDie(PlayerDeathEvent ev)
		{
			if (Mystery.enabled || Mystery.roundstarted)
			{
				if (ev.Player.TeamRole.Role == Role.CLASSD)
				{
					if (Mystery.murds.Contains(ev.Player.Name))
					{
						plugin.Server.Map.ClearBroadcasts();
						plugin.Server.Map.Broadcast(15, "A murderer, " + ev.Player.Name + " has been eliminated by " + ev.Killer.Name + "!", false);
					}
					else
					{
						plugin.Server.Map.ClearBroadcasts();
						plugin.Server.Map.Broadcast(25, "There are now " + (plugin.Server.Round.Stats.ClassDAlive - 1) + " Civilians alive.", false);
						if (!Mystery.murds.Contains(ev.Player.Name))
						{
							ev.Killer.Kill();
							ev.Killer.PersonalClearBroadcasts();
							ev.Killer.PersonalBroadcast(15, "<color=#c50000>You killed an innocent person! You monster!", false);
						}
					}
				}
				else if (ev.Player.TeamRole.Role == Role.SCIENTIST)
				{
					plugin.Server.Map.ClearBroadcasts();
					plugin.Server.Map.Broadcast(15, "A detective, " + ev.Player.Name + " has been killed!", false);
					if (!Mystery.murds.Contains(ev.Killer.Name))
						{
							ev.Killer.Kill();
							ev.Killer.PersonalClearBroadcasts();
							ev.Killer.PersonalBroadcast(15, "<color=#c50000>You were innocent and killed a Detective! How rude!", false);
						}
				}
			}
		}
		public void OnCheckRoundEnd(CheckRoundEndEvent ev)
		{
			if (Mystery.enabled || Mystery.roundstarted)
			{
				bool murd_alive = false;
				bool civ_alive = false;

				foreach (Player player in ev.Server.GetPlayers())
				{
					if (player.TeamRole.Role == Role.CLASSD && Mystery.murds.Contains(player.Name))
					{
						murd_alive = true; continue;
					}
					else if (player.TeamRole.Role == Role.CLASSD && !Mystery.murds.Contains(player.Name))
					{
						civ_alive = true; continue;
					}
				}

				if (ev.Server.GetPlayers().Count > 1)
				{
					if (murd_alive && civ_alive)
					{
						ev.Status = ROUND_END_STATUS.ON_GOING;
					}
					else if (!murd_alive && civ_alive)
					{
						ev.Status = ROUND_END_STATUS.MTF_VICTORY; Functions.singleton.EndGamemoderound();
						plugin.Server.Map.ClearBroadcasts();
						plugin.Server.Map.Broadcast(25, "The Civilains and Detectives have eliminated all the murderers!", false);
					}
					else if (murd_alive && !civ_alive)
					{
						ev.Status = ROUND_END_STATUS.SCP_VICTORY; Functions.singleton.EndGamemoderound();
						plugin.Server.Map.ClearBroadcasts();
						plugin.Server.Map.Broadcast(25, "The murderers have killed all of the civilians!", false);
					}
				}
			}
		}
		public void OnTeamRespawn(TeamRespawnEvent ev)
        {
            if (Mystery.enabled || Mystery.roundstarted)
            {
                ev.SpawnChaos = true;
				foreach (Player player in ev.PlayerList)
				{
					int random = Mystery.gen.Next(ev.PlayerList.Count);
					Player ranmurd = ev.PlayerList[random];
					ev.PlayerList.Remove(ranmurd);
					int Random = Mystery.gen.Next(ev.PlayerList.Count);
					Player randet = ev.PlayerList[random];
					ev.PlayerList.Remove(randet);

					Functions.singleton.SpawnMurd(ranmurd);
					Functions.singleton.SpawnDet(randet);
				}
				foreach (Player player in ev.PlayerList)
				{
					Functions.singleton.SpawnCiv(player);
				}
				
            }
        }
	}
}