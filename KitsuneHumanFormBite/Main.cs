using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Items;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityEngine;
using UnityModManagerNet;

namespace KitsuneHumanFormBite
{
    public class Main
    {
        public static bool Enabled;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            modEntry.OnGUI = OnGUI;
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            return true;
        }

        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            var party = Game.Instance.Player.Party.ToList();
            var player = party[0];
            GUILayout.Label("Is Polymorphed: " + player.Body.IsPolymorphed, GUILayout.ExpandWidth(false));
            foreach (var limb in player.Body.AdditionalLimbs)
            {
                GUILayout.Label($"Limb: {(limb.MaybeItem != null ? "Has item" : "No item")}. Eligible for polymorph: {(limb.MaybeItem as ItemEntityWeapon)?.Blueprint.KeepInPolymorph}. Id: {(limb.MaybeItem as ItemEntityWeapon)?.Blueprint?.AssetGuid}", GUILayout.ExpandWidth(false));
            }
            GUILayout.Label("Additional Limbs Eligible For Polymorph: " + player.Body.AdditionalLimbs.Count(l =>
            {
                ItemEntityWeapon itemEntityWeapon = l?.MaybeItem as ItemEntityWeapon;
                return (itemEntityWeapon != null && itemEntityWeapon.Blueprint.KeepInPolymorph);
            }), GUILayout.ExpandWidth(false));
        }

        /// <summary>
        /// We cannot modify blueprints until after the game has loaded them. We patch BlueprintsCache.Init
        /// to initialize our modifications as soon as the game blueprints have loaded.
        /// </summary>
        [HarmonyPatch(typeof(BlueprintsCache))]
        public static class BlueprintsCache_Patches
        {
            public static bool loaded = false;

            [HarmonyPriority(Priority.First)]
            [HarmonyPatch(nameof(BlueprintsCache.Init)), HarmonyPostfix]
            public static void Postfix()
            {
                if (loaded) return;
                loaded = true;

                var polymorphableFeatures = new List<string>
                {
                    // Standard Kitsune bite
                    // UnitBody only knows about the kitsune bite,
                    // but the damage logic looks at all the features and takes the most powerful (like the dragon bites)
                    "35dfad6517f401145af54111be04d6cf",

                    // Dragon Disciple bites
                    "ec17d0f1d77cf00439316ceddfafa0f8",
                    "c66afbc07845e4245bf62021b7278a43",
                    "443e310d801832449870491cdad0632d",
                    "82b3f00299c928b46a578a636dca1c0d",
                    "75cedf4d76ed27341b435877d473194d"
                };
                foreach (var blueprintId in polymorphableFeatures) 
                {
                    var blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintItemWeapon>(blueprintId);
                    blueprint.KeepInPolymorph = true;
                }

                var kitsunePolymorphBuff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("ee6c7f5437a57ad48aaf47320129df33");
                var addFactContextActionsFeature = (AddFactContextActions)kitsunePolymorphBuff.ComponentsArray.First(f => f is AddFactContextActions);
                addFactContextActionsFeature.Activated.Actions = addFactContextActionsFeature.Deactivated.Actions.ToArray();
            }
        }
    }
}
