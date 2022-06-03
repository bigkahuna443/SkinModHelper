using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.Client.Entities;
using Celeste.Mod.CelesteNet.DataTypes;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SkinModHelper.Module
{

    public class SkinModHelperCelesteNet
    {
        public static void HandleDataReady(CelesteNetConnection con, DataReady data)
        {
            con.Send(new SkinModHelperChange {SkinID = SkinModHelperModule.Settings.SelectedSkinMod});
        }

        private static DataHandler handleDataReady;

        public static void HandleSkinModHelperChange(CelesteNetConnection con, SkinModHelperChange data)
        {
            SkinModHelperModule.GhostIDRefreshSet.Add(data.ChangePlayer.ID);
        }

        private static DataHandler handleSkinModHelperChange;
        
        public static void Load()
        {
            CelesteNetClientContext.OnInit += clientContext =>
            {
                CelesteNetClient client = clientContext.Client;
                handleDataReady = client.Data.RegisterHandler<DataReady>(HandleDataReady);
                handleSkinModHelperChange = client.Data.RegisterHandler<SkinModHelperChange>(HandleSkinModHelperChange);
            };
        }

        public static void Unload()
        {
            CelesteNetClient client = CelesteNetClientModule.Instance.Client;
            if (client != null)
            {
                if (handleDataReady != null)
                {
                    client.Data.UnregisterHandler(typeof(DataReady), handleDataReady);
                    handleDataReady = null;
                }

                if (handleSkinModHelperChange != null)
                {
                    client.Data.UnregisterHandler(typeof(SkinModHelperChange), handleSkinModHelperChange);
                    handleSkinModHelperChange = null;
                }
            }
        }
    }
    
    public class SkinModHelperChange : DataType<SkinModHelperChange>
    {
        static SkinModHelperChange()
        {
            DataID = "skinmodhelperChange";
        }

        public DataPlayerInfo ChangePlayer;
        public string SkinID;

        public override MetaType[] GenerateMeta(DataContext ctx)
        {
            return new MetaType[]
            {
                new MetaPlayerPrivateState(ChangePlayer),
                new MetaBoundRef(DataType<DataPlayerInfo>.DataID, ChangePlayer?.ID ?? uint.MaxValue, true)
            };
        }

        public override void FixupMeta(DataContext ctx)
        {
            ChangePlayer = Get<MetaPlayerPrivateState>(ctx).Player;
            Get<MetaBoundRef>(ctx).ID = ChangePlayer?.ID ?? uint.MaxValue;
        }

        protected override void Read(CelesteNetBinaryReader reader)
        {
            SkinID = reader.ReadNetString();
        }

        protected override void Write(CelesteNetBinaryWriter writer)
        {
            writer.WriteNetString(SkinID);
        }
    }
}