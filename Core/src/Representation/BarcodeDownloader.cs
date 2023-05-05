using LabFusion.Representation;
using System.Threading.Tasks;
using HarmonyLib;
using SLZ.Marrow.Forklift;
using MelonLoader;
using System.Reflection;
using System;
using System.Runtime.InteropServices;
using SLZ.Combat;
using Cysharp.Threading.Tasks;
using SLZ.Marrow.Forklift.Model;
using Il2CppSystem.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using SLZ.Marrow.Warehouse;
using static SLZ.Marrow.Forklift.ModDownloadManager;
using UnityEngine.Networking;

namespace LabFusion.Core.src.Representation
{
    internal class BarcodeDownloader
    {
        public static ModDownloadManager LatestDownloadManager;
        public static List<ModRepository> FetchedRepositories;
        public static System.Collections.Generic.Dictionary<string, ModListing> AllModListings;
        public static System.Collections.Generic.List<DownloadWaiter> WaitingReps;
        public static System.Collections.Generic.List<string> DoneReps;
        public static void TryDownloadBarcode(PlayerRep targPlayer, string barcode)
        {
            if (FetchedRepositories == null || AllModListings == null) return;

            if (WaitingReps == null) WaitingReps = new System.Collections.Generic.List<DownloadWaiter>();
            if (DoneReps == null) DoneReps = new System.Collections.Generic.List<string>();

            if (DoneReps.Contains(barcode)) return;
            MelonLogger.Msg("repos & list not null");
            string[] barcodeSections = barcode.Split('.');
            if (barcodeSections.Length > 3 ) 
            {
                MelonLogger.Msg("barcode valid");
                if (AllModListings.TryGetValue(barcodeSections[0] + '.' + barcodeSections[1], out ModListing foundMod))
                {
                    MelonLogger.Msg("found in AllModListings");
                    foreach (KeyValuePair<string, ModTarget> target in foundMod.Targets)
                    {
                        if (target.Key == "pc")
                        {
                            MelonLogger.Msg("pc compat");
                            LatestDownloadManager.DownloadMod(foundMod, target.value);
                            //WaitingReps.Add(new DownloadWaiter() { avatarId = targPlayer.avatarId, player = targPlayer, Url = finalTarg.Url });
                            MelonLogger.Msg("pending completion of download");
                            DoneReps.Add(barcode);
                            break;
                        }
                    }
                }
            }
            return;
            //if (targPlayer != null && targPlayer.avatarId == barcode) 
            //{
            //    targPlayer.MarkDirty();
            //}
        }
        private static async void DownloadComplete(FileDownloader.TaskItem taskItem)
        {
            MelonLogger.Msg("mod downloaded!!");
            await Task.Delay(800);
            foreach (PlayerRep rep in PlayerRepManager.PlayerReps)
            {
                rep.MarkDirty();
            }
            /*Console.WriteLine(taskItem.url.AbsoluteUri);
            foreach (DownloadWaiter rep in WaitingReps)
            {
                if (rep.Url == taskItem.url.AbsoluteUri)
                {
                    MelonLogger.Msg("found targplr");
                    if (rep.avatarId == rep.player.avatarId)
                    {
                        MelonLogger.Msg("relevent to them & dirted");
                        rep.player.MarkDirty();
                    }
                    break;
                }
            }*/
        }
        private static async void SetupRepos(UniTask<List<ModRepository>> repoFetcher)
        {
            if (FetchedRepositories != null || AllModListings != null) return;
            FetchedRepositories = await repoFetcher;

            AllModListings = new System.Collections.Generic.Dictionary<string, ModListing>();
            foreach (ModRepository modRepo in FetchedRepositories)
            {
                foreach (ModListing mod in modRepo.Mods)
                {
                    try
                    {
                        AllModListings.Add(mod.Barcode, mod);
                    } catch { }
                }
            }
            
            //printRepositories();
        }
        private static void printRepositories()
        {
            if (FetchedRepositories == null) return;

            string o = "\nModRepositories:\n{";
            string divider = "\n    ";
            foreach (ModRepository modRepo in FetchedRepositories)
            {
                o += divider + ($"Title: {modRepo.Title}");
                o += divider + ($"Description: {modRepo.Description}");
                o += divider + ($"Length: {modRepo.Mods.Count}");
                o += divider + ("Mods:");
                o += divider + ("{");
                divider = "\n        ";
                foreach (ModListing mod in modRepo.Mods)
                {
                    o += divider + mod.Barcode;
                }
                divider = "\n    ";
                o += divider + ("}");
                divider = "\n";
                o += divider + ("}");
            }
            MelonLogger.Msg(o);
        }
        public static void Patch()
        {
            PatchDMDctor();
            PatchFetchRepositoriesAsync();
            PatchModDownloadManager_OnDownloadFinished();
        }

        #region DMDctor patching
        private static DMDctorPatchDelegate _original;

        public delegate void DMDctorPatchDelegate(IntPtr instance, IntPtr method);

        private unsafe static void PatchDMDctor()
        {
            DMDctorPatchDelegate patch = DMDctor;

            // Mouthful
            string nativeInfoName = "NativeMethodInfoPtr__ctor_Public_Void_0";

            var tgtPtr = *(IntPtr*)(IntPtr)typeof(ModDownloadManager).GetField(nativeInfoName, AccessTools.all).GetValue(null);
            var dstPtr = patch.Method.MethodHandle.GetFunctionPointer();

            MelonUtils.NativeHookAttach((IntPtr)(&tgtPtr), dstPtr);
            _original = Marshal.GetDelegateForFunctionPointer<DMDctorPatchDelegate>(tgtPtr);
        }

        private static void DMDctor(IntPtr instance, IntPtr method)
        {
            LatestDownloadManager = new ModDownloadManager(instance);
            if (FetchedRepositories == null)
            {
                SetupRepos(LatestDownloadManager.FetchRepositoriesAsync(""));
            }
            _original(instance, method);
        }
        #endregion
        
        
        #region FetchRepositoriesAsync patching
        private static FetchRepositoriesAsyncDelegate _original2;

        public delegate IntPtr FetchRepositoriesAsyncDelegate(IntPtr instance, IntPtr parent, IntPtr method);

        private unsafe static void PatchFetchRepositoriesAsync()
        {
            FetchRepositoriesAsyncDelegate patch = FetchRepositoriesAsync;

            // Mouthful
            string nativeInfoName = "NativeMethodInfoPtr_FetchRepositoriesAsync_Public_UniTask_1_List_1_ModRepository_String_0";

            var tgtPtr = *(IntPtr*)(IntPtr)typeof(ModDownloadManager).GetField(nativeInfoName, AccessTools.all).GetValue(null);
            var dstPtr = patch.Method.MethodHandle.GetFunctionPointer();

            MelonUtils.NativeHookAttach((IntPtr)(&tgtPtr), dstPtr);
            _original2 = Marshal.GetDelegateForFunctionPointer<FetchRepositoriesAsyncDelegate>(tgtPtr);
        }

        private static IntPtr FetchRepositoriesAsync(IntPtr instance, IntPtr parent, IntPtr method)
        {
            //MelonLogger.Msg(Marshal.PtrToStringAuto(parent));
            IntPtr oPtr = _original2(instance, parent, method);
            return oPtr;
        }
        #endregion

        #region ModDownloadManager_OnDownloadFinished patching
        private static ModDownloadManager_OnDownloadFinishedDelegate _original3;

        public delegate void ModDownloadManager_OnDownloadFinishedDelegate(IntPtr instance, IntPtr FileDownloader, IntPtr uwr, IntPtr taskItem, IntPtr method);

        private unsafe static void PatchModDownloadManager_OnDownloadFinished()
        {
            ModDownloadManager_OnDownloadFinishedDelegate patch = ModDownloadManager_OnDownloadFinished;

            // Mouthful
            string nativeInfoName = "NativeMethodInfoPtr_ModDownloadManager_OnDownloadFinished_Private_Void_FileDownloader_UnityWebRequest_TaskItem_0";

            var tgtPtr = *(IntPtr*)(IntPtr)typeof(ModDownloadManager).GetField(nativeInfoName, AccessTools.all).GetValue(null);
            var dstPtr = patch.Method.MethodHandle.GetFunctionPointer();

            MelonUtils.NativeHookAttach((IntPtr)(&tgtPtr), dstPtr);
            _original3 = Marshal.GetDelegateForFunctionPointer<ModDownloadManager_OnDownloadFinishedDelegate>(tgtPtr);
        }

        private static void ModDownloadManager_OnDownloadFinished(IntPtr instance, IntPtr FileDownloader, IntPtr uwr, IntPtr taskItem, IntPtr method)
        {
            DownloadComplete(new FileDownloader.TaskItem(taskItem));
            _original3(instance, FileDownloader, uwr, taskItem, method);
        }
        #endregion
    }
    internal class DownloadWaiter
    {
        public string Url;
        public string avatarId;
        public PlayerRep player;
    }
}