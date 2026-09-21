#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace CardBattle.Core.Editor
{
    public static class CurseMutationExampleSetup
    {
        private const string Folder = "Assets/ScriptsData/CardUpgrades/";
        [MenuItem("Card Battle/B2.R2/Create All Strike Mutation Proof")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var card=AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptsData/Cards/Card_AllStrike.asset");
            var catalog=AssetDatabase.LoadAssetAtPath<CardUpgradeCatalog>(Folder+"CardUpgradeCatalog.asset");
            if(card==null || catalog==null) throw new Exception("Existing card/catalog missing");
            var damage=Load<DealDamageEffectData>("AllStrikeUpgradeDamage8.asset",x=>Set(x,"damage",8));
            var mutation=Load<BonusUpgradeDefinition>("AllStrikeQuickenedForm.asset",x=>{
                Set(x,"bonusId","all_strike_quickened_form");Set(x,"displayName","Quickened Form");
                Set(x,"description","All Strike costs 1 AP.");Set(x,"effects",new CardEffectData[0]);
                Set(x,"overrideApCost",true);Set(x,"apCostOverride",1);
            });
            var definition=Load<CardUpgradeDefinition>("AllStrikeLevel1.asset",x=>{
                Set(x,"baseCard",card);Set(x,"guaranteedEffects",new CardEffectData[]{damage});
                Set(x,"bonusPool",new BonusUpgradeDefinition[]{mutation});
            });
            var definitions=Get<List<CardUpgradeDefinition>>(catalog,"upgrades");
            var registry=Get<List<BonusUpgradeDefinition>>(catalog,"bonuses");
            if(!definitions.Contains(definition))definitions.Add(definition);
            if(!registry.Contains(mutation))registry.Add(mutation);
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("[B2.R2] All Strike proof: AP2 / Damage8 Guaranteed; optional AP1 Quickened Form. Strike content preserved.");
        }
        private static T Load<T>(string name,Action<T> configure) where T:ScriptableObject
        {
            var path=Folder+name;var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value!=null)return value;
            if(System.IO.File.Exists(path))throw new Exception("Unexpected asset at "+path);
            value=ScriptableObject.CreateInstance<T>();configure(value);AssetDatabase.CreateAsset(value,path);AssetDatabase.SaveAssetIfDirty(value);return value;
        }
        private static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        private static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    }
}
#endif
