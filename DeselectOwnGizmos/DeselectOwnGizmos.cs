using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using FrooxEngine.ProtoFlux;

namespace DeselectOwnGizmos
{
    public class DeselectOwnGizmos : ResoniteMod
    {
        public override string Name => "DeselectOwnGizmos";
        public override string Author => "badhaloninja";
        public override string Version => "2.0.1";
        public override string Link => "https://github.com/badhaloninja/DeselectOwnGizmos";
        
        private static ModConfiguration Config;

        [AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> ShowOnProtoFluxTool = new("ShowOnProtoFluxTool", "Adds deselect to the Protoflux tool", () => false);

        public override void OnEngineInit()
        {
            Config = GetConfiguration();

            Harmony harmony = new("ninja.badhalo.DeselectOwnGizmos");
            harmony.PatchAll();
        }

        const string DeselectOwnIcon = "6c6fe0b17b9f9fc07d9c47363988eb98560ff2daf81132bc041bb4ef6a487c18";

        [HarmonyPatch]
        class ToolPatches
        {

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DevTool), "GenerateMenuItems")]
            public static void AddDeselectButton(DevTool __instance, ContextMenu menu, SyncRef<Slot> ____currentGizmo, SyncRef<Slot> ____previousGizmo)
            {
                Uri deselect = __instance.Cloud.Assets.GenerateURL(DeselectOwnIcon);
                ContextMenuItem item = menu.AddItem("Deselect Own", deselect, colorX.White);

                item.Button.LocalPressed += (IButton button, ButtonEventData eventData) => Deselect(__instance, ____currentGizmo, ____previousGizmo);
            }
            
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ProtoFluxTool), "GenerateMenuItems")]
            public static void FluxAddDeselectButton(ProtoFluxTool __instance, ContextMenu menu)
            {
                if (!Config.GetValue(ShowOnProtoFluxTool)) return;

                Uri deselect = __instance.Cloud.Assets.GenerateURL(DeselectOwnIcon);
                ContextMenuItem item = menu.AddItem("Deselect Own", deselect, colorX.White);

                item.Button.LocalPressed += (IButton button, ButtonEventData eventData) => Deselect(__instance);
            }


            private static void Deselect(Tool tool, SyncRef<Slot> currentGizmo = null, SyncRef<Slot> previousGizmo = null)
            {
                tool.World.RootSlot.GetComponentsInChildren<SlotGizmo>(IsLocalUserGizmo).ForEach((SlotGizmo s) => s.Slot.Destroy());
                tool.ActiveHandler?.CloseContextMenu();

                if (tool is DevTool devTool)
                {
                    currentGizmo.Target = null;
                    previousGizmo.Target = null;
                    SelectAnchor(devTool, null);
                }
            }
            static bool IsLocalUserGizmo(SlotGizmo gizmo) => gizmo.World.GetUserByAllocationID(gizmo.ReferenceID.User).IsLocalUser;


            [HarmonyReversePatch]
            [HarmonyPatch(typeof(DevTool), "SelectAnchor")]
            public static void SelectAnchor(DevTool instance, PointAnchor pointAnchor) => throw new NotImplementedException("It's a stub");
        }

        [HarmonyPatch(typeof(SlotRecord), "Pressed")]
        class GenerateGizmoFromInspector
        {
            public static void Prefix(SlotRecord __instance, ref double ____lastPress)
            {
                if (__instance.World.IsAuthority || !(__instance.Time.WorldTime - ____lastPress < 0.35)) return;

                if (__instance.TargetSlot.Target != null && !__instance.TargetSlot.Target.IsRootSlot)
                    __instance.TargetSlot.Target?.GetGizmo();
            }
        }

        [HarmonyPatch(typeof(DevCreateNewForm), "OpenInspector")]
        class GenerateGizmoCreateNew
        {
            public static void Prefix(Slot slot)
            {
                if (!slot.World.IsAuthority) slot.GetGizmo();
            }
        }
    }
}