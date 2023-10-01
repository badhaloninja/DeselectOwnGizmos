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
        public override string Version => "2.0.0";
        public override string Link => "https://github.com/badhaloninja/DeselectOwnGizmos";
        
        private static ModConfiguration Config;

        [AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> ProtoFluxTool = new ModConfigurationKey<bool>("ProtoFluxTool", "Adds deselect to the Protoflux tool", () => true);

        public override void OnEngineInit()
        {
            Config = GetConfiguration();
            Config.Save(true);
            Harmony harmony = new Harmony("me.badhaloninja.DeselectOwnGizmos");
            harmony.PatchAll();
        }


        [HarmonyPatch(typeof(DevTool), "GenerateMenuItems")]
        class DevToolTipGenerateContext
        {
            static bool IsLocalUserGizmo(SlotGizmo gizmo)
            {
                return gizmo.World.GetUserByAllocationID(gizmo.ReferenceID.User).IsLocalUser;
            }

            public static void Postfix(DevTool __instance, ContextMenu menu, SyncRef<Slot> ____currentGizmo,
                SyncRef<Slot> ____previousGizmo)
            {
                Uri deselect =OfficialAssets.Graphics.Icons.Item.Deselect; //NeosAssets.Graphics.Icons.Item.Deselect;
                ContextMenuItem item = menu.AddItem("Deselect Own", deselect, colorX.White);
                item.Button.LocalPressed += (IButton button, ButtonEventData eventData) =>
                {
                    __instance.World.RootSlot.GetComponentsInChildren<SlotGizmo>(IsLocalUserGizmo)
                        .ForEach((SlotGizmo s) => s.Slot.Destroy());

                    ____currentGizmo.Target = null;
                    ____previousGizmo.Target = null;

                    InteractionHandler activeTool = __instance.ActiveHandler;
                    if (activeTool != null)
                        activeTool.CloseContextMenu();

                    SelectAnchor(__instance, null);
                };
            }

            [HarmonyReversePatch]
            [HarmonyPatch(typeof(DevTool), "SelectAnchor")]
            public static void SelectAnchor(DevTool instance, PointAnchor pointAnchor) =>
                throw new NotImplementedException("It's a stub");
        }

        [HarmonyPatch(typeof(SlotRecord), "Pressed")]
        class NonHostInspectorGizmoCreation
        {
            public static bool Prefix(SlotRecord __instance, ref double ____lastPress)
            {
                if (__instance.Time.WorldTime - ____lastPress < 0.35)
                {
                    __instance.Slot.GetComponentInParents<SceneInspector>().ComponentView.Target =
                        __instance.TargetSlot.Target;
                    if (!__instance.World.IsAuthority)
                    {
                        // Create gizmo on double press as self instead of have host do it
                        __instance.TargetSlot.Target.GetGizmo();
                    }
                }

                ____lastPress = __instance.Time.WorldTime;
                return false;
            }
        }

        [HarmonyPatch(typeof(ProtoFluxTool), "GenerateMenuItems")]
        class LogixTipGenerateContext
        {
            static bool IsLocalUserGizmo(SlotGizmo gizmo)
            {
                return gizmo.World.GetUserByAllocationID(gizmo.ReferenceID.User).IsLocalUser;
            }

            public static void Postfix(ProtoFluxTool __instance, ContextMenu menu)
            {
                if (Config.GetValue(ProtoFluxTool))
                {
                    Uri deselect =
                        OfficialAssets.Graphics.Icons.Item.Deselect; //NeosAssets.Graphics.Icons.Item.Deselect;
                    ContextMenuItem item = menu.AddItem("Deselect Own", deselect, colorX.White);
                    item.Button.LocalPressed += (IButton button, ButtonEventData eventData) =>
                    {
                        __instance.World.RootSlot.GetComponentsInChildren<SlotGizmo>(IsLocalUserGizmo)
                            .ForEach((SlotGizmo s) => s.Slot.Destroy());
                        InteractionHandler activeTool = __instance.ActiveHandler;
                        if (activeTool != null)
                            activeTool.CloseContextMenu();
                    };
                }
            }
        }
    }
}