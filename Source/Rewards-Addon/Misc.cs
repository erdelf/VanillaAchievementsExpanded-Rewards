using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rewards_Addon
{
    using RimWorld;
    using Verse;

    public class PawnGroupKindWorker_Wrath : PawnGroupKindWorker_Normal
    {

        protected override void GeneratePawns(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns, bool errorOnZeroResults = true)
        {
            bool  allowFood      = parms.raidStrategy == null || parms.raidStrategy.pawnsCanBringFood;
            bool  forceIncapDone = false;
            float points         = parms.points;
            while (points > 0)
            {
                Pawn p = PawnGenerator.GeneratePawn(request: new PawnGenerationRequest(kind: PawnKindDefOf.AncientSoldier, faction: parms.faction, context: PawnGenerationContext.NonPlayer, tile: parms.tile, forceGenerateNewPawn: true, newborn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, colonistRelationChanceFactor: 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowFood: allowFood));
                p.InitializeComps();
                if (parms.forceOneIncap && !forceIncapDone)
                {
                    p.health.forceIncap           = true;
                    p.mindState.canFleeIndividual = false;
                    forceIncapDone                = true;
                }
                points -= p.kindDef.combatPower;
                outPawns.Add(item: p);
            }
        }
    }
}
