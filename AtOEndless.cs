using HarmonyLib;
using System;
using static AtOEndless.Plugin;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using UnityEngine.UIElements.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.IO;


/*
    AtOManager.Instance.SwapCharacter

    CharacterWindowUI DrawLevelButtons

*/

namespace AtOEndless
{
    [Serializable]
    public class AtOEndlessSaveData {
        private string test;

        public void FillData() {
            LogInfo($"SET {AtOEndless.testData}");
            test = AtOEndless.testData;
        }

        public void LoadData() {
            AtOEndless.testData = test;
            LogInfo($"GET {test}");
        }
    }

    [HarmonyPatch]
    public class AtOEndless {
        public static string testData;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Node), nameof(Node.OnMouseUp))]
        public static void OnMouseUp(ref Node __instance) {
            if(!Functions.ClickedThisTransform(__instance.transform) || AlertManager.Instance.IsActive() || GameManager.Instance.IsTutorialActive() || SettingsManager.Instance.IsActive() || DamageMeterManager.Instance.IsActive() || (bool) (UnityEngine.Object) MapManager.Instance && MapManager.Instance.IsCharacterUnlock() || (bool) (UnityEngine.Object) MapManager.Instance && (MapManager.Instance.IsCorruptionOver() || MapManager.Instance.IsConflictOver()) || (bool) (UnityEngine.Object) MapManager.Instance && MapManager.Instance.selectedNode || (bool) (UnityEngine.Object) EventManager.Instance)
                return;
            if(SteamManager.Instance.steamId.ToString() == "76561197965495526") {
                GameManager.Instance.SetCursorPlain();
                MapManager.Instance.HidePopup();
                MapManager.Instance.PlayerSelectedNode(__instance);
                GameManager.Instance.PlayAudio(AudioManager.Instance.soundButtonClick);
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveGame))]
        public static void SaveGame(int slot, bool backUp) {
            string saveGameName = Traverse.Create(typeof(SaveManager)).Field("saveGameName").GetValue<string>();
            string saveGameExtensionBK = Traverse.Create(typeof(SaveManager)).Field("saveGameExtensionBK").GetValue<string>();
            string saveGameExtension = Traverse.Create(typeof(SaveManager)).Field("saveGameExtension").GetValue<string>();
            byte[] key = Traverse.Create(typeof(SaveManager)).Field("key").GetValue<byte[]>();
            byte[] iv = Traverse.Create(typeof(SaveManager)).Field("iv").GetValue<byte[]>();
            
            StringBuilder stringBuilder1 = new StringBuilder();
            stringBuilder1.Append(Application.persistentDataPath);
            stringBuilder1.Append("/");
            stringBuilder1.Append((ulong) SteamManager.Instance.steamId);
            stringBuilder1.Append("/");
            stringBuilder1.Append(GameManager.Instance.ProfileFolder);
            stringBuilder1.Append("endless_");
            stringBuilder1.Append(slot);
            StringBuilder stringBuilder2 = new StringBuilder();
            stringBuilder2.Append(stringBuilder1.ToString());
            stringBuilder2.Append(saveGameExtensionBK);
            stringBuilder1.Append(saveGameExtension);
            string str = stringBuilder1.ToString();
            string destFileName = stringBuilder2.ToString();
            if(backUp && File.Exists(str))
                File.Copy(str, destFileName, true);
            DESCryptoServiceProvider cryptoServiceProvider = new DESCryptoServiceProvider();
            try {
                FileStream fileStream = new FileStream(str, FileMode.Create, FileAccess.Write);
                using (CryptoStream cryptoStream = new CryptoStream(fileStream, cryptoServiceProvider.CreateEncryptor(key, iv), CryptoStreamMode.Write))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    AtOEndlessSaveData endlessData = new AtOEndlessSaveData();
                    endlessData.FillData();
                    CryptoStream serializationStream = cryptoStream;
                    binaryFormatter.Serialize(serializationStream, endlessData);
                    cryptoStream.Close();
                }
                fileStream.Close();
            } catch {
                LogInfo($"Failed to save AtOEndless Data");
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadGame))]
        public static void LoadGame(int slot, bool comingFromReloadCombat) {
            string saveGameExtension = Traverse.Create(typeof(SaveManager)).Field("saveGameExtension").GetValue<string>();
            byte[] key = Traverse.Create(typeof(SaveManager)).Field("key").GetValue<byte[]>();
            byte[] iv = Traverse.Create(typeof(SaveManager)).Field("iv").GetValue<byte[]>();

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Application.persistentDataPath);
            stringBuilder.Append("/");
            stringBuilder.Append((ulong) SteamManager.Instance.steamId);
            stringBuilder.Append("/");
            stringBuilder.Append(GameManager.Instance.ProfileFolder);
            stringBuilder.Append("endless_");
            stringBuilder.Append(slot);
            stringBuilder.Append(saveGameExtension);
            string path = stringBuilder.ToString();
            if (!File.Exists(path)) {
                LogInfo("ERROR File does not exists");
            } else {
                FileStream fileStream = new FileStream(path, FileMode.Open);
                if(fileStream.Length == 0L) {
                    fileStream.Close();
                } else {
                    DESCryptoServiceProvider cryptoServiceProvider = new DESCryptoServiceProvider();
                    try {
                        CryptoStream serializationStream = new CryptoStream(fileStream, cryptoServiceProvider.CreateDecryptor(key, iv), CryptoStreamMode.Read);
                        (new BinaryFormatter().Deserialize(serializationStream) as AtOEndlessSaveData).LoadData();
                        serializationStream.Close();
                    }
                    catch (SerializationException ex) {
                        LogInfo("Failed to deserialize LoadGame. Reason: " + ex.Message);
                    }
                    fileStream.Close();
                }
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.DeleteGame))]
        public static void DeleteGame(int slot, bool sendTelemetry) {
            string saveGameExtension = Traverse.Create(typeof(SaveManager)).Field("saveGameExtension").GetValue<string>();
            string saveGameExtensionBK = Traverse.Create(typeof(SaveManager)).Field("saveGameExtensionBK").GetValue<string>();

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Application.persistentDataPath);
            stringBuilder.Append("/");
            stringBuilder.Append((ulong) SteamManager.Instance.steamId);
            stringBuilder.Append("/");
            stringBuilder.Append(GameManager.Instance.ProfileFolder);
            stringBuilder.Append("endless_");
            stringBuilder.Append(slot);
            string path1 = stringBuilder.ToString() + saveGameExtension;
            string path2 = stringBuilder.ToString() + saveGameExtensionBK;
            if(File.Exists(path1))
                File.Delete(path1);
            if(File.Exists(path2))
                File.Delete(path2);
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), nameof(Character.LevelUp))]
        public static void LevelUp(Character __instance, HeroData ___heroData) {
            if(___heroData.HeroSubClass.MaxHp.Length < 9) {
                int[] maxHp = new int[9];
                ___heroData.HeroSubClass.MaxHp.CopyTo(maxHp, 4);
                ___heroData.HeroSubClass.MaxHp.CopyTo(maxHp, 0);
                ___heroData.HeroSubClass.MaxHp = maxHp;
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SubClassData), nameof(SubClassData.GetTraitLevel))]
        public static void GetTraitLevel(SubClassData __instance, string traitName, ref int __result) {
            Hero[] team = AtOManager.Instance.GetTeam();
            Hero hero = team.Where(hero => hero.SubclassName.ToLower() == __instance.SubClassName.ToLower()).First();
            if(hero.Traits.Length < 9) {
                string[] traits = new string[9];
                hero.Traits.CopyTo(traits, 0);
                hero.Traits = traits;
            }
            if(hero.Traits[__result] != traitName)
                __result += 4;
        }
        
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(CharacterWindowUI), "GetTraitData")]
        public static TraitData GetTraitData(CharacterWindowUI __instance, int level, int index) {
            return new TraitData();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterWindowUI), "DrawLevelButtons")]
        public static void DrawLevelButtons(ref CharacterWindowUI __instance, int heroLevel, bool levelUp, ref Hero ___currentHero, ref SubClassData ___currentSCD) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;

            string _color = Globals.Instance.ClassColor[___currentHero.ClassName];
            int characterTier = PlayerManager.Instance.GetCharacterTier("", "trait", ___currentHero.PerkRank);
            for (int level = 1; level < 5; ++level) {
                bool _state1 = false;
                bool _state2 = false;
                int index1 = level * 2;
                int index2 = index1 + 1;
                TraitData traitData2 = GetTraitData(__instance, level, 0);
                if (___currentHero.HaveTrait(traitData2.Id))
                    _state2 = true;
                else if ((level == heroLevel || level+4 == heroLevel) & levelUp && (___currentHero.Owner == null || ___currentHero.Owner == "" || ___currentHero.Owner == NetworkManager.Instance.GetPlayerNick()))
                    if (!___currentHero.HaveTrait(traitData2.Id))
                        _state1 = true;
                __instance.traitLevel[index1].SetHeroIndex(__instance.heroIndex);
                __instance.traitLevel[index1].SetColor(_color);
                __instance.traitLevel[index1].SetPosition(1);
                __instance.traitLevel[index1].SetEnable(_state2);
                __instance.traitLevel[index1].SetActive(_state1);
                __instance.traitLevel[index1].SetTrait(traitData2, characterTier);
                TraitData traitData3 = GetTraitData(__instance, level, 1);
                bool _state3 = false;
                bool _state4 = false;
                if(___currentHero.HaveTrait(traitData3.Id))
                    _state3 = true;
                else if ((level == heroLevel || level+4 == heroLevel) & levelUp && (___currentHero.Owner == null || ___currentHero.Owner == "" || ___currentHero.Owner == NetworkManager.Instance.GetPlayerNick()))
                    if (!___currentHero.HaveTrait(traitData3.Id))
                        _state4 = true;
                __instance.traitLevel[index2].SetHeroIndex(__instance.heroIndex);
                __instance.traitLevel[index2].SetColor(_color);
                __instance.traitLevel[index2].SetPosition(2);
                __instance.traitLevel[index2].SetEnable(_state3);
                __instance.traitLevel[index2].SetActive(_state4);
                __instance.traitLevel[index2].SetTrait(traitData3, characterTier);
                StringBuilder stringBuilder2 = new StringBuilder();
                bool flag = false;
                if ((level < heroLevel || (level == heroLevel || level+4 == heroLevel) & levelUp) && (___currentHero.Owner == null || ___currentHero.Owner == "" || ___currentHero.Owner == NetworkManager.Instance.GetPlayerNick()))
                    flag = true;
                stringBuilder2.Append("<size=+.4>");
                if (flag)
                    stringBuilder2.Append("<color=#FC0>");
                stringBuilder2.Append(Texts.Instance.GetText("levelNumber").Replace("<N>", $"{(level + 1)}&{(level + 5)}"));
                if (flag)
                    stringBuilder2.Append("</color>");
                stringBuilder2.Append("</size>");
                stringBuilder2.Append("\n");
                if (flag)
                    stringBuilder2.Append("<color=#EE5A3C>");
                stringBuilder2.Append(Texts.Instance.GetText("incrementMaxHp").Replace("<N>", ___currentSCD.MaxHp[level].ToString()));
                if (flag)
                    stringBuilder2.Append("</color>");
                 __instance.traitLevelText[level].text = stringBuilder2.ToString();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Node), nameof(Node.AssignNode))]
        public static void AssignNodePre(ref AtOManager __instance, out string[][] __state) {
            __state = [[..AtOManager.Instance.mapVisitedNodes], [..AtOManager.Instance.mapVisitedNodesAction]];
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            AtOManager.Instance.mapVisitedNodes.Clear();
            AtOManager.Instance.mapVisitedNodesAction.Clear();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Node), nameof(Node.AssignNode))]
        public static void AssignNodePost(ref Node __instance, string[][] __state) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            AtOManager.Instance.mapVisitedNodes.AddRange([..__state[0]]);
            AtOManager.Instance.mapVisitedNodesAction.AddRange([..__state[1]]);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), nameof(AtOManager.GetTownTier))]
        public static void GetTownTier(ref AtOManager __instance, ref int ___townTier, ref int __result) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            __result = Math.Min(___townTier, 3);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), nameof(AtOManager.GetActNumberForText))]
        public static void GetActNumberForText(ref AtOManager __instance, ref int ___townTier, ref int __result) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            __result = ___townTier + 1;
        }

        public static bool refreshed = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapManager), "Awake")]
        public static void Awake(ref MapManager __instance) {
            if(!refreshed) {
                foreach (GameObject mapGO in __instance.mapList) {
                    foreach(Transform transform in mapGO.transform) {
                        if(transform.gameObject.name == "Nodes") {
                            foreach(Transform transform2 in transform) {
                                GameObject nodeGO = transform2.gameObject;
                                Node node = nodeGO.GetComponent<Node>();
                                node.GetComponent<Node>().nodeData = Globals.Instance.GetNodeData(node.nodeData.NodeId);
                            }
                        }
                    }
                }
                refreshed = true;
            }
        }

        public static string[] GetEnabledPerks() {
            List<string> acquiredPerks = [];
            foreach(List<string> sub in AtOManager.Instance.heroPerks.Values) {
                foreach(string perk in sub) {
                    if(perk.StartsWith("endless_") && !acquiredPerks.Contains(perk))
                        acquiredPerks.Add(perk);
                }
            }
            return [..acquiredPerks];
        }

        public static string GetRandomPerkDescription(PerkData perkData) {
            StringBuilder stringBuilder = new();
            if (perkData.MaxHealth != 0)
                stringBuilder.Append($"<sprite name=heart><space=.5>Health {perkData.MaxHealth:+#;-#;0}<space=1.5>");
            if (perkData.SpeedQuantity != 0)
                stringBuilder.Append($"<sprite name=speedMini><space=.5>Speed {perkData.SpeedQuantity:+#;-#;0}<space=1.5>");
            if (perkData.AuracurseBonus != null && perkData.AuracurseBonusValue != 0)
                stringBuilder.Append($"<sprite name={perkData.AuracurseBonus.Sprite.name}><space=.5>charges {perkData.AuracurseBonusValue:+#;-#;0}<space=1.5>");
            if(perkData.ResistModifiedValue != 0) {
                if(perkData.ResistModified == Enums.DamageType.All)
                    stringBuilder.Append($"<sprite name=ui_resistance><space=.5>All resistances {perkData.ResistModifiedValue:+#;-#;0}%<space=1.5>");
                if(perkData.ResistModified != Enums.DamageType.All) {
                    string sprite = perkData.ResistModified.ToString().ToLower();
                    if(sprite == "slashing")
                        sprite = "slash";
                    stringBuilder.Append($"<sprite name=resist_{sprite}><space=.5>resistance {perkData.ResistModifiedValue:+#;-#;0}%<space=1.5>");
                }
            }
            if(perkData.DamageFlatBonusValue != 0) {
                if(perkData.DamageFlatBonus == Enums.DamageType.All)
                    stringBuilder.Append($"<sprite name=damage><space=.5>All damage {perkData.DamageFlatBonusValue:+#;-#;0}<space=1.5>");
                if(perkData.DamageFlatBonus != Enums.DamageType.All) {
                    string sprite = perkData.DamageFlatBonus.ToString().ToLower();
                    if(sprite == "slashing")
                        sprite = "slash";
                    stringBuilder.Append($"<sprite name={sprite}><space=.5>damage {perkData.DamageFlatBonusValue:+#;-#;0}<space=1.5>");
                }
            }
            stringBuilder.Replace("<c>", "");
            stringBuilder.Replace("</c>", "");
            return $"<space=1>{stringBuilder}";
        }

        public static string GetRandomPerkType(string[] exclude)
        {
            string[] types = [..(new string[] { "h", "a", "d", "r", "s" }).Where(v => !exclude.Contains(v))];
            return types[MapManager.Instance.GetRandomIntRange(0, types.Length)];
        }

        public static string GetRandomPerkSubtype(string type) {
            if(type == "a") {
                string[] types = ["bleed", "block", "burn", "chill", "dark", "fury", "insane", "poison", "regeneration", "sharp", "shield", "sight", "spark", "thorns", "vitality", "wet"];
                return types[MapManager.Instance.GetRandomIntRange(0, types.Length)];
            }
            if(type == "d" || type == "r") {
                Enums.DamageType[] types = [..Enum.GetValues(typeof(Enums.DamageType)).Cast<Enums.DamageType>().Where(d => !d.Equals(Enums.DamageType.None))];
                return types[MapManager.Instance.GetRandomIntRange(0, types.Length)].ToString().ToLower();
            }
            return "";
        }

        public static int GetRandomPerkValue(string type, string subtype = "")
        {
            int high = 0;
            int low = 0;
            if(type == "h") {
                low = 5;
                high = 15;
            }
            if(type == "a") {
                low = 1;
                high = 2;
            }
            if(type == "s") {
                low = 1;
                high = 2;
            }
            if(type == "d") {
                low = 1;
                high = 3;
            }
            if(type == "r") {
                low = 5;
                high = 10;
            }
            return MapManager.Instance.GetRandomIntRange(low, high);
        }

        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EventManager), nameof(EventManager.SetEvent))]
        public static void SetEvent(ref EventManager __instance, EventData _eventData)
        {
            if(_eventData.EventId == "e_endless_perk") {
                EventReplyData perkReplyPrefab = Globals.Instance.GetEventData("e_challenge_next").Replys.First<EventReplyData>();
                
                List<EventReplyData> replies = [];
                for(int i = 0; i < 5; i++) {
                    EventReplyData perkReplyData = perkReplyPrefab.ShallowCopy();
                    StringBuilder sb = new();
                    sb.Append("endless_");
                    List<string> exclude = [];
                    for(int t = 0; t < 2; t++) {
                        string type = GetRandomPerkType([..exclude]);
                        exclude.Add(type);
                        string subtype = GetRandomPerkSubtype(type);
                        int value = GetRandomPerkValue(type, subtype);
                        sb.Append($"{type}:");
                        if(subtype != "")
                            sb.Append($"{subtype}#{type}v:");
                        sb.Append($"{value}#");
                    }
                    sb.Length--;
                    sb.Append($"_{Functions.RandomString(6f)}");
                    perkReplyData.SsPerkData = Globals.Instance.GetPerkData(sb.ToString());
                    perkReplyData.SsPerkData1 = null;
                    //LogInfo($"{perkReplyData.SsPerkData.Id}");
                    perkReplyData.ReplyText = GetRandomPerkDescription(perkReplyData.SsPerkData);
                    perkReplyData.SsRewardText = "";
                    //LogInfo($"{perkReplyData.ReplyText}");
                    perkReplyData.SsRequirementUnlock = null;
                    perkReplyData.SsDustReward = 0;
                    perkReplyData.SsExperienceReward = 0;
                    perkReplyData.SsGoldReward = 0;
                    perkReplyData.SsFinishObeliskMap = false;
                    perkReplyData.SsEvent = Globals.Instance.GetEventData("e_endless_obelisk");
                    replies.Add(perkReplyData);
                }
                Globals.Instance.GetEventData("e_endless_perk").Replys = [..replies];
            }

            if(_eventData.EventId == "e_endless_obelisk") {
                EventReplyData obeliskReplyPrefab = Globals.Instance.GetEventData("e_sen34_a").Replys.First();
                Enums.Zone zone = AtOManager.Instance.GetMapZone(AtOManager.Instance.currentMapNode);

                bool canUlmin = SteamManager.Instance.PlayerHaveDLC("2511580") || (GameManager.Instance.IsMultiplayer() && NetworkManager.Instance.AnyPlayersHaveSku("2511580"));
                bool canSahti = SteamManager.Instance.PlayerHaveDLC("3185630") || (GameManager.Instance.IsMultiplayer() && NetworkManager.Instance.AnyPlayersHaveSku("3185630"));

                List<NodeData> possibleNodes = [];
                possibleNodes.Add(Globals.Instance.GetNodeData("sen_0"));
                possibleNodes.Add(Globals.Instance.GetNodeData("faen_0"));
                possibleNodes.Add(Globals.Instance.GetNodeData("aqua_0"));
                possibleNodes.Add(Globals.Instance.GetNodeData("velka_0"));
                if(canUlmin)
                    possibleNodes.Add(Globals.Instance.GetNodeData("ulmin_0"));
                if(canSahti)
                    possibleNodes.Add(Globals.Instance.GetNodeData("sahti_0"));
                possibleNodes.Add(Globals.Instance.GetNodeData("voidlow_0"));

                bool noUnlock = false;
                
                if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_unique_zones"))) {
                    if((AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_sen")) || zone == Enums.Zone.Senenthia) &&
                       (AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_faen")) || zone == Enums.Zone.Faeborg) &&
                       (AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_aqua")) || zone == Enums.Zone.Aquarfall) &&
                       (AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_velka")) || zone == Enums.Zone.Velkarath) &&
                       (AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_ulmin")) || zone == Enums.Zone.Ulminin) &&
                       (AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_sahti")) || zone == Enums.Zone.Sahti) &&
                       (AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_void")) || zone == Enums.Zone.VoidHigh)) {
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_sen"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_faen"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_aqua"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_velka"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_ulmin"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_sahti"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_void"));
                        noUnlock = true;
                    }
                }

                if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_require_all_before_void"))) {
                    if(zone == Enums.Zone.VoidHigh) {
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_sen"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_faen"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_aqua"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_velka"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_ulmin"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_sahti"));
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData("endless_complete_void"));
                        noUnlock = true;
                    }
                }

                if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_unique_zones")) ||
                  !AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_allow_repeats"))) {
                    if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_sen")) || zone == Enums.Zone.Senenthia)
                        possibleNodes.Remove(Globals.Instance.GetNodeData("sen_0"));
                    if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_faen")) || zone == Enums.Zone.Faeborg)
                        possibleNodes.Remove(Globals.Instance.GetNodeData("faen_0"));
                    if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_aqua")) || zone == Enums.Zone.Aquarfall)
                        possibleNodes.Remove(Globals.Instance.GetNodeData("aqua_0"));
                    if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_velka")) || zone == Enums.Zone.Velkarath)
                        possibleNodes.Remove(Globals.Instance.GetNodeData("velka_0"));
                    if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_ulmin")) || zone == Enums.Zone.Ulminin)
                        possibleNodes.Remove(Globals.Instance.GetNodeData("ulmin_0"));
                    if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_sahti")) || zone == Enums.Zone.Sahti)
                        possibleNodes.Remove(Globals.Instance.GetNodeData("sahti_0"));
                    if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_void")) || zone == Enums.Zone.VoidHigh)
                        possibleNodes.Remove(Globals.Instance.GetNodeData("voidlow_0"));
                }
                
                if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_require_all_before_void"))) {
                    if((!AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_sen")) && zone != Enums.Zone.Senenthia) ||
                       (!AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_faen")) && zone != Enums.Zone.Faeborg) ||
                       (!AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_aqua")) && zone != Enums.Zone.Aquarfall) ||
                       (!AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_velka")) && zone != Enums.Zone.Velkarath) ||
                       (canUlmin && !AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_ulmin")) && zone != Enums.Zone.Ulminin) ||
                       (canSahti && !AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_complete_sahti")) && zone != Enums.Zone.Sahti)) {
                            possibleNodes.Remove(Globals.Instance.GetNodeData("voidlow_0"));
                    }
                }

                List<EventReplyData> replies = [];
                int zoneCount = -1;

                if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_zonecount_1"))) {
                    zoneCount = 1;
                }
                if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_zonecount_2"))) {
                    zoneCount = 2;
                }
                if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("endless_zonecount_3"))) {
                    zoneCount = 3;
                }

                int nodeCount = zoneCount == -1 ? possibleNodes.Count : Math.Min(possibleNodes.Count, zoneCount);
                for(int i = 0; i < nodeCount; i++) {
                    NodeData selectedNode = possibleNodes[MapManager.Instance.GetRandomIntRange(0, possibleNodes.Count)];

                    EventReplyData obeliskReplyData = obeliskReplyPrefab.ShallowCopy();

                    obeliskReplyData.ReplyActionText = Enums.EventAction.None;
                    obeliskReplyData.SsRequirementUnlock = null;
                    if(!noUnlock) {
                        if(zone == Enums.Zone.Senenthia) {
                            obeliskReplyData.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_complete_sen");
                        } else if(zone == Enums.Zone.Faeborg) {
                            obeliskReplyData.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_complete_faen");
                        } else if(zone == Enums.Zone.Aquarfall) {
                            obeliskReplyData.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_complete_aqua");
                        } else if(zone == Enums.Zone.Velkarath) {
                            obeliskReplyData.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_complete_velka");
                        } else if(zone == Enums.Zone.Ulminin) {
                            obeliskReplyData.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_complete_ulmin");
                        } else if(zone == Enums.Zone.Sahti) {
                            obeliskReplyData.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_complete_sahti");
                        } else if(zone == Enums.Zone.VoidHigh) {
                            obeliskReplyData.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_complete_void");
                        }
                    }

                    obeliskReplyData.ReplyText = zoneCount == 1 ? "You are thrust into a portal of unknown color" : GetPortalString(AtOManager.Instance.GetMapZone(selectedNode.NodeId));
                    obeliskReplyData.SsRewardText = "";
                    
                    obeliskReplyData.SsNodeTravel = selectedNode;
                    replies.Add(obeliskReplyData);

                    possibleNodes.Remove(selectedNode);
                }

                Globals.Instance.GetEventData("e_endless_obelisk").Replys = [..replies];
            }
        }

        public static Dictionary<Enums.Zone, string> portalStrings = new()
        {
            { Enums.Zone.Senenthia, "grassy" },
            { Enums.Zone.Faeborg, "icy" },
            { Enums.Zone.Aquarfall, "swampy" },
            { Enums.Zone.Velkarath, "molten" },
            { Enums.Zone.Ulminin, "sandy" },
            { Enums.Zone.Sahti, "salty" },
            { Enums.Zone.VoidLow, "cosmic" }
        };

        public static string GetPortalString(Enums.Zone zone)
        {
            return $"Step into the {portalStrings.Get(zone)} portal.";
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(EventManager), "FinalResolution")]
        public static void FinalResolution(EventManager __instance, EventData ___currentEvent, EventReplyData ___replySelected) {
            if(___currentEvent.EventId == "e_endless_perk") {
                if(___replySelected != null) {
                    __instance.result.text = GetRandomPerkDescription(___replySelected.SsPerkData);
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(Globals), nameof(Globals.GetPerkData))]
        public static void GetPerkData(Globals __instance, ref PerkData __result, ref Dictionary<string, PerkData> ____PerksSource, string id) {
            if(__result == null) {
                if(id.StartsWith("endless_")) {
                    __result = ScriptableObject.CreateInstance<PerkData>();
                    __result.Icon = Globals.Instance.GetAuraCurseData("burn").Sprite;
                    __result.Id = id.ToLower();
                    string[] idparts = id.Split("_");
                    string data = idparts[1];
                    foreach(string part in data.Split("#")) {
                        string[] parts = part.Split(":");
                        string type = parts[0];
                        string value = parts[1];
                        switch (type) {
                            case "h":
                                __result.MaxHealth = int.Parse(value);
                                break;
                            case "s":
                                __result.SpeedQuantity = int.Parse(value);
                                break;
                            case "a":
                                __result.AuracurseBonus = Globals.Instance.GetAuraCurseData(value);
                                break;
                            case "av":
                                __result.AuracurseBonusValue = int.Parse(value);
                                break;
                            case "d":
                                __result.DamageFlatBonus = Enum.Parse<Enums.DamageType>($"{char.ToUpper(value[0])}{value[1..].ToLower()}");
                                break;
                            case "dv":
                                __result.DamageFlatBonusValue = int.Parse(value);
                                break;
                            case "r":
                                __result.ResistModified = Enum.Parse<Enums.DamageType>($"{char.ToUpper(value[0])}{value[1..].ToLower()}");
                                break;
                            case "rv":
                                __result.ResistModifiedValue = int.Parse(value);
                                break;
                            case "e":
                                __result.EnergyBegin = int.Parse(value);
                                break;
                            default:
                                break;
                        }
                    }
                    __result.Init();
                    ____PerksSource.Add(__result.Id, __result);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), nameof(AtOManager.AddPerkToHero))]
        public static void AddPerkToHeroPost(AtOManager __instance, ref Hero[] ___teamAtO, int _heroIndex, string _perkId, bool _initHealth) {
            PerkData perkData = Globals.Instance.GetPerkData(_perkId);
            if(!(perkData != null))
                return;
            string subclassName = ___teamAtO[_heroIndex].SubclassName;
        }

        public static void AddNewRequirement(string id, ref Dictionary<string, EventRequirementData> ____Requirements) {
            if(____Requirements.TryGetValue("_tier2", out EventRequirementData requirementPrefab)) {
                EventRequirementData endlessRequirement = UnityEngine.Object.Instantiate(requirementPrefab);
                endlessRequirement.RequirementId = endlessRequirement.name = id;
                ____Requirements.Add(endlessRequirement.RequirementId.ToLower(), endlessRequirement);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Globals), nameof(Globals.CreateGameContent))]
        public static void CreateGameContent(ref Globals __instance,
        ref Dictionary<string, EventData> ____Events,
        ref Dictionary<string, NodeData> ____NodeDataSource,
        ref Dictionary<string, CombatData> ____CombatDataSource,
        ref Dictionary<string, CinematicData> ____Cinematics,
        ref Dictionary<string, EventRequirementData> ____Requirements,
        ref Dictionary<int, int> ____ExperienceByLevel
        ) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;

            ____ExperienceByLevel.Add(5, 1750);
            ____ExperienceByLevel.Add(6, 2000);
            ____ExperienceByLevel.Add(7, 2500);
            ____ExperienceByLevel.Add(8, 3000);

            AddNewRequirement("endless_complete_sen", ref ____Requirements);
            AddNewRequirement("endless_complete_faen", ref ____Requirements);
            AddNewRequirement("endless_complete_aqua", ref ____Requirements);
            AddNewRequirement("endless_complete_ulmin", ref ____Requirements);
            AddNewRequirement("endless_complete_velka", ref ____Requirements);
            AddNewRequirement("endless_complete_sahti", ref ____Requirements);
            AddNewRequirement("endless_complete_void", ref ____Requirements);

            AddNewRequirement("endless_unique_zones", ref ____Requirements);
            AddNewRequirement("endless_allow_repeats", ref ____Requirements);
            AddNewRequirement("endless_require_all_before_void", ref ____Requirements);
            
            AddNewRequirement("endless_allow_perks", ref ____Requirements);

            AddNewRequirement("endless_zonecount_1", ref ____Requirements);
            AddNewRequirement("endless_zonecount_2", ref ____Requirements);
            AddNewRequirement("endless_zonecount_3", ref ____Requirements);


            if(____Events.TryGetValue("e_sen44_a", out EventData eventConfigPrefab)) {
                EventData eventDataAllowPerks = UnityEngine.Object.Instantiate(eventConfigPrefab);
                EventData eventDataAllowRepeats = UnityEngine.Object.Instantiate(eventConfigPrefab);
                EventData eventDataRequireAll = UnityEngine.Object.Instantiate(eventConfigPrefab);
                EventData eventDataUniqueZones = UnityEngine.Object.Instantiate(eventConfigPrefab);
                EventData eventDataZoneCount = UnityEngine.Object.Instantiate(eventConfigPrefab);

                EventReplyData configReplyPrefab = Globals.Instance.GetEventData("e_sen44_a").Replys.First();


                EventReplyData configReplyAllowPerksYes = configReplyPrefab.ShallowCopy();
                EventReplyData configReplyAllowPerksNo = configReplyPrefab.ShallowCopy();

                configReplyAllowPerksYes.ReplyActionText = Enums.EventAction.None;
                configReplyAllowPerksYes.ReplyText = "Yes";
                configReplyAllowPerksYes.SsRewardText = "";
                configReplyAllowPerksYes.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_allow_perks");
                configReplyAllowPerksYes.SsEvent = eventDataRequireAll;

                configReplyAllowPerksNo.ReplyActionText = Enums.EventAction.None;
                configReplyAllowPerksNo.ReplyText = "No";
                configReplyAllowPerksNo.SsRewardText = "";
                configReplyAllowPerksNo.SsRequirementUnlock = null;
                configReplyAllowPerksNo.SsEvent = eventDataRequireAll;


                EventReplyData configReplyRequireAllYes = configReplyPrefab.ShallowCopy();
                EventReplyData configReplyRequireAllNo = configReplyPrefab.ShallowCopy();

                configReplyRequireAllYes.ReplyActionText = Enums.EventAction.None;
                configReplyRequireAllYes.ReplyText = "Yes";
                configReplyRequireAllYes.SsRewardText = "";
                configReplyRequireAllYes.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_require_all_before_void");
                configReplyRequireAllYes.SsEvent = eventDataUniqueZones;

                configReplyRequireAllNo.ReplyActionText = Enums.EventAction.None;
                configReplyRequireAllNo.ReplyText = "No";
                configReplyRequireAllNo.SsRewardText = "";
                configReplyRequireAllNo.SsRequirementUnlock = null;
                configReplyRequireAllNo.SsEvent = eventDataUniqueZones;


                EventReplyData configReplyUniqueZonesYes = configReplyPrefab.ShallowCopy();
                EventReplyData configReplyUniqueZonesNo = configReplyPrefab.ShallowCopy();

                configReplyUniqueZonesYes.ReplyActionText = Enums.EventAction.None;
                configReplyUniqueZonesYes.ReplyText = "Yes";
                configReplyUniqueZonesYes.SsRewardText = "";
                configReplyUniqueZonesYes.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_unique_zones");
                configReplyUniqueZonesYes.SsEvent = eventDataZoneCount;

                configReplyUniqueZonesNo.ReplyActionText = Enums.EventAction.None;
                configReplyUniqueZonesNo.ReplyText = "No";
                configReplyUniqueZonesNo.SsRewardText = "";
                configReplyUniqueZonesNo.SsRequirementUnlock = null;
                configReplyUniqueZonesNo.SsEvent = eventDataAllowRepeats;



                EventReplyData configReplyAllowRepeatsYes = configReplyPrefab.ShallowCopy();
                EventReplyData configReplyAllowRepeatsNo = configReplyPrefab.ShallowCopy();

                configReplyAllowRepeatsYes.ReplyActionText = Enums.EventAction.None;
                configReplyAllowRepeatsYes.ReplyText = "Yes";
                configReplyAllowRepeatsYes.SsRewardText = "";
                configReplyAllowRepeatsYes.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_allow_repeats");
                configReplyAllowRepeatsYes.SsEvent = eventDataZoneCount;

                configReplyAllowRepeatsNo.ReplyActionText = Enums.EventAction.None;
                configReplyAllowRepeatsNo.ReplyText = "No";
                configReplyAllowRepeatsNo.SsRewardText = "";
                configReplyAllowRepeatsNo.SsRequirementUnlock = null;
                configReplyAllowRepeatsNo.SsEvent = eventDataZoneCount;


                
                EventReplyData configReplyZoneCount1 = configReplyPrefab.ShallowCopy();
                EventReplyData configReplyZoneCount2 = configReplyPrefab.ShallowCopy();
                EventReplyData configReplyZoneCount3 = configReplyPrefab.ShallowCopy();
                EventReplyData configReplyZoneCountAll = configReplyPrefab.ShallowCopy();

                configReplyZoneCount1.ReplyActionText = Enums.EventAction.None;
                configReplyZoneCount1.ReplyText = "1";
                configReplyZoneCount1.SsRewardText = "";
                configReplyZoneCount1.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_zonecount_1");
                configReplyZoneCount1.SsEvent = null;

                configReplyZoneCount2.ReplyActionText = Enums.EventAction.None;
                configReplyZoneCount2.ReplyText = "2";
                configReplyZoneCount2.SsRewardText = "";
                configReplyZoneCount2.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_zonecount_2");
                configReplyZoneCount2.SsEvent = null;

                configReplyZoneCount3.ReplyActionText = Enums.EventAction.None;
                configReplyZoneCount3.ReplyText = "3";
                configReplyZoneCount3.SsRewardText = "";
                configReplyZoneCount3.SsRequirementUnlock = Globals.Instance.GetRequirementData("endless_zonecount_3");
                configReplyZoneCount3.SsEvent = null;

                configReplyZoneCountAll.ReplyActionText = Enums.EventAction.None;
                configReplyZoneCountAll.ReplyText = "All";
                configReplyZoneCountAll.SsRewardText = "";
                configReplyZoneCountAll.SsRequirementUnlock = null;
                configReplyZoneCountAll.SsEvent = null;



                eventDataAllowPerks.EventName = "Endless Obelisk";
                eventDataAllowPerks.Description = "Allow Perks";
                eventDataAllowPerks.DescriptionAction = "Allow randomized perks at the end of each act?";
                eventDataAllowPerks.EventId = "e_endless_allow_perks";
                eventDataAllowPerks.Replys = [configReplyAllowPerksYes, configReplyAllowPerksNo];
                eventDataAllowPerks.Init();
                ____Events.Add(eventDataAllowPerks.EventId.ToLower(), eventDataAllowPerks);

                eventDataRequireAll.EventName = "Endless Obelisk";
                eventDataRequireAll.Description = "Require All Before Void";
                eventDataRequireAll.DescriptionAction = "Require all other acts to be completed per cycle before void act?";
                eventDataRequireAll.EventId = "e_endless_require_all_before_void";
                eventDataRequireAll.Replys = [configReplyRequireAllYes, configReplyRequireAllNo];
                eventDataRequireAll.Init();
                ____Events.Add(eventDataRequireAll.EventId.ToLower(), eventDataRequireAll);

                eventDataUniqueZones.EventName = "Endless Obelisk";
                eventDataUniqueZones.Description = "Unique Zones";
                eventDataUniqueZones.DescriptionAction = "Only encounter each zone once per cycle?";
                eventDataUniqueZones.EventId = "e_endless_unique_zones";
                eventDataUniqueZones.Replys = [configReplyUniqueZonesYes, configReplyUniqueZonesNo];
                eventDataUniqueZones.Init();
                ____Events.Add(eventDataUniqueZones.EventId.ToLower(), eventDataUniqueZones);

                eventDataAllowRepeats.EventName = "Endless Obelisk";
                eventDataAllowRepeats.Description = "Allow Repeat Zones";
                eventDataAllowRepeats.DescriptionAction = "Allow the same zone to be encountered after itself?";
                eventDataAllowRepeats.EventId = "e_endless_allow_repeats";
                eventDataAllowRepeats.Replys = [configReplyAllowRepeatsYes, configReplyAllowRepeatsNo];
                eventDataAllowRepeats.Init();
                ____Events.Add(eventDataAllowRepeats.EventId.ToLower(), eventDataAllowRepeats);

                eventDataZoneCount.EventName = "Endless Obelisk";
                eventDataZoneCount.Description = "Zones to Pick From";
                eventDataZoneCount.DescriptionAction = "How many random zones to pick from at the obelisk?";
                eventDataZoneCount.EventId = "e_endless_zone_count";
                eventDataZoneCount.Replys = [configReplyZoneCount1, configReplyZoneCount2, configReplyZoneCount3, configReplyZoneCountAll];
                eventDataZoneCount.Init();
                ____Events.Add(eventDataZoneCount.EventId.ToLower(), eventDataZoneCount);
            }

            if(____Events.TryGetValue("e_sen34_a", out EventData eventPrefab)) {
                EventData eventDataEndlessObelisk = UnityEngine.Object.Instantiate<EventData>(eventPrefab);
                eventDataEndlessObelisk.EventName = "Endless Obelisk";
                eventDataEndlessObelisk.Description = "Ahhhhhhhhhhhhhhhhhhhh!";
                eventDataEndlessObelisk.DescriptionAction = "AHHHHHHHHHHHHHHHH!";
                eventDataEndlessObelisk.EventId = "e_endless_obelisk";
                eventDataEndlessObelisk.Replys = [];
                eventDataEndlessObelisk.Init();
                ____Events.Add(eventDataEndlessObelisk.EventId.ToLower(), eventDataEndlessObelisk);
            }

            if(____Events.TryGetValue("e_challenge_next", out EventData perkPrefab)) {
                EventData eventDataEndlessPerk = UnityEngine.Object.Instantiate<EventData>(perkPrefab);
                eventDataEndlessPerk.EventName = "Endless Obelisk";
                eventDataEndlessPerk.Description = "Pick yo perk";
                eventDataEndlessPerk.DescriptionAction = "Choose it yo";
                eventDataEndlessPerk.EventId = "e_endless_perk";
                eventDataEndlessPerk.Requirement = Globals.Instance.GetRequirementData("endless_allow_perks");
                eventDataEndlessPerk.Replys = [];
                eventDataEndlessPerk.ReplyRandom = 0;
                eventDataEndlessPerk.Init();
                ____Events.Add(eventDataEndlessPerk.EventId.ToLower(), eventDataEndlessPerk);
            }
                
            if (____Cinematics.TryGetValue("intro", out CinematicData introData)) {
                introData.CinematicEvent = Globals.Instance.GetEventData("e_endless_allow_perks");
                ____Cinematics["intro"] = introData;
            }
                
            if (____NodeDataSource.TryGetValue("sen_34", out NodeData sen34Data)) {
                sen34Data.NodeEvent = [Globals.Instance.GetEventData("e_endless_perk"), Globals.Instance.GetEventData("e_endless_obelisk")];
                sen34Data.NodeEventPriority = [0, 1];
                ____NodeDataSource["sen_34"] = sen34Data;
            }
            if (____NodeDataSource.TryGetValue("faen_39", out NodeData faen39Data)) {
                faen39Data.NodeEvent = [Globals.Instance.GetEventData("e_endless_perk"), Globals.Instance.GetEventData("e_endless_obelisk")];
                faen39Data.NodeEventPriority = [0, 1];
                ____NodeDataSource["faen_39"] = faen39Data;
            }
            if (____NodeDataSource.TryGetValue("aqua_36", out NodeData aqua36Data)) {
                aqua36Data.NodeEvent = [Globals.Instance.GetEventData("e_endless_perk"), Globals.Instance.GetEventData("e_endless_obelisk")];
                aqua36Data.NodeEventPriority = [0, 1];
                ____NodeDataSource["aqua_36"] = aqua36Data;
            }
            if (____NodeDataSource.TryGetValue("velka_33", out NodeData velka33Data)) {
                velka33Data.NodeEvent = [Globals.Instance.GetEventData("e_endless_perk"), Globals.Instance.GetEventData("e_endless_obelisk")];
                velka33Data.NodeEventPriority = [0, 1];
                ____NodeDataSource["velka_33"] = velka33Data;
            }
            if (____NodeDataSource.TryGetValue("ulmin_40", out NodeData ulmin40Data)) {
                ulmin40Data.NodeEvent = [Globals.Instance.GetEventData("e_endless_perk"), Globals.Instance.GetEventData("e_endless_obelisk")];
                ulmin40Data.NodeEventPriority = [0, 1];
                ____NodeDataSource["ulmin_40"] = ulmin40Data;
            }
            if (____NodeDataSource.TryGetValue("sahti_63", out NodeData sahti63Data)) {
                sahti63Data.NodeEvent = [Globals.Instance.GetEventData("e_endless_perk"), Globals.Instance.GetEventData("e_endless_obelisk")];
                sahti63Data.NodeEventPriority = [0, 1];
                ____NodeDataSource["sahti_63"] = sahti63Data;
            }
            if (____CombatDataSource.TryGetValue("evoidhigh_13b", out CombatData evoidhigh13bData)) {
                evoidhigh13bData.EventData = Globals.Instance.GetEventData("e_endless_obelisk");
                ____CombatDataSource["evoidhigh_13b"] = evoidhigh13bData;
            }
            if (____Cinematics.TryGetValue("endgame", out CinematicData endgameData)) {
                endgameData.CinematicEndAdventure = false;
                ____Cinematics["endgame"] = endgameData;
            }
        }
        public static Dictionary<Enums.Zone, List<string>> RemoveRequirementsByZone = new()
        {
            { Enums.Zone.Senenthia, ["treasurehuntii"] },
            { Enums.Zone.Faeborg, ["darkdeal"] },
            { Enums.Zone.Velkarath, ["armblowtorch", "armchargedrod", "armcoolingengine", "armdisclauncher", "armsmallcannon"]},
            { Enums.Zone.Aquarfall, [] },
            { Enums.Zone.Ulminin, ["ulmininportal", "ulminindown", "riftulmi61"] },
            { Enums.Zone.Sahti, ["sahtidown", "sahtipolizonevent", "sahtiship", "dreadroom", "dreadup", "dreadmastcannon"] },
            { Enums.Zone.VoidLow, [] },
        };

        public static Dictionary<Enums.Zone, List<string>> AddRequirementsByZone = new()
        {
            { Enums.Zone.Senenthia, ["crossroadnorth", "crossroadsouth", "caravannopay", "fungamemill", "riftsen47", "riftsen48"] },
            { Enums.Zone.Faeborg, ["boatfaenlor", "riftfaen42", "riftfaen43"] },
            { Enums.Zone.Velkarath, ["lavacascade", "goblinnorth", "riftvelka39", "riftvelka40"]},
            { Enums.Zone.Aquarfall, ["boatup", "boatcenter", "boatdown", "riftaqua46", "riftaqua47"] },
            { Enums.Zone.Ulminin, ["ulmininup", "riftulmi60", "riftulmi61"] },
            { Enums.Zone.Sahti, ["sahtiup", "dreaddown", "sahtipirateking", "sahtipolizon", "riftsahti67", "riftsahti68"] },
            { Enums.Zone.VoidLow, ["voidnorth", "voidnorthpass", "voidsouth", "voidsouthpass"] },

        };

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EventManager), nameof(EventManager.CloseEvent))]
        public static void CloseEvent(ref EventManager __instance, ref NodeData ___destinationNode, ref EventData ___currentEvent)
        {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            if(___destinationNode != null && ___currentEvent.EventId == "e_endless_obelisk") {
                AtOManager.Instance.SetTownTier(AtOManager.Instance.GetActNumberForText());
                AtOManager.Instance.SetGameId($"{AtOManager.Instance.GetGameId().Split("+", StringSplitOptions.RemoveEmptyEntries).First()}+{AtOManager.Instance.GetActNumberForText()}");
                AtOManager.Instance.gameNodeAssigned.Clear();
                AtOManager.Instance.RemoveItemList(true);

                if(RemoveRequirementsByZone.TryGetValue(AtOManager.Instance.GetMapZone(___destinationNode.NodeId), out List<string> requirementsToRemove)) {
                    foreach(string requirementToRemove in requirementsToRemove) {
                        AtOManager.Instance.RemovePlayerRequirement(Globals.Instance.GetRequirementData(requirementToRemove));
                    }
                }
                if(AddRequirementsByZone.TryGetValue(AtOManager.Instance.GetMapZone(___destinationNode.NodeId), out List<string> requirementsToAdd)) {
                    foreach(string requirementToAdd in requirementsToAdd) {
                        AtOManager.Instance.AddPlayerRequirement(Globals.Instance.GetRequirementData(requirementToAdd));
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), nameof(AtOManager.AddPlayerRequirement))]
        public static void AddPlayerRequirement(ref AtOManager __instance, EventRequirementData requirement, bool share, ref List<string> ___playerRequeriments)
        {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            LogInfo($"Add Requirement: {requirement.RequirementId}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), nameof(AtOManager.RemovePlayerRequirement))]
        public static void RemovePlayerRequirement(ref AtOManager __instance, EventRequirementData requirement, string requirementId, ref List<string> ___playerRequeriments)
        {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            LogInfo($"Remove Requirement: {(requirementId != "" ? requirementId : requirement.RequirementId)}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AtOManager), nameof(AtOManager.UpgradeTownTier))]
        public static bool UpgradeTownTier(ref AtOManager __instance, ref int ___townTier)
        {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return true;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NPC), nameof(NPC.InitData))]
        public static void InitDataPre(ref NPC __instance)
        {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            if (__instance.NpcData != null && AtOManager.Instance.GetActNumberForText() >= 3 && __instance.NpcData.UpgradedMob != null)
                __instance.NpcData = __instance.NpcData.UpgradedMob;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(NPC), nameof(NPC.InitData))]
        public static void InitDataPost(ref NPC __instance)
        {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            int townTier = AtOManager.Instance.GetActNumberForText() - 1;
            if(townTier >= 1 && (AtOManager.Instance.GetMapZone(AtOManager.Instance.currentMapNode) == Enums.Zone.Senenthia || AtOManager.Instance.GetMapZone(AtOManager.Instance.currentMapNode) == Enums.Zone.Sectarium)) {
                __instance.Hp = __instance.HpCurrent = Functions.FuncRoundToInt(__instance.Hp + (__instance.Hp * 3f));
                __instance.Speed += 2;
            }

            if(townTier >= 3 && AtOManager.Instance.GetMapZone(AtOManager.Instance.currentMapNode) != Enums.Zone.VoidLow && AtOManager.Instance.GetMapZone(AtOManager.Instance.currentMapNode) != Enums.Zone.VoidHigh) {
                __instance.Hp = __instance.HpCurrent = Functions.FuncRoundToInt(__instance.Hp + (__instance.Hp * 0.5f));
                __instance.Speed += 1;
            }

            if(townTier >= 4) {
                __instance.Hp = __instance.HpCurrent = Functions.FuncRoundToInt(__instance.Hp + (__instance.Hp * (0.5f * (townTier - 3))));
                __instance.Speed += 1 * (townTier - 3);
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.GetAuraCurseQuantityModification))]
        public static void GetAuraCurseQuantityModification(ref Character __instance, ref int __result, string id, Enums.CardClass CC, ref bool ___isHero)
        {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            if (!___isHero) {
                if (id == "doom" || id == "paralyze" || id == "invulnerable" || id == "stress" || id == "fatigue")
                    __result = 0;

                int townTier = AtOManager.Instance.GetActNumberForText() - 1;
                if(townTier >= 4) {
                    __result = Mathf.FloorToInt(__result * (1 + (.25f * (townTier - 3))));
                }
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.DamageWithCharacterBonus))]
        public static void DamageWithCharacterBonus(ref Character __instance, ref int __result, int value, Enums.DamageType DT, Enums.CardClass CC,
            int energyCost, int additionalDamage, ref bool ___isHero)
        {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            if (!___isHero) {
                int townTier = AtOManager.Instance.GetActNumberForText() - 1;
                if(townTier >= 4) {
                    __result = Functions.FuncRoundToInt(__result * (1 + (.25f * (townTier - 3))));
                }
            }
        }

        

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), nameof(GameData.FillData))]
        public static void FillData(ref GameData __instance, ref int ___townTier) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            ___townTier = AtOManager.Instance.GetActNumberForText() - 1;

            testData = AtOManager.Instance.GetGameId();
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(AtOManager), nameof(AtOManager.SetCurrentNode))]
        public static void SetCurrentNodePre(ref AtOManager __instance, out string[][] __state) {
            __state = [[..AtOManager.Instance.mapVisitedNodes], [..AtOManager.Instance.mapVisitedNodesAction]];
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            AtOManager.Instance.mapVisitedNodes.Clear();
            AtOManager.Instance.mapVisitedNodesAction.Clear();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), nameof(AtOManager.SetCurrentNode))]
        public static void SetCurrentNodePost(ref AtOManager __instance, string[][] __state) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            AtOManager.Instance.mapVisitedNodes.AddRange([..__state[0]]);
            AtOManager.Instance.mapVisitedNodesAction.AddRange([..__state[1]]);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapManager), "DrawNodes")]
        public static void DrawNodesPre(ref MapManager __instance, out string[][] __state) {
            __state = [[..AtOManager.Instance.mapVisitedNodes], [..AtOManager.Instance.mapVisitedNodesAction]];
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            AtOManager.Instance.mapVisitedNodes.Clear();
            AtOManager.Instance.mapVisitedNodesAction.Clear();
        }

        public static string[] zoneStartNodes = ["sen_0", "faen_0", "aqua_0", "velka_0", "ulmin_0", "sahti_0", "voidlow_0"];
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapManager), "DrawNodes")]
        public static void DrawNodesPost(ref MapManager __instance, string[][] __state, ref Dictionary<string, Node> ___mapNode) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            AtOManager.Instance.mapVisitedNodes.AddRange([..__state[0]]);
            AtOManager.Instance.mapVisitedNodesAction.AddRange([..__state[1]]);

            for (int index = 0; index < AtOManager.Instance.mapVisitedNodes.Count; ++index)
            {
                if (AtOManager.Instance.mapVisitedNodes[index] != "" && ___mapNode.ContainsKey(AtOManager.Instance.mapVisitedNodes[index])) {
                    ___mapNode[AtOManager.Instance.mapVisitedNodes[index]].SetVisited();
                }
                if(zoneStartNodes.Contains(AtOManager.Instance.mapVisitedNodes[index])) {
                    break;
                }
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapManager), "GetMapNodesCo")]
        public static void GetMapNodesCoPre(ref MapManager __instance, out string[][] __state) {
            __state = [[..AtOManager.Instance.mapVisitedNodes], [..AtOManager.Instance.mapVisitedNodesAction]];
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            AtOManager.Instance.mapVisitedNodes.Clear();
            AtOManager.Instance.mapVisitedNodesAction.Clear();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapManager), "GetMapNodesCo")]
        public static void GetMapNodesCoPost(ref MapManager __instance, string[][] __state) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            AtOManager.Instance.mapVisitedNodes.AddRange([..__state[0]]);
            AtOManager.Instance.mapVisitedNodesAction.AddRange([..__state[1]]);
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapManager), "AssignGameNodes")]
        public static void AssignGameNodesPre(ref MapManager __instance, out string[][] __state) {
            __state = [[..AtOManager.Instance.mapVisitedNodes], [..AtOManager.Instance.mapVisitedNodesAction]];
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            AtOManager.Instance.mapVisitedNodes.Clear();
            AtOManager.Instance.mapVisitedNodesAction.Clear();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapManager), "AssignGameNodes")]
        public static void AssignGameNodesPost(ref MapManager __instance, string[][] __state) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            AtOManager.Instance.mapVisitedNodes.AddRange([..__state[0]]);
            AtOManager.Instance.mapVisitedNodesAction.AddRange([..__state[1]]);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CinematicManager), "DoCinematic")]
        public static bool DoCinematicPre(ref CinematicManager __instance, ref CinematicData ___cinematicData) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return true;
            if(AtOManager.Instance.CinematicId == "intro" && AtOManager.Instance.GetActNumberForText() > 1) {
                GameManager.Instance.ChangeScene("Map");
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AtOManager), nameof(AtOManager.FinishGame))]
        public static bool FinishGame(ref AtOManager __instance, ref CombatData ___currentCombatData) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return true;
            if(___currentCombatData.CombatId == "evoidhigh_13b") {
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapManager), "SetPositionInCurrentNode")]
        public static void SetPositionInCurrentNode(ref MapManager __instance, ref bool __result) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            if(__result == false && AtOManager.Instance.currentMapNode == "voidhigh_13" && AtOManager.Instance.bossesKilledName.Any(s => s.StartsWith("archonnihr", StringComparison.OrdinalIgnoreCase))) {
                CombatData currentCombatData = AtOManager.Instance.GetCurrentCombatData();
                CombatData globalCombatData = Globals.Instance.GetCombatData(currentCombatData?.CombatId);
                if(currentCombatData != globalCombatData) {
                    AtOManager.Instance.SetCombatData(globalCombatData);
                    currentCombatData = AtOManager.Instance.GetCurrentCombatData();
                }

                if(AtOManager.Instance.PlayerHasRequirement(Globals.Instance.GetRequirementData("e_endless_allow_perks")) && currentCombatData.EventData != Globals.Instance.GetEventData("e_endless_perk"))
                    currentCombatData.EventData = Globals.Instance.GetEventData("e_endless_perk");
                __result = true;
                return;
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(IntroNewGameManager), "DoIntro")]
        public static void DoIntro(ref IntroNewGameManager __instance) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            __instance.title.text = __instance.title.text.Replace(string.Format(Texts.Instance.GetText("actNumber"), (AtOManager.Instance.GetTownTier() + 2)), string.Format(Texts.Instance.GetText("actNumber"), AtOManager.Instance.GetActNumberForText()));
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuSaveButton), nameof(MenuSaveButton.SetGameData))]
        public static void SetGameData(ref MenuSaveButton __instance, GameData _gameData) {
            if(GameManager.Instance.IsObeliskChallenge() || GameManager.Instance.IsWeeklyChallenge())
                return;
            __instance.descriptionText.text = __instance.descriptionText.text.Replace(string.Format(Texts.Instance.GetText("actNumber"), Math.Min(4, _gameData.TownTier + 1)), string.Format(Texts.Instance.GetText("actNumber"), _gameData.TownTier + 1));
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.SetScore))]
        public static bool SetScore() {
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.SetObeliskScore))]
        public static bool SetObeliskScore() {
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.SetSingularityScore))]
        public static bool SetSingularityScore() {
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.SetWeeklyScore))]
        public static bool SetWeeklyScore() {
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MatchManager), nameof(MatchManager.CastCardAction))]
        public static void CastCardAction(MatchManager __instance, 
                                            ref CardData _cardActive,
                                            Transform targetTransformCast,
                                            ref CardItem theCardItem,
                                            string _uniqueCastId,
                                            bool _automatic,
                                            ref CardData _card,
                                            int _cardIterationTotal,
                                            int _cardSpecialValueGlobal) {
            if (!_automatic) {
                if(theCardItem.CardData.KillPet)
                    theCardItem.CardData.KillPet = false;
            } else if(_cardActive == null && _card !=  null) {
                if(_card.KillPet)
                    _card.KillPet = false;
            } else {
                if(_cardActive.KillPet)
                    _cardActive.KillPet = false;
            }
        }


        
    }
}