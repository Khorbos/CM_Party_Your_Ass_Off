using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CM_Party_Your_Ass_Off
{
    [StaticConstructorOnStartup]
    public static class FloatMenuMakerMap_Patches
    {
        [HarmonyPatch(typeof(FloatMenuMakerMap))]
        [HarmonyPatch("AddHumanlikeOrders", MethodType.Normal)]
        public static class FloatMenuMakerMap_AddHumanlikeOrders
        {
            [HarmonyPostfix]
            public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
            {
                IntVec3 clickedCell = IntVec3.FromVector3(clickPos);

                foreach (Thing thing in clickedCell.GetThingList(pawn.Map))
                {
                    GatheringDef gatheringDef = GatheringDefOf.Party;
                    {
                        if (gatheringDef.gatherSpotDefs.Contains(thing.def))
                        {
                            string optionString = "";

                            bool canPartyHere = CanPartyHere(clickedCell, pawn, out optionString);
                            if (!canPartyHere)
                            {
                                opts.Add(new FloatMenuOption(optionString, null));
                            }
                            else
                            {
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(optionString, delegate
                                {
                                    if (TryStartParty(gatheringDef, pawn, thing.Position))
                                    {
                                        // Let the VoluntarilyJoinableLordsStarter for this map know we are throwing a party so we don't get a random party directly after
                                        Map map = pawn.Map;
                                        if (map.lordsStarter != null)
                                        {
                                            FieldInfo lastLordStartTickField = typeof(VoluntarilyJoinableLordsStarter).GetField("lastLordStartTick", BindingFlags.NonPublic | BindingFlags.Instance);
                                            if (lastLordStartTickField != null)
                                                lastLordStartTickField.SetValue(map.lordsStarter, Find.TickManager.TicksGame);
                                        }
                                    }
                                }, MenuOptionPriority.High), pawn, thing));
                            }
                        }
                    }
                }
            }

            public static bool TryStartParty(GatheringDef gatheringDef, Pawn organizer, IntVec3 spot)
            {
                MethodInfo createLordJobMethod = gatheringDef.workerClass.GetMethod("CreateLordJob", BindingFlags.NonPublic | BindingFlags.Instance);
                LordJob lordJob = (LordJob)createLordJobMethod.Invoke(gatheringDef.Worker, new object[] { spot, organizer });

                LordMaker.MakeNewLord(organizer.Faction, lordJob, organizer.Map, (!lordJob.OrganizerIsStartingPawn) ? null : new Pawn[1]
                {
                    organizer
                });

                MethodInfo sendLetterMethod = gatheringDef.workerClass.GetMethod("SendLetter", BindingFlags.NonPublic | BindingFlags.Instance);
                sendLetterMethod.Invoke(gatheringDef.Worker, new object[] { spot, organizer });

                return true;
            }

            private static bool CanPartyHere(IntVec3 cell, Pawn organizer, out string reasonString)
            {
                Map map = organizer.Map;

                // Things to check if further restriction needs to be done
                //JoyUtility.EnjoyableOutsideNow
                //GatheringsUtility.ValidateGatheringSpot_NewTemp
                //GatheringsUtility.PawnCanStartOrContinueGathering

                if (!cell.Standable(map) || cell.IsForbidden(organizer) || !organizer.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.None))
                {
                    reasonString = "CM_Party_Your_Ass_Off_Cannot_Party_Here".Translate() + ": " + "NoPath".Translate().CapitalizeFirst();
                    return true;
                }

                reasonString = "CM_Party_Your_Ass_Off_Throw_Party_Here".Translate();
                return true;
            }
        }
    }
}
