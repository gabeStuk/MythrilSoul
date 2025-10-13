using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Modding;
using Satchel;
using UnityEngine;
using UnityEngine.UI;
using Satchel.BetterMenus;
using TMPro;

namespace MythrilSoul
{
    public class MythrilSoul : Mod, ICustomMenuMod, IGlobalSettings<MSGlobalSettings>
    {

        public static string[] packs = [
            "mythril",
            "pale",
            "sol"
        ];

        private Menu MenuRef;
        private AssetBundle bundle;

        public class MythrilQuitter : MonoBehaviour
        {
            private static CoroutineRunner instance;

            public static Coroutine Start(IEnumerator routine)
            {
                if (instance == null)
                {
                    var go = new GameObject("CoroutineRunner");
                    instance = go.AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return instance.StartCoroutine(routine);
            }
        }

        public MythrilSoul() : base("MythrilSoul") { }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? modToggleDelegates)
        {
            MenuRef ??= new(
                name: "Mythril Soul",
                elements: [
                    new HorizontalOption(
                        name: "Appearance",
                        description: "",
                        values: packs,
                        applySetting: index => {
                            GS.pack = packs[index];
                            LoadThings();
                        },
                        loadSetting: () => Array.IndexOf(packs, GS.pack)
                    ),
                    new HorizontalOption(
                        name: "Pause Control",
                        description: "Allow this mod to disable pausing on low health",
                        values: [ "On", "Off" ],
                        applySetting: index => GS.usePause = index == 0,
                        loadSetting: () => GS.usePause ? 0 : 1
                    )
                ]
            );

            return MenuRef.GetMenuScreen(modListMenu);
        }

        public MSGlobalSettings GS { get; set; } = new MSGlobalSettings();

        public bool ToggleButtonInsideMenu => false;

        public override string GetVersion() => "1.0.0";

        public GameObject mythrilSoul = new GameObject();

        public void OnLoadGlobal(MSGlobalSettings ms)
        {
            GS = ms;
        }

        public MSGlobalSettings OnSaveGlobal()
        {
            return GS;
        }

        public void SaveMythril()
        {
            GS.msGames.Add(GameManager.instance.profileID);
        }

        public GameObject GetSteelSoulButton()
        {
            return UIManager.instance.playModeMenuScreen.gameObject.GetComponentsInChildren<Transform>(true).Where(t => t.gameObject.GetName() == "SteelButton").First().gameObject;
        }

        public GameObject GetSlot(string name)
        {
            return UIManager.instance.saveProfileScreen.gameObject.GetComponentsInChildren<Transform>(true).Where(t => t.gameObject.GetName() == name).First().gameObject;
        }

        public GameObject GetActiveSlot(GameObject slot, int slotIdx)
        {
            return slot.transform.Find("ActiveSaveSlot" + slotIdx).gameObject;
        }

        public IEnumerable<GameObject> GetButtons()
        {
            return UIManager.instance.playModeMenuScreen.gameObject.GetComponentsInChildren<Transform>(true).Where(t => t.gameObject.GetName().Contains("Button")).Select(t => t.gameObject);
        }

        public IEnumerator CheckSetPause(On.GameManager.orig_PauseGameToggle orig, global::GameManager self)
        {
            if (GS.usePause && GS.msGames.Contains(GameManager.instance.profileID) && GameManager.instance.gameState == GlobalEnums.GameState.PLAYING && GameManager.instance.playerData.health <= 2)
            {
                yield return null;
            }
            else
            {
                yield return orig(self);
            }
        }

        public void LoadBundle()
        {
            Log("Loading assetbundle");
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("MythrilSoul.mythril");
            bundle = AssetBundle.LoadFromStream(s);
            s.Dispose();
        }

        public void LoadThings()
        {
            //LoadTex();
            if (GameManager.instance.IsGameplayScene())
            {
                LoadTex();
            }
            else
            {
                Log("Updating menu textures, updating mythril mode button; pack: " + GS.pack);
                GameObject.DestroyImmediate(mythrilSoul);
                mythrilSoul = GameObject.Instantiate(GetSteelSoulButton());
                MenuScreen playScreen = UIManager.instance.playModeMenuScreen;
                playScreen.content.GetComponent<VerticalLayoutGroup>().spacing = 2f;
                mythrilSoul.transform.Find("Image").gameObject.GetComponent<Image>().sprite = bundle.LoadAsset<Sprite>(GS.pack + "-mode");
                mythrilSoul.name = "MythrilButton";
                mythrilSoul.RemoveComponent<StartGameEventTrigger>();
                var sgec = mythrilSoul.AddComponent<StartGameEventTriggerMythril>();
                sgec.permaDeath = true;
                sgec.bossRush = false;
                sgec.preSubmit = SaveMythril;
                mythrilSoul.transform.Find("Text").gameObject.GetComponent<AutoLocalizeTextUI>().enabled = false;
                mythrilSoul.transform.Find("Text").gameObject.GetComponent<Text>().text = "Mythril Soul";
                mythrilSoul.transform.Find("DescriptionText").gameObject.GetComponent<AutoLocalizeTextUI>().enabled = false;
                mythrilSoul.transform.Find("DescriptionText").gameObject.GetComponent<Text>().text = "No Reviving. Death is permanent. Face your dreams.";
                mythrilSoul.transform.SetParent(playScreen.content.transform, false);
                playScreen.GetComponent<MenuButtonList>().AddSelectable(mythrilSoul.GetComponent<UnityEngine.UI.MenuButton>(), index: 2);
                mythrilSoul.transform.SetSiblingIndex(2);
                foreach (var item in GetButtons())
                    item.SetScale(0.8f, 0.8f);
                UpdateSaveMenus();
            }
        }
        
        public void LoadTex()
        {
            Log("Updating HUD Frame texture with pack " + GS.pack);
            if (GS.msGames.Contains(GameManager.instance.profileID))
                GameCameras.instance.hudCanvas.transform.GetComponentsInChildren<Transform>().Where(t => t.gameObject.name == "HUD_frame").First().gameObject.GetComponent<tk2dSprite>().CurrentSprite.material.mainTexture = bundle.LoadAsset<Sprite>(GS.pack + "-atlas").texture;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
            {
                if (s.name == "Menu_Title") LoadThings();
                if (s.name == "End_Credits" && GS.msGames.Contains(GameManager.instance.profileID))
                {
                    var co = GameObject.Find("credits object");
                    var ca = co.transform.GetComponentsInChildren<Transform>(true).Where(t => t.gameObject.GetName() == "congrats body perma");
                    var cbp = ca.First().gameObject;
                    cbp.GetComponent<SetTextMeshProGameText>().enabled = false;
                    cbp.GetComponent<TextMeshPro>().text = """
                        You played skillfully and proved you had a Mythril Soul.
                        Thank you for taking the time to explore and conquer the world we built.
                        With your dreams subdued, betray then void: take up the silken mantle.
                    """;
                }
            };
            LoadBundle();
            On.GameManager.BeginScene += (o, s) =>
            {
                if (GameManager.instance.IsGameplayScene()) LoadTex();
                o(s);
            };
            ModHooks.NewGameHook += LoadTex;
            ModHooks.AfterSaveGameClearHook += (i) =>
            {
                GS.msGames.Remove(i);
            };
            ModHooks.FinishedLoadingModsHook += () =>
            {
                On.GameManager.PauseGameToggle += CheckSetPause;
                LoadThings();
            };

            ModHooks.AfterTakeDamageHook += (h, d) =>
            {
                if (d >= GameManager.instance.playerData.health + GameManager.instance.playerData.healthBlue && GS.msGames.Contains(GameManager.instance.profileID))
                {
                    // kill
                    PlayerData.instance.permadeathMode = 2;
                    MythrilQuitter.Start(GameManager.instance.ReturnToMainMenu((GameManager.ReturnToMainMenuSaveModes.SaveAndContinueOnFail)));
                }

                return d;
            };
        }

        public void UpdateSaveMenus()
        {
            
            var screen = UIManager.instance.saveProfileScreen;
            List<String> slotnames = GS.msGames.ToList().Select(i =>
            {
                switch(i)
                {
                    case 1:
                        return "SlotOne";
                    case 2:
                        return "SlotTwo";
                    case 3:
                        return "SlotThree";
                    case 4:
                        return "SlotFour";
                    default: return "";
                }
            }).ToList();
            for (int i = 0; i < slotnames.Count; i++)
            {
                var slot = GetSlot(slotnames[i]);
                slot.transform.Find("BrokenSteelOrb").gameObject.GetComponent<Image>().sprite = bundle.LoadAsset<Sprite>(GS.pack + "-broken");
                var slotActive = GetActiveSlot(slot, GS.msGames.ToList()[i]);
                if (slotActive)
                {
                    slotActive.transform.GetComponentsInChildren<Transform>().Where(t => t.GetName() == "SteelSoulOrb").First().gameObject.GetComponent<Image>().sprite = bundle.LoadAsset<Sprite>(GS.pack + "-select");
                    slotActive.transform.GetComponentsInChildren<Transform>().Where(t => t.GetName() == "HealthSlots").First().gameObject.GetComponent<SaveProfileHealthBar>().steelHealth = bundle.LoadAsset<Sprite>(GS.pack + "-heart");
                    slotActive.transform.GetComponentsInChildren<Transform>().Where(t => t.GetName() == "MPSlots").First().gameObject.GetComponent<SaveProfileMPSlots>().steelSoulOrb = bundle.LoadAsset<Sprite>(GS.pack + "-soul");
                }
            }
        }
    }

    public class MSGlobalSettings
    {
        public HashSet<int> msGames = [];
        public string pack;
        public bool usePause;

        public MSGlobalSettings() {
            pack = "mythril";
            usePause = true;
        }
    }
}