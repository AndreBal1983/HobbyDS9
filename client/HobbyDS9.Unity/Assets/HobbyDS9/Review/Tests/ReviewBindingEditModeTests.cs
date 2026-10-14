#nullable enable
using System;
using System.IO;
using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using NUnit.Framework;
using UnityEngine;

namespace HobbyDS9.UnityReview.Tests
{
    // AUTHORED ONLY: NOT EXECUTED IN EDITOR during overnight source work.
    // Requires Primary's delivered PrepareUnityReview assets/DLL, not default preparation.
    [Category("PhysicalReviewBinding")]
    public sealed class ReviewBindingEditModeTests
    {
        private string directory=null!;
        private ReviewNewtonsoftCodec codec=null!;
        private CampaignTacticalTravelPackageDto package=null!;
        [SetUp] public void Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"hobby-editor-review-"+Guid.NewGuid().ToString("N"));
            codec=new ReviewNewtonsoftCodec();
            byte[] Bytes(string name)
            {
                var asset=Resources.Load<TextAsset>("ReviewPackages/faster/"+name);
                Assert.That(asset,Is.Not.Null,"Run delivered review preparation before explicit review tests.");return asset.bytes;
            }
            package=ReviewExactPackageLoader.Load(Bytes("package"),Bytes("core"),Bytes("scenario"),codec);
        }
        [TearDown] public void Cleanup()
        {
            var full=Path.GetFullPath(directory);var temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(!full.StartsWith(temp+Path.DirectorySeparatorChar,StringComparison.Ordinal)||!Path.GetFileName(full).StartsWith("hobby-editor-review-",StringComparison.Ordinal))throw new InvalidOperationException();
            if(Directory.Exists(full))Directory.Delete(full,true);
        }
        [Test] public void ActualResourceCodecLoadsCompletePhysicalRootWithNullInitialProof()
        {
            var session=CampaignTacticalTravelApplication.StartReviewPackage(package);
            var root=codec.DecodeRoot(codec.EncodeRoot(session.ExportSave()));
            Assert.That(root.FormatVersion,Is.EqualTo(2));Assert.That(root.MovementProof,Is.Null);
            Assert.That(CampaignTacticalTravelApplication.LoadReviewSave(package,root).CaptureStatus().Phase,Is.EqualTo(SessionPhase.Planning));
            Assert.Throws<InvalidDataException>(()=>codec.DecodeRoot(Encoding.UTF8.GetBytes("{\"FormatVersion\":\"2\"}")));
        }
        [Test] public void ActualWaitingCommitmentSaveReopensAndResolvesOnlyAfterOtherHumanOrders()
        {
            var store=new ReviewUnitySlotStore(directory,codec);var ui=new ReviewUiModel(package,codec,store,"civ.federation");
            Assert.That(ui.DraftTravel("fleet.federation-scout",CampaignTravelIntent.StayEngage,null,null),Is.True);
            Assert.That(ui.SendTravel("command.editor.a"),Is.True);Assert.That(ui.SwitchActor("civ.cardassian"),Is.True);
            Assert.That(ui.DraftTravel("fleet.cardassian-scout",CampaignTravelIntent.StayEngage,null,null),Is.True);
            Assert.That(ui.SendTravel("command.editor.b"),Is.True);Assert.That(ui.Advance(),Is.True);
            Assert.That(ui.CommitRound("command.editor.round.b"),Is.True);Assert.That(ui.Current.Mode,Is.EqualTo(ReviewMode.Waiting));
            Assert.That(ui.Save("waiting"),Is.True);Assert.That(ui.Load("waiting",true),Is.True);
            Assert.That(ui.Current.Frame.Battle!.Round,Is.EqualTo(1));Assert.That(ui.SwitchActor("civ.federation"),Is.True);
            Assert.That(ui.CommitRound("command.editor.round.a"),Is.True);Assert.That(ui.Current.Frame.Battle!.Round,Is.EqualTo(2));
        }
        [TestCase(UnityReviewWriteStage.BeforeWrite)]
        [TestCase(UnityReviewWriteStage.AfterPartialWrite)]
        [TestCase(UnityReviewWriteStage.BeforeReplace)]
        public void FailedAtomicOverwritePreservesPriorReviewFile(UnityReviewWriteStage stage)
        {
            bool fail=false;var store=new ReviewUnitySlotStore(directory,codec,s=>{if(fail&&s==stage)throw new IOException();});
            var root=CampaignTacticalTravelApplication.StartReviewPackage(package).ExportSave();store.Write("kept",root,false);
            var path=Path.Combine(directory,"unity-physical-review-kept.json");var before=File.ReadAllBytes(path);fail=true;
            Assert.Throws<ReviewCheckpointStoreException>(()=>store.Write("kept",root,true));Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));
        }
    }
}
