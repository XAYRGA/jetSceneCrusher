using System;
using System.Collections;
using System.Collections.Generic;
using xayrga;
using Newtonsoft.Json;
using System.IO;
using System.Numerics;

namespace jetSceneCrusher
{


    class Program
    {

        static BinaryReader binexecReader;
        static uint ASSET_TABLE_ADDRESS = 0;
        static uint TEXTURE_TABLE_ADDRESS = 0;
        static uint ASSET_COUNT = 0;
        static uint OBJECT_COUNT = 0;
        static uint OBJECT_TABLE_ADDRESS = 0; 
        static uint GLOBAL_TEXLIST_TABLE_ADDRESS = 0;
        static AFSOffset SCENE_FILE = new AFSOffset();
        static AFSOffset[] TEXTURE_AFS_INDICES = new AFSOffset[0];

     

        static void Main(string[] args)
        {
            var execFile = "JET.BIN"; 
            cmdarg.assert(!File.Exists(execFile),"Cannot find executable file.");
            try { binexecReader = new BinaryReader(File.OpenRead(execFile)); } catch { cmdarg.assert("Cannot open executable file"); }
            cmdarg.assert(!Directory.Exists("JETRADIO"), "Cannot find JETRADIO directory");


            // Stage 1 -- Slice 1 // 

            ASSET_TABLE_ADDRESS = 0x8c1063b4;
            TEXTURE_TABLE_ADDRESS = 0x8c106648;
            OBJECT_TABLE_ADDRESS = 0x8c105e98;
            GLOBAL_TEXLIST_TABLE_ADDRESS = 0x8c1a27c8;
            SCENE_FILE = new AFSOffset { Filename = "STAGE1.AFS", FileIndex = 0, Offset = 0 };
            ASSET_COUNT = 165;
            OBJECT_COUNT = 62;
            TEXTURE_AFS_INDICES = new AFSOffset[]
            {
               new AFSOffset{ Filename = "STAGE1TXP_ALL.AFS", FileIndex = 0, Offset = 0 }
            };
            runProcess();

            Console.ReadLine();

        }

        public static void runProcess()
        {
            var texBuilder = new TextureListDictionaryBuilderV1(binexecReader, GLOBAL_TEXLIST_TABLE_ADDRESS, TEXTURE_AFS_INDICES);
            var tlistp = texBuilder.build();
            var assMan = new AssetTableConverter(binexecReader, ASSET_TABLE_ADDRESS, TEXTURE_TABLE_ADDRESS, ASSET_COUNT, SCENE_FILE);
            var assets = assMan.processAssetTable();
            var objMan = new GameObjTableLoaderV1(binexecReader, OBJECT_TABLE_ADDRESS, OBJECT_COUNT, SCENE_FILE);
            var gobj = objMan.processAssetTable();

       
            attachTexlists(assets, tlistp);
            var scn = new JSRScene()
            {
                Assets = assets,
                Textures = tlistp.Textures,
                MapParts = gobj
            };
            File.WriteAllText("SCENE.JSON", Newtonsoft.Json.JsonConvert.SerializeObject(scn, Formatting.Indented));
        }

        public static void attachTexlists(JSRAsset[] assets, TextureListDictionaryBuilderV1.TextureListDictionaryBuilderPayload texdata)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                var currentAsset = assets[i];
                var addr = currentAsset.TextureAddress;
                var keyk = 0xFFFFFFFFu;
                if (!texdata.NormalizerMap.TryGetValue(addr, out keyk))
                {
                    Console.WriteLine($"Failed to attach texture map {addr:X} to asset {i:X} (doesn't exist in manifest).");
                    continue; // remains null. 
                }
                var texmap = texdata.TexLists[keyk];
                currentAsset.Texlist = texmap;
            }
        }

     
    }
}
