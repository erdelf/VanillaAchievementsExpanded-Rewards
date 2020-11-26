namespace RewardsAddon
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AchievementsExpanded;
    using Rewards_Addon;
    using RimWorld;
    using RimWorld.Planet;
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
                    disabled += "\n" + "NoValidMap".Translate();

                return disabled;
            }
        }
    }

    public abstract class Reward_GiveThings : Reward_MapBased
    {
        public IntRange range = IntRange.one;

        public override bool TryExecuteEvent()
        {
            Map map = Find.CurrentMap;

            if (!TargetCell(map, out IntVec3 pos))
                return false;

            IEnumerable<Thing> things = this.GetThings(map, this.range.RandomInRange);

            if (!things.Any())
                return false;

            things = things.Select(selector: t => t.TryMakeMinified());

            DropPodUtility.DropThingsNear(pos, map, things);
            Find.CameraDriver.JumpToCurrentMapLoc(pos);

            return true;
        }

        public virtual bool TargetCell(Map map, out IntVec3 position) =>
            map.areaManager.Home.ActiveCells.Where(predicate: i => i.Standable(map)).TryRandomElement(out position);

        public virtual IEnumerable<Thing> GetThings(Map map, int count)
        {
            for (int i = 0; i < count; i++)
                yield return this.GetThing(map);
        }

        public abstract Thing GetThing(Map map);
    }

    public class Reward_GiveWeapon : Reward_GiveThings
    {
        public override Thing GetThing(Map map)
        {
            if (!DefDatabase<ThingDef>.AllDefsListForReading
                                   .Where(predicate: def => def.equipmentType == EquipmentType.Primary &&
                                                            !(def.weaponTags?.TrueForAll(match: s => s.Contains(value: "Mechanoid") || s.Contains(value: "Turret") || s.Contains(value: "Artillery")) ??
                                                              false)).ToList().TryRandomElement(out ThingDef tDef))
                return null;

            Thing       thing = ThingMaker.MakeThing(tDef, tDef.MadeFromStuff ? GenStuff.RandomStuffFor(tDef) : null);
            CompQuality qual  = thing.TryGetComp<CompQuality>();
            qual?.SetQuality(Rand.Bool ? QualityCategory.Normal :
                             Rand.Bool ? QualityCategory.Good :
                             Rand.Bool ? QualityCategory.Excellent :
                             Rand.Bool ? QualityCategory.Masterwork :
                                         QualityCategory.Legendary, ArtGenerationContext.Colony);

            return thing;

        }
    }

    public class Reward_GiveApparel : Reward_GiveThings
    {

        private List<ThingDef> apparelList;

        public List<ThingDef> ApparelList => this.apparelList ?? (this.apparelList = DefDatabase<ThingDef>.AllDefsListForReading.Where(predicate: td => td.IsApparel).ToList());


        public override Thing GetThing(Map map)
        {
            if (!this.ApparelList.TryRandomElement(out ThingDef apparelDef))
                return null;


            Thing       apparel = ThingMaker.MakeThing(apparelDef, apparelDef.MadeFromStuff ? GenStuff.RandomStuffFor(apparelDef) : null);
            CompQuality qual    = apparel.TryGetComp<CompQuality>();
            qual?.SetQuality(Rand.Bool ? QualityCategory.Normal :
                             Rand.Bool ? QualityCategory.Good :
                             Rand.Bool ? QualityCategory.Excellent :
                             Rand.Bool ? QualityCategory.Masterwork :
                                         QualityCategory.Legendary, ArtGenerationContext.Colony);

            return apparel;
        }
    }

    public class Reward_GiveArt : Reward_GiveThings
    {
        public override Thing GetThing(Map map)
        {
            if (!DefDatabase<ThingDef>.AllDefsListForReading.Where(predicate: td => td.thingClass == typeof(Building_Art)).TryRandomElement(out ThingDef tDef))
                return null;

            Thing       thing = ThingMaker.MakeThing(tDef, tDef.MadeFromStuff ? GenStuff.RandomStuffFor(tDef) : null);
            CompQuality qual  = thing.TryGetComp<CompQuality>();
            qual?.SetQuality(Rand.Bool ? QualityCategory.Normal :
                             Rand.Bool ? QualityCategory.Good :
                             Rand.Bool ? QualityCategory.Excellent :
                             Rand.Bool ? QualityCategory.Masterwork :
                                         QualityCategory.Legendary, ArtGenerationContext.Outsider);

            thing.SetFactionDirect(Faction.OfPlayer);

            return thing;
        }
    }

    public class Reward_GiveAnimals : Reward_MapBased
    {
        public IntRange range = IntRange.one;

        public override bool TryExecuteEvent()
        {
            Map map = Find.CurrentMap;

            if (!DefDatabase<PawnKindDef>.AllDefsListForReading.Where(predicate: p => p.RaceProps.Animal).TryRandomElement(out PawnKindDef pawnKindDef))
                return false;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 root, map, roadChance: 50f))
                return false;

            int count = this.range.RandomInRange;

            for (int i = 0; i < count; i++)
            {
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


                List<IntVec3> cells = map.areaManager.Home.ActiveCells.ToList();

                IntVec3 target = cells.NullOrEmpty() ? map.Center : cells.Where(predicate: iv => iv.Standable(map)).RandomElement();
                pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.GotoWander, target));
            }

            Find.CameraDriver.JumpToCurrentMapLoc(root);
            return true;
        }
    }

    public class Reward_OrbitalTrader : Reward_MapBased
    {
        public override bool TryExecuteEvent() =>
            IncidentDefOf.OrbitalTraderArrival.Worker.TryExecute(new IncidentParms() {target = Find.CurrentMap});
    }

    public class Reward_SendThoughts : Reward_MapBased
    {
        public ThoughtDef thoughtDef;

        public override bool TryExecuteEvent()
        {
            foreach (Pawn p in Find.ColonistBar.GetColonistsInOrder().Where(predicate: x => !x.Dead))
            {
                p.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
            }

            return true;
        }
    }

    public class Reward_EvilRaid : Reward_MapBased
    {
        public override bool TryExecuteEvent()
        {
            Map map = Find.AnyPlayerHomeMap;
            List<Pawn> colonists = Find.ColonistBar.GetColonistsInOrder().Where(predicate: x => !x.Dead).ToList();

            IncidentParms parms = new IncidentParms()
            {
                target = map,
                points = colonists.Count * PawnKindDefOf.AncientSoldier.combatPower,
                faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.AncientsHostile),
                raidStrategy = RaidStrategyDefOf.ImmediateAttack,
                raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop,
                podOpenDelay = GenDate.TicksPerHour / 2,
                spawnCenter = map.listerBuildings.ColonistsHaveBuildingWithPowerOn(ThingDefOf.OrbitalTradeBeacon) ? DropCellFinder.TradeDropSpot(map) : RCellFinder.TryFindRandomSpotJustOutsideColony(map.IsPlayerHome ? map.mapPawns.FreeColonists.RandomElement().Position : CellFinder.RandomCell(map), map, out IntVec3 spawnPoint) ? spawnPoint : CellFinder.RandomCell(map),
                generateFightersOnly = true,
                forced = true,
                raidNeverFleeIndividual = true
            };
            List<Pawn> pawns = new PawnGroupMaker()
            {
                kindDef = new PawnGroupKindDef()
                {
                    workerClass = typeof(PawnGroupKindWorker_Wrath)
                },
            }.GeneratePawns(new PawnGroupMakerParms()
            {
                tile = ((Map)parms.target).Tile,
                faction = parms.faction,
                points = parms.points,
                generateFightersOnly = true,
                raidStrategy = parms.raidStrategy
            }).ToList();

            IEnumerable<RecipeDef> recipes = DefDatabase<RecipeDef>.AllDefsListForReading.Where(predicate: rd => (rd.addsHediff?.addedPartProps?.betterThanNatural ?? false) &&
                (rd.fixedIngredientFilter?.AllowedThingDefs.Any(predicate: td => td.techHediffsTags?.Contains(item: "Advanced") ?? false) ?? false) && !rd.appliedOnFixedBodyParts.NullOrEmpty()).ToList();

            float weaponMoney = ThingDef.Named(defName: "Gun_ChargeRifle").BaseMarketValue * 1.1f;
            List<ThingDef> weaponDefs = ThingCategoryDefOf.Weapons.DescendantThingDefs.ToList();
            float weaponMaxDifference = weaponDefs.Max(selector: td => Mathf.Abs(td.BaseMarketValue - weaponMoney));


            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn colonist = colonists[i];
                Pawn pawn = pawns[i];

                pawn.Name = colonist.Name;
                pawn.story.traits.allTraits = colonist.story.traits.allTraits.ListFullCopy();
                pawn.story.childhood = colonist.story.childhood;
                pawn.story.adulthood = colonist.story.adulthood;
                pawn.skills.skills = colonist.skills.skills.ListFullCopy();
                pawn.health.hediffSet.hediffs = colonist.health.hediffSet.hediffs.ListFullCopy().Where(predicate: hediff => hediff is Hediff_AddedPart).ToList();
                pawn.story.bodyType = colonist.story.bodyType;
                (typeof(Pawn_StoryTracker).GetField(name: "headGraphicPath", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException()).SetValue(pawn.story, colonist.story.HeadGraphicPath);
                FieldInfo recordInfo = typeof(Pawn_RecordsTracker).GetField(name: "records", BindingFlags.NonPublic | BindingFlags.Instance);
                (recordInfo ?? throw new NullReferenceException()).SetValue(pawn.records, recordInfo.GetValue(colonist.records));
                pawn.gender = colonist.gender;
                pawn.story.hairDef = colonist.story.hairDef;
                pawn.story.hairColor = colonist.story.hairColor;
                pawn.apparel.DestroyAll();

                colonist.apparel.WornApparel.ForEach(action: ap =>
                {
                    Apparel copy = ThingMaker.MakeThing(ap.def, ap.Stuff) as Apparel;
                    copy.TryGetComp<CompQuality>().SetQuality(ap.TryGetComp<CompQuality>().Quality, ArtGenerationContext.Colony);
                    pawn.apparel.Wear(copy);
                });

                foreach (FieldInfo fi in typeof(Pawn_AgeTracker).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                    if (!fi.Name.EqualsIgnoreCase(B: "pawn"))
                        fi.SetValue(pawn.ageTracker, fi.GetValue(colonist.ageTracker));

                pawn.story.melanin = colonist.story.melanin;

                for (int x = 0; x < new IntRange(min: 3, max: 8).RandomInRange; x++)
                {
                    recipes.Where(predicate: rd => rd.appliedOnFixedBodyParts != null).TryRandomElement(out RecipeDef recipe);
                    BodyPartRecord record;
                    do
                        record = pawn.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Bullet);
                    while (!recipe.appliedOnFixedBodyParts.Contains(record.def));
                    recipe.Worker.ApplyOnPawn(pawn, record, billDoer: null, recipe.fixedIngredientFilter.AllowedThingDefs.Select(selector: td => ThingMaker.MakeThing(td, td.MadeFromStuff ? GenStuff.DefaultStuffFor(td) : null)).ToList(), bill: null);
                }
                pawn.equipment.DestroyAllEquipment();

                ThingDef weaponDef = weaponDefs.RandomElementByWeight(weightSelector: td => weaponMaxDifference - (td.BaseMarketValue - weaponMoney));
                if (weaponDef.IsRangedWeapon)
                    pawn.apparel.WornApparel.RemoveAll(match: ap => ap.def == ThingDefOf.Apparel_ShieldBelt);
                ThingWithComps weapon = ThingMaker.MakeThing(weaponDef, weaponDef.MadeFromStuff ? ThingDefOf.Plasteel : null) as ThingWithComps;
                weapon.TryGetComp<CompQuality>()?.SetQuality(Rand.Bool ? QualityCategory.Normal : Rand.Bool ? QualityCategory.Good : QualityCategory.Excellent, ArtGenerationContext.Colony);
                pawn.equipment.AddEquipment(weapon);
                //pawn.story.traits.GainTrait(trait: new Trait(def: AnkhDefOf.ankhTraits.RandomElement(), degree: 0, forced: true));
            }

            DropPodUtility.DropThingsNear(parms.spawnCenter, map, pawns, parms.podOpenDelay, canInstaDropDuringInit: false, leaveSlag: true);
            parms.raidStrategy.Worker.MakeLords(parms, pawns);
            map.avoidGrid.Regenerate();

            MoteMaker.ThrowMetaPuffs(CellRect.CenteredOn(parms.spawnCenter, radius: 10), map);


            Find.LetterStack.ReceiveLetter(label: "The gods wrath",
                                           text: "The gods are angry at your colony.", LetterDefOf.ThreatBig, new GlobalTargetInfo(parms.spawnCenter, map));
            return true;
        }
    }

    public class Reward_DebugGivePoints : AchievementReward
    {
        public override bool TryExecuteEvent() => true;
    }
}