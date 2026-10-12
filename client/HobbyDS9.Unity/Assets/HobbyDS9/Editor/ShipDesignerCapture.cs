using System;
using HobbyDS9.UnityClient;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient.Editor
{
    public static class ShipDesignerCapture
    {
        private static readonly Vector2Int[] Sizes={new Vector2Int(1920,1080),new Vector2Int(3440,1440),new Vector2Int(1280,720)};
        [MenuItem("HobbyDS9/LCARS/Capture ship designer preview fixtures")]
        public static void CaptureFixtures()
        {
            if(!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode and open Ship design from strategic navigation.");
            var entry=UnityEngine.Object.FindAnyObjectByType<ClientEntry>();
            var root=entry==null?null:entry.GetComponent<UIDocument>()?.rootVisualElement;
            var shell=root?.Q<LcarsShell>(); var view=root?.Q<ShipDesignerView>();
            if(shell==null || !shell.DesignerOpen || view==null) throw new InvalidOperationException("Open Ship design before capture.");
            if(view.Draft.Modified || view.Draft.ConfirmationPending) throw new InvalidOperationException("Keep your local edits or explicitly reset them before fixture capture; this tool will not discard a modified draft.");
            if(!view.Draft.Provider.FixtureResults) throw new InvalidOperationException("This capture sequence is for the labelled fixture provider.");
            var original=view.Draft.TemplateId; var profile=view.Draft.Profile;
            view.Draft.RequestTemplate("demo.surveyor"); view.Draft.SelectProfile(DesignerProfile.Survey);
            StrategicPrototypeCapture.CaptureSequence("ship-designer-prototype","surveyor",Sizes,Validate,()=>
            {
                view.Draft.RequestTemplate("demo.escort"); view.Draft.SelectProfile(DesignerProfile.Combat);
                StrategicPrototypeCapture.CaptureSequence("ship-designer-prototype","escort",Sizes,Validate,()=>
                {
                    view.Draft.RequestTemplate(original); view.Draft.SelectProfile(profile);
                    Debug.Log("Ship designer: six real Game view screenshots complete. Inspect and perform native interaction checks before accepting.");
                });
            });
        }
        private static void Validate(VisualElement root)
        {
            var shell=root.Q<LcarsShell>(); var view=root.Q<ShipDesignerView>();
            if(shell==null || !shell.DesignerOpen || view==null) throw new InvalidOperationException("Designer closed during capture.");
            foreach(var button in new[] {view.ResetButton,view.CloseButton})
            {
                var bounds=button.worldBound;
                if(bounds.width<80 || bounds.height<40 || !view.worldBound.Contains(bounds.min) || !view.worldBound.Contains(bounds.max))
                    throw new InvalidOperationException("A primary designer action is clipped or undersized.");
            }
            if(!view.Q<Label>("designer-provider-status").text.Contains("DEMO")) throw new InvalidOperationException("Fixture/demo provenance is missing.");
            if(view.Q<Button>("designer-construction").enabledSelf) throw new InvalidOperationException("Construction must remain unavailable.");
        }
    }
}
