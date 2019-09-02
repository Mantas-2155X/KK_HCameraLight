using Harmony;

using BepInEx;
using BepInEx.Logging;

using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

using TMPro;

[BepInPlugin(nameof(KK_HCameraLight), nameof(KK_HCameraLight), "1.2")]
public class KK_HCameraLight : BaseUnityPlugin {
    static bool inH;
    static bool menuCreated;
    static bool lightLocked;

    static Transform cameraLightTransform;

    static Vector3 savedPosition;
    static Quaternion savedAngle;
    static TextMeshProUGUI btnText;

    static Button.ButtonClickedEvent verticalReset;
    static Button.ButtonClickedEvent horizontalReset;

    void Awake() {
        HarmonyInstance.Create(nameof(KK_HCameraLight)).PatchAll(typeof(KK_HCameraLight));
    }

    static void BtnClick() {
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

    static void CreateMenu() {
        var light = GameObject.Find("Canvas/SubMenu/LightGroup/light").transform;
        if (light == null) return;
        
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

    void KK_HFixes_LoadScene(Scene Scene, LoadSceneMode mode) {
        if (inH || (Scene.name != "H")) return;
        
        inH = true;
        menuCreated = false;
        lightLocked = false;

        savedPosition = Vector3.zero;
        savedAngle = Quaternion.Euler(0f, 0f, 0f);
    }

    void KK_HFixes_UnloadScene(Scene Scene) {
        if (inH && (Scene.name == "H")) inH = false;
    }

    void OnEnable() {
        SceneManager.sceneLoaded += KK_HFixes_LoadScene;
        SceneManager.sceneUnloaded += KK_HFixes_UnloadScene;
    }

    void OnDisable() {
        SceneManager.sceneLoaded -= KK_HFixes_LoadScene;
        SceneManager.sceneUnloaded -= KK_HFixes_UnloadScene;
    }

    void LateUpdate() {
        if (!lightLocked || !inH || cameraLightTransform == null) return;
        
        cameraLightTransform.position = savedPosition;
        cameraLightTransform.rotation = savedAngle;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "SetShortcutKey")]
    [UsedImplicitly]
    public static void SetLightParent(HSceneProc __instance) {
        cameraLightTransform = __instance.lightCamera.transform;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "ShortCut")]
    [UsedImplicitly]
    public static void CreateMenuPatch(HSceneProc __instance) {
        if (inH && !__instance.sprite.isFade && __instance.sprite.GetFadeKindProc() != HSprite.FadeKindProc.OutEnd) {
            if (!menuCreated) {
                menuCreated = true;
                
                CreateMenu();
            }
        }
    }
}
