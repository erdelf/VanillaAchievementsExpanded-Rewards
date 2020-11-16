namespace RewardsAddon
{
    using System.Collections.Generic;
    using System.Linq;
    using AchievementsExpanded;
    using RimWorld;
    using UnityEngine;
    using Verse;
    using Verse.AI;

    public abstract class Reward_MapBased : AchievementsExpanded.AchievementReward
    {
        public override string Disabled
        {
            get
            {
                string disabled = base.Disabled;
                
                if (!Find.CurrentMap?.IsPlayerHome ?? false)
                {
                    disabled += "\n" + "NoValidMap".Translate();
                }

                return disabled;
            }
        }
    }

    public class Reward_GiveWeapon : Reward_MapBased
    {
        public override bool TryExecuteEvent()
        {
            Map map = Find.CurrentMap;

            if (!map.areaManager.Home.ActiveCells.Where(predicate: i => i.Standable(map)).TryRandomElement(out IntVec3 position))
                return false;

            if (!DefDatabase<ThingDef>.AllDefsListForReading
                                   .Where(predicate: def => def.equipmentType == EquipmentType.Primary && !(def.weaponTags?.TrueForAll(match: s => s.Contains(value: "Mechanoid") || s.Contains(value: "Turret") || s.Contains(value: "Artillery")) ??
                                                                                                            false)).ToList().TryRandomElement(out ThingDef tDef))
                return false;

            Thing thing = ThingMaker.MakeThing(tDef, tDef.MadeFromStuff ? GenStuff.RandomStuffFor(tDef) : null);
            CompQuality qual = thing.TryGetComp<CompQuality>();
            qual?.SetQuality(Rand.Bool ?
                                 QualityCategory.Normal : Rand.Bool ?
                                     QualityCategory.Good : Rand.Bool ?
                                         QualityCategory.Excellent : Rand.Bool ?
                                             QualityCategory.Masterwork :
                                             QualityCategory.Legendary, ArtGenerationContext.Colony);

            GenSpawn.Spawn(thing, position, map);
            Vector3 vec = position.ToVector3();
            MoteMaker.ThrowSmoke(vec, map, size: 5);
            MoteMaker.ThrowMetaPuff(vec, map);

            Find.CameraDriver.JumpToCurrentMapLoc(position);

            return true;
        }
    }

    public class Reward_GiveApparel : Reward_MapBased
    {
        public override bool TryExecuteEvent()
        {
            Map map = Find.CurrentMap;

            IEnumerable<ThingDef> apparelList = DefDatabase<ThingDef>.AllDefsListForReading.Where(predicate: td => td.IsApparel).ToList();

            IntVec3 intVec = map.areaManager.Home.ActiveCells.Where(predicate: iv => iv.Standable(map)).RandomElement();

            for (int i = 0; i < 5; i++)
            {
                if (!apparelList.TryRandomElement(out ThingDef apparelDef)) continue;

                intVec = intVec.RandomAdjacentCell8Way();
                Thing       apparel = ThingMaker.MakeThing(apparelDef, apparelDef.MadeFromStuff ? GenStuff.RandomStuffFor(apparelDef) : null);
                CompQuality qual    = apparel.TryGetComp<CompQuality>();
                qual?.SetQuality(Rand.Bool ?
                                        QualityCategory.Normal : Rand.Bool ?
                                            QualityCategory.Good : Rand.Bool ?
                                                QualityCategory.Excellent : Rand.Bool ?
                                                    QualityCategory.Masterwork :
                                                    QualityCategory.Legendary, ArtGenerationContext.Colony);

                GenSpawn.Spawn(apparel, intVec, map);
                Vector3 vec = intVec.ToVector3();
                MoteMaker.ThrowSmoke(vec, map, 5);
                MoteMaker.ThrowMetaPuff(vec, map);
            }

            Find.CameraDriver.JumpToCurrentMapLoc(intVec);
            return true;
        }
    }

    public class Reward_GiveArt : Reward_MapBased
    {
        public override bool TryExecuteEvent()
        {
            Map map = Find.CurrentMap;

            if (!map.areaManager.Home.ActiveCells.Where(predicate: i => i.Standable(map)).TryRandomElement(out IntVec3 position))
                return false;

            if (!DefDatabase<ThingDef>.AllDefsListForReading.Where(predicate: td => td.thingClass == typeof(Building_Art)).TryRandomElement(out ThingDef tDef))
                return false;

            Thing       thing = ThingMaker.MakeThing(tDef, tDef.MadeFromStuff ? GenStuff.RandomStuffFor(tDef) : null);
            CompQuality qual  = thing.TryGetComp<CompQuality>();
            qual?.SetQuality(Rand.Bool ?
                                    QualityCategory.Normal : Rand.Bool ?
                                        QualityCategory.Good : Rand.Bool ?
                                            QualityCategory.Excellent : Rand.Bool ?
                                                QualityCategory.Masterwork :
                                                QualityCategory.Legendary, ArtGenerationContext.Colony);

            thing.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(thing, position, map);
            Vector3 vec = position.ToVector3();
            MoteMaker.ThrowSmoke(vec, map, size: 5);
            MoteMaker.ThrowMetaPuff(vec, map);

            Find.CameraDriver.JumpToCurrentMapLoc(position);
            return true;
        }
    }

    public class Reward_GiveAnimal : Reward_MapBased
    {
        public override bool TryExecuteEvent()
        {
            Map map = Find.CurrentMap;

            if (!DefDatabase<PawnKindDef>.AllDefsListForReading.Where(predicate: p => p.RaceProps.Animal).TryRandomElement(out PawnKindDef pawnKindDef)) 
                return false;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 root, map, roadChance: 50f)) 
                return false;

            Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDef);

            IntVec3 loc = CellFinder.RandomClosewalkCellNear(root, map, radius: 10);
            GenSpawn.Spawn(pawn, loc, map);


            pawn.SetFaction(Faction.OfPlayer);

            foreach (TrainableDef td in TrainableUtility.TrainableDefsInListOrder)
            {
                if (!pawn.training.CanAssignToTrain(td, out bool _).Accepted) 
                    continue;

                while (!pawn.training.HasLearned(td))
                    pawn.training.Train(td, map.mapPawns.FreeColonists.RandomElement());
            }

            pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.GotoWander, map.areaManager.Home.ActiveCells.Where(predicate: iv => iv.Standable(map)).RandomElement()));

            Find.CameraDriver.JumpToCurrentMapLoc(loc);
            return true;
        }
    }

    public class Reward_OrbitalTrader : Reward_MapBased
    {
        public override bool TryExecuteEvent() => 
            IncidentDefOf.OrbitalTraderArrival.Worker.TryExecute(new IncidentParms() { target = Find.CurrentMap });
    }

    public class Reward_SendHappyThoughts : Reward_MapBased
    {
        public override bool TryExecuteEvent()
        {
            foreach (Pawn p in Find.ColonistBar.GetColonistsInOrder().Where(predicate: x => !x.Dead))
            {
                p.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.NewColonyOptimism);
            }
            return true;
        }
    }

    public class Reward_DebugGivePoints : AchievementReward
    {
        public override bool TryExecuteEvent() => true;
    }
}