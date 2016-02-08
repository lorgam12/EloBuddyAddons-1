﻿using System;
using System.Drawing;
using System.Runtime.Remoting.Channels;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Rendering;
using JokerFioraBuddy.Evade;
using SharpDX;
using Color = System.Drawing.Color;
using PermaSettings = JokerFioraBuddy.Config.Modes.Perma;
using ComboSettings = JokerFioraBuddy.Config.Modes.Combo;
using ShieldSettings = JokerFioraBuddy.Config.ShieldBlock;

namespace JokerFioraBuddy
{
    public static class Program
    {
        public const string ChampName = "Fiora";
        private static Text Text { get; set; }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        private static void OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampName)
                return;

            Config.Initialize();
            TargetSelector2.Initialize();
            ModeManager.Initialize();
            ItemManager.Initialize();
            SpellManager.Initialize();
            PassiveManager.Initialize();
            SpellBlock.Initialize();
            Dispeller.Initialize();

            Text = new Text("", new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold)) {Color = Color.Red};

            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnLevelUp += Obj_AI_Base_OnLevelUp;
            Drawing.OnDraw += Drawing_OnDraw;

            Chat.Print(
                "<font color = \"#6B9FE3\">Joker Fiora 2.00</font><font color = \"#E3AF6B\"> by JokerArt</font>. Report any bugs please! Thanks and enjoy.");
            Player.LevelSpell(SpellSlot.Q);
            Player.SetSkinId(Config.Misc.SkinID);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            
            if (Config.Misc.drawQ)
                Circle.Draw(SpellManager.Q.IsReady() ? Config.Misc.currentColor : SharpDX.Color.Red,
                    SpellManager.Q.Range, 3F, Player.Instance.Position);
            if (Config.Misc.drawW)
                Circle.Draw(SpellManager.W.IsReady() ? Config.Misc.currentColor : SharpDX.Color.Red,
                    SpellManager.W.Range, 3F, Player.Instance.Position);
            if (Config.Misc.drawE)
                Circle.Draw(SpellManager.E.IsReady() ? Config.Misc.currentColor : SharpDX.Color.Red,
                    SpellManager.E.Range, 3F, Player.Instance.Position);
            if (Config.Misc.drawR)
                Circle.Draw(SpellManager.R.IsReady() ? Config.Misc.currentColor : SharpDX.Color.Red,
                    SpellManager.R.Range, 3F, Player.Instance.Position);
        }

        private static void Obj_AI_Base_OnLevelUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe)
                return;
            SpellSlot[] levels =
            {
                SpellSlot.Unknown, SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.Q, SpellSlot.Q,
                SpellSlot.R, SpellSlot.Q, SpellSlot.E, SpellSlot.Q, SpellSlot.E, SpellSlot.R, SpellSlot.E, SpellSlot.E,
                SpellSlot.W, SpellSlot.W, SpellSlot.R, SpellSlot.W, SpellSlot.W
            };
            if (Config.Misc.enableLevelUP)
                Core.DelayAction(() => Player.LevelSpell(levels[Player.Instance.Level]), 500);
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var unit = sender as AIHeroClient;
            

            /*
            Chat.Print("=========================================");
            Chat.Print("End Distance: " + args.End.Distance(Player.Instance.Position).ToString());
            Chat.Print("Mov speed: " + unit.MoveSpeed.ToString());
            Chat.Print("Start Distance: " + args.Start.Distance(Player.Instance));
            Chat.Print("unit distance: " + unit.Distance(Player.Instance));
            Chat.Print(args.Time.ToString());
            Chat.Print(sender.Distance(Player.Instance));
            Chat.Print(sender.Distance(Player.Instance.Position));
            */


            if (unit == null || !unit.IsValid)
            {
                return;
            }


            if (unit.IsMe && args.Slot.Equals(SpellSlot.E))
            {
                Orbwalker.ResetAutoAttack();
                return;
            }

            if (!unit.IsEnemy || !ShieldSettings.BlockSpells || !SpellManager.W.IsReady())
            {
                return;
            }

            // spell handled by evade
            if (SpellDatabase.GetByName(args.SData.Name) != null && !ShieldSettings.EvadeIntegration)
                return;

            if (!SpellBlock.Contains(unit, args))
                return;

            if (args.End.Distance(Player.Instance) == 0)
                return;

  

            var castUnit = unit;
            var type = args.SData.TargettingType;

            if (!unit.IsValidTarget())
            {
                var target = TargetSelector2.GetTarget(SpellManager.W.Range, DamageType.Mixed);
                if (target == null || !target.IsValidTarget(SpellManager.W.Range))
                {
                    target = TargetSelector.SelectedTarget;
                }

                if (target != null && target.IsValidTarget(SpellManager.W.Range))
                {
                    castUnit = target;
                }
            }

            if (unit.ChampionName.Equals("Caitlyn") && args.Slot == SpellSlot.Q)
            {
                Core.DelayAction(() => CastW(castUnit),
                    (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                    (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 500);
            }
            if (unit.ChampionName.Equals("Zyra"))
            {
                if (args.SData.Name == "ZyraGraspingRoots")
                    Core.DelayAction(() => SpellManager.W.Cast(Game.CursorPos), 15000);
                else
                    Core.DelayAction(() => CastW(castUnit),
                        (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                        (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 500);
            }
            if (args.End.Distance(Player.Instance) < 250)
            {
                if (unit.ChampionName.Equals("Bard") && args.End.Distance(Player.Instance) < 300)
                {
                    Core.DelayAction(() => CastW(castUnit), (int)(unit.Distance(Player.Instance) / 7f) + 400);
                }
                else if (unit.ChampionName.Equals("Ashe"))
                {
                    Core.DelayAction(() => CastW(castUnit),
                        (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                        (int)args.End.Distance(Player.Instance));
                    return;
                }
                else if (unit.ChampionName.Equals("Varus") || unit.ChampionName.Equals("TahmKench") ||
                         unit.ChampionName.Equals("Lux"))
                {
                    Core.DelayAction(() => CastW(castUnit),
                        (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                        (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 500);
                }
                else if (unit.ChampionName.Equals("Amumu"))
                {
                    if (sender.Distance(Player.Instance) < 1100)
                        Core.DelayAction(() => CastW(castUnit),
                            (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                            (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 500);
                }
            }
            //    else if (args.End.Distance(Player.Instance) < 60)
            //    {
            //        Core.DelayAction(() => CastW(castUnit),
            //            (int) (args.Start.Distance(Player.Instance)/args.SData.MissileSpeed*1000) -
            //            (int) (args.End.Distance(Player.Instance)/args.SData.MissileSpeed) - 500);
            //    }
            //}


            if (args.Target != null)
            {
            //    if (!args.Target.IsMe ||
            //        (args.Target.Name.Equals("Barrel") && args.Target.Distance(Player.Instance) > 200 &&
            //         args.Target.Distance(Player.Instance) < 400))
            //    {
            //        Console.WriteLine("barrel");
            //        return;
            //    }

                if (unit.ChampionName.Equals("Nautilus") ||
                    (unit.ChampionName.Equals("Caitlyn") && args.Slot.Equals(SpellSlot.R)))
                {
                    var d = unit.Distance(Player.Instance);
                    var travelTime = d / args.SData.MissileSpeed;
                    var delay = travelTime * 1000 - SpellManager.W.CastDelay + 150;
                    Console.WriteLine("TT: " + travelTime + " " + delay);
                    Core.DelayAction(() => CastW(castUnit), (int)delay);
                    return;
                }
                CastW(castUnit);
            }

            if (type.Equals(SpellDataTargetType.Unit))
            {
                if (unit.ChampionName.Equals("Bard") && args.End.Distance(Player.Instance) < 300)
                {
                    Core.DelayAction(() => CastW(castUnit), 400 + (int)(unit.Distance(Player.Instance) / 7f));
                }
                else if (unit.ChampionName.Equals("Riven") && args.End.Distance(Player.Instance) < 260)
                {
                    Console.WriteLine("RIVEN");
                    CastW(castUnit);
                }
                else
                {
                    CastW(castUnit);
                }
            }
            else if (type.Equals(SpellDataTargetType.LocationAoe) &&
                     args.End.Distance(Player.Instance) < args.SData.CastRadius)
            {
                // annie moving tibbers
                if (unit.ChampionName.Equals("Annie") && args.Slot.Equals(SpellSlot.R))
                {
                    return;
                }
                CastW(castUnit);
            }
            else if (type.Equals(SpellDataTargetType.Cone) &&
                     args.End.Distance(Player.Instance) < args.SData.CastRadius)
            {
                CastW(castUnit);
            }
            else if (type.Equals(SpellDataTargetType.SelfAoe) || type.Equals(SpellDataTargetType.Self))
            {
                var d = args.End.Distance(Player.Instance.ServerPosition);
                var p = args.SData.CastRadius > 5000 ? args.SData.CastRange : args.SData.CastRadius;
                if (d < p)
                    CastW(castUnit);
            }
        }

        public static bool CastW(Obj_AI_Base target)
        {
            if (target == null || !target.IsValidTarget(SpellManager.W.Range))
                 //return SpellManager.W.Cast(Game.CursorPos);
                return SpellManager.W.Cast(Player.Instance.Position);

            var cast = SpellManager.W.GetPrediction(target);
            var castPos = SpellManager.W.IsInRange(cast.CastPosition) ? cast.CastPosition : target.ServerPosition;

            return SpellManager.W.Cast(castPos);
        }
    }
}