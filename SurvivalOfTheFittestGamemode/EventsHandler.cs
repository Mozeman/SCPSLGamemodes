using Smod2;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.EventSystem.Events;
using System.Collections.Generic;
using Smod2.Events;
using System;
using System.Timers;
using System.Linq;
using System.Text;
using Blackout;
using scp4aiur;

namespace SurvivalGamemode
{
    internal class EventsHandler : IEventHandlerTeamRespawn, IEventHandlerSetRole, IEventHandlerCheckRoundEnd, IEventHandlerRoundStart, IEventHandlerPlayerDie, IEventHandlerPlayerJoin, IEventHandlerRoundEnd
    {
        private readonly Survival plugin;
        public static Timer timer;
        public static bool blackouts;

        public EventsHandler(Survival plugin) => this.plugin = plugin;
        public void OnPlayerJoin(PlayerJoinEvent ev)
        {
            if (Survival.enabled)
            {
                if (!Survival.roundstarted)
                {
                    Server server = plugin.pluginManager.Server;
                    server.Map.ClearBroadcasts();
                    server.Map.Broadcast(25, "<color=#50c878>Survival Gamemode</color> is starting...", false);
                }
            }
        }
        public void OnSetRole(PlayerSetRoleEvent ev)
        {
            if (Survival.enabled)
            {
               if (ev.TeamRole.Team == Team.SCP && ev.TeamRole.Role != Role.SCP_173)
               {
                   SpawnNut(ev.Player);
               }
               else if (ev.TeamRole.Team != Team.SPECTATOR && ev.TeamRole.Team != Team.SCP)
               {
                    SpawnDboi(ev.Player);
               }
               else if (ev.TeamRole.Team == Team.SPECTATOR)
               {
                    ev.Player.PersonalBroadcast(25, "You are dead! But don't worry, now you get to relax and watch your friends die!", false);
               }
           }
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            if (Survival.enabled)
            {
                Survival.nut_delay = this.plugin.GetConfigInt("Surival_peanut_delay");

                foreach (Smod2.Plugin p in PluginManager.Manager.EnabledPlugins)
                {
                    if (p.Details.id == "Blackout" && p is Blackout.Plugin)
                    {
                        if (Blackout.Plugin.enabled)
                        {
                            Blackout.Plugin.DisableBlackouts();
                            blackouts = true;
                        }
                    }
                }
                int nut_delay = Survival.nut_delay;
                timer = new System.Timers.Timer();
                timer.Interval = 120000;
                timer.Elapsed += OnTimedEvent;
                timer.AutoReset = false;
                timer.Enabled = true;
                plugin.Info("Timer Initialized..");
                plugin.Info("Timer set to " + nut_delay + " ms.");

                Survival.roundstarted = true;
                plugin.pluginManager.Server.Map.ClearBroadcasts();
                plugin.Info("Survival of the Fittest Gamemode Started!");

                string[] dlist = new string[] { "CHECKPOINT_LCZ_A", "CHECKPOINT_LCZ_B", "CHECKPOINT_ENT" };
                
                foreach (string d in dlist)
                {
                    foreach (Door door in ev.Server.Map.GetDoors())
                    {
                        if ( d == door.Name)
                        {
                            door.Open = false;
                            door.Locked = true;
                        }
                    }
                }


                foreach (Player player in ev.Server.GetPlayers())
                {
                    if (player.TeamRole.Team != Team.SCP && player.TeamRole.Team != Team.SPECTATOR)
                    {
                        SpawnDboi(player);
                    }
                    else if (player.TeamRole.Team == Team.SCP)
                    {
                        SpawnNut(player);
                    }
                }
            }
        }

        public void OnRoundEnd(RoundEndEvent ev)
        {
            if (Survival.enabled)
                plugin.Info("Round Ended!");
                EndGamemodeRound();
        }

        public void OnCheckRoundEnd(CheckRoundEndEvent ev)
        {
            if (Survival.enabled)
            {
                bool peanutAlive = false;
                bool humanAlive = false;

                foreach (Player player in ev.Server.GetPlayers())
                {
                    if (player.TeamRole.Team == Team.SCP)
                    {
                        peanutAlive = true; continue;
                    }

                    else if (player.TeamRole.Team != Team.SCP && player.TeamRole.Team != Team.SPECTATOR)
                        humanAlive = true;
                }
                if (ev.Server.GetPlayers().Count > 1)
                {
                    if (peanutAlive && humanAlive)
                    {
                        ev.Status = ROUND_END_STATUS.ON_GOING;
                    }
                    else if (peanutAlive && humanAlive == false)
                    {
                        ev.Status = ROUND_END_STATUS.SCP_VICTORY; EndGamemodeRound();
                    }
                    else if (peanutAlive == false && humanAlive)
                    {
                        ev.Status = ROUND_END_STATUS.CI_VICTORY; EndGamemodeRound();
                    }
                }
            }
        }

        public void OnPlayerDie(PlayerDeathEvent ev)
        {
            if (Survival.enabled)
            {
                if (ev.Player.TeamRole.Team != Team.SCP)
                {
                    SpawnDboi(ev.Player);
                }
            }
        }

        public void OnTeamRespawn(TeamRespawnEvent ev)
        {
            if (Survival.enabled)
            {
                ev.SpawnChaos = true;
                ev.PlayerList = new List<Player>();
            }
        }

        public void EndGamemodeRound()
        {
            plugin.Info("EndgameRound Function");
            Survival.roundstarted = false;
            plugin.Server.Round.EndRound();
            
            foreach (Smod2.Plugin p in PluginManager.Manager.EnabledPlugins)
            {
                if (p.Details.id == "Blackout" && p is Blackout.Plugin)
                {
                    Blackout.Plugin.ToggleBlackout();
                    if (blackouts)
                    {
                        Blackout.Plugin.EnableBlackouts();
                    }
                }
            }
            if (Blackout.Plugin.enabled)
            {
                Blackout.Plugin.ToggleBlackout();
                if (blackouts)
                {
                    Blackout.Plugin.EnableBlackouts();
                }
            }

        }

        public void SpawnDboi(Player player)
        {
            Vector spawn = plugin.Server.Map.GetRandomSpawnPoint(Role.SCP_096);
            player.ChangeRole(Role.CLASSD, false, false, false, true);
            player.Teleport(spawn);

           player.PersonalClearBroadcasts();
           player.PersonalBroadcast(15, "You are a <color=#ffa41a>D-Boi</color>! Find a hiding place and survive from the peanuts!", false);
        }

        public void SpawnNut(Player player)
        {
            player.ChangeRole(Role.SCP_173, true, true, true, true);
            plugin.Info("Spawned " + player.Name + " as SCP-173");
            player.PersonalClearBroadcasts();
            player.PersonalBroadcast(35, "You will be teleported into the game arena when adequate time has passed for other players to hide...", false);
        }
        public void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            plugin.Info("Timer completed!");
            foreach (Smod2.Plugin p in PluginManager.Manager.EnabledPlugins)
            {
                if (p.Details.id == "Blackout" && p is Blackout.Plugin)
                {
                    Blackout.Plugin.ToggleBlackout();
                }
            }
            Vector spawn = plugin.Server.Map.GetRandomSpawnPoint(Role.SCP_939_53);
            foreach (Player player in plugin.Server.GetPlayers())
            {
                if (player.TeamRole.Role == Role.SCP_173)
                    player.Teleport(spawn);
                    player.PersonalBroadcast(15, "You are a <color=#c50000>Neck-Snappy Boi</color>! Kill all of the Class-D before the auto-nuke goes off!", false);
            }
        }
    }
}