using BepInEx;
using BepInEx.Harmony;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KK_HCameraLight
{
    [BepInPlugin(nameof(KK_HCameraLight), nameof(KK_HCameraLight), "1.3")]
    public class KK_HCameraLight : BaseUnityPlugin {
    
        private static bool inH;
        private static bool preButtonCreated;
        private static bool lightLocked;

        private static Transform cameraLightTransform;

        private static Vector3 savedPosition;
        private static Quaternion savedAngle;
        private static TextMeshProUGUI btnText;

        private static Button.ButtonClickedEvent verticalReset;
        private static Button.ButtonClickedEvent horizontalReset;

        private void Awake() => HarmonyWrapper.PatchAll(typeof(KK_HCameraLight));

        private static void BtnClick() {
            lightLocked = !lightLocked;

            if (lightLocked) {
                savedPosition = cameraLightTransform.position;
                savedAngle = cameraLightTransform.rotation;
            }else {
                verticalReset.Invoke();
                horizontalReset.Invoke();
            }

            btnText.text = lightLocked ? "Unlock Light" : "Lock Light";
        }

        private static void CreateButton() {
            var light = GameObject.Find("Canvas/SubMenu/LightGroup/light").transform;
            if (light == null) 
                return;
        
            verticalReset = light.transform.Find("Vertical").GetChild(2).GetComponent<Button>().onClick; // get vertical reset for invoking
            horizontalReset = light.transform.Find("Horizontal").GetChild(2).GetComponent<Button>().onClick; // get horizontal reset for invoking

            Transform copiedEntry = Instantiate(light.Find("Power"), light, false);
            copiedEntry.name = "LockUnlock light";
            copiedEntry.gameObject.name = "LockUnlock light";

            var textShape = copiedEntry.GetChild(0);
            textShape.SetParent(null);
            Destroy(textShape.gameObject);

            var Slider = copiedEntry.GetChild(0);
            Slider.SetParent(null);
            Destroy(Slider.gameObject);

            var copiedBtn = copiedEntry.GetChild(0);
            copiedBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.24f, 0f);

            btnText = copiedBtn.GetChild(0).GetComponent<TextMeshProUGUI>();
            btnText.text = lightLocked ? "Unlock Light" : "Lock Light";

            var uiBtn = copiedBtn.GetComponent<Button>();

            for (int i = 0; i < uiBtn.onClick.GetPersistentEventCount(); i++)
                uiBtn.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

            uiBtn.onClick.AddListener(BtnClick);
        }

        private void OnLoadScene(Scene Scene, LoadSceneMode mode) {
            if (inH || (Scene.name != "H")) 
                return;
        
            inH = true;
            preButtonCreated = false;
            lightLocked = false;

            savedPosition = Vector3.zero;
            savedAngle = Quaternion.Euler(0f, 0f, 0f);
        }

        private void OnUnloadScene(Scene Scene) {
            if (inH && (Scene.name == "H")) 
                inH = false;
        }

        private void OnEnable() {
            SceneManager.sceneLoaded += OnLoadScene;
            SceneManager.sceneUnloaded += OnUnloadScene;
        }

        private void OnDisable() {
            SceneManager.sceneLoaded -= OnLoadScene;
            SceneManager.sceneUnloaded -= OnUnloadScene;
        }

        private void LateUpdate() {
            if (!lightLocked || !inH || cameraLightTransform == null) 
                return;
        
            cameraLightTransform.position = savedPosition;
            cameraLightTransform.rotation = savedAngle;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "SetShortcutKey")]
        public static void HSceneProc_SetShortcutKey_ParentCameraLight(HSceneProc __instance) {
            cameraLightTransform = __instance.lightCamera.transform;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "ShortCut")]
        public static void HSceneProc_ShortCut_CreateLightLockButton(HSceneProc __instance)
        {
            if (!inH || __instance.sprite.isFade || __instance.sprite.GetFadeKindProc() == HSprite.FadeKindProc.OutEnd) 
                return;

            if (preButtonCreated) 
                return;
        
            preButtonCreated = true;
            CreateButton();
        }
    }
}