using BaseX;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;

namespace DeselectOwnGizmos
{
    public class DeselectOwnGizmos : NeosMod
    {
        public override string Name => "DeselectOwnGizmos";
        public override string Author => "badhaloninja";
        public override string Version => "1.1.0";
        public override string Link => "https://github.com/badhaloninja/DeselectOwnGizmos";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.badhaloninja.DeselectOwnGizmos");
            harmony.PatchAll();
        }


        [HarmonyPatch(typeof(DevToolTip), "GenerateMenuItems")]
        class DevToolTipGenerateContext
        {
            static bool IsLocalUserGizmo(SlotGizmo gizmo)
            {
                return gizmo.World.GetUserByAllocationID(gizmo.ReferenceID.User).IsLocalUser;
            }
            public static void Postfix(DevToolTip __instance, ContextMenu menu, SyncRef<Slot> ____currentGizmo, SyncRef<Slot> ____previousGizmo)
            {
                Uri deselect = new Uri("neosdb:///f9e3d4646db0607fcbc73981a97cb4f3875dc787fcaa3a6560874b207a95d895.png"); //NeosAssets.Graphics.Icons.Item.Deselect;
                ContextMenuItem item = menu.AddItem("Deselect Own", deselect, color.White);
                item.Button.LocalPressed += (IButton button, ButtonEventData eventData) =>
                {
                    __instance.World.RootSlot.GetComponentsInChildren<SlotGizmo>(IsLocalUserGizmo).ForEach((SlotGizmo s) => s.Slot.Destroy());

                    ____currentGizmo.Target = null;
                    ____previousGizmo.Target = null;
                    
                    CommonTool activeTool = __instance.ActiveTool;
                    if (activeTool != null)
                        activeTool.CloseContextMenu();
                    
                    SelectAnchor(__instance, null);
                };
            }

            [HarmonyReversePatch]
            [HarmonyPatch(typeof(DevToolTip), "SelectAnchor")]
            public static void SelectAnchor(DevToolTip instance, PointAnchor pointAnchor) => throw new NotImplementedException("It's a stub");
        }
        [HarmonyPatch(typeof(SlotRecord), "Pressed")]
        class NonHostInspectorGizmoCreation
        {
            public static bool Prefix(SlotRecord __instance, ref double ____lastPress)
            {
                if (__instance.Time.WorldTime - ____lastPress < 0.35)
                {
                    __instance.Slot.GetComponentInParents<SceneInspector>().ComponentView.Target = __instance.TargetSlot.Target;
                    if (!__instance.World.IsAuthority)
                    { // Create gizmo on double press as self instead of have host do it
                        __instance.TargetSlot.Target.GetGizmo(); 
                    }
                }
                ____lastPress = __instance.Time.WorldTime;
                return false;
            }
        }
    }
}