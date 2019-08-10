using Harmony;

using BepInEx;
using BepInEx.Logging;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

using System.Collections;

using TMPro;

[BepInPlugin(nameof(KK_HCameraLight), nameof(KK_HCameraLight), "1.1")]
public class KK_HCameraLight : BaseUnityPlugin {

    int tries;

    bool inH;
    bool lightLocked;

    static Transform cameraLightTransform;

    Vector3 savedPosition;
    Quaternion savedAngle;
    TextMeshProUGUI btnText;

    Button.ButtonClickedEvent verticalReset;
    Button.ButtonClickedEvent horizontalReset;

    void Awake() {
        HarmonyInstance.Create(nameof(KK_HCameraLight)).PatchAll(typeof(KK_HCameraLight));
    }

    void BtnClick() {
        lightLocked = !lightLocked;

        if (lightLocked) {
            savedPosition = cameraLightTransform.position;
            savedAngle = cameraLightTransform.rotation;
        } else {
            verticalReset.Invoke();
            horizontalReset.Invoke();
        }

        btnText.text = lightLocked ? "Unlock Light" : "Lock Light";
    }

    IEnumerator InitMenu(float time) {
        yield return new WaitForSeconds(time);

        try {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null) {
                GameObject lightMenu = canvas.transform.GetChild(15).GetChild(1).GetChild(0).gameObject;

                if (lightMenu != null) {
                    verticalReset = lightMenu.transform.GetChild(3).GetChild(2).GetComponent<Button>().onClick; // get vertical reset for invoking
                    horizontalReset = lightMenu.transform.GetChild(4).GetChild(2).GetComponent<Button>().onClick; // get horizontal reset for invoking

                    Transform copiedEntry = Instantiate(lightMenu.transform.GetChild(5)); // copy the strength entry
                    copiedEntry.parent = lightMenu.transform;
                    copiedEntry.localScale = new Vector3(1f, 1f, 1f);
                    copiedEntry.name = "LockUnlock light";
                    copiedEntry.gameObject.name = "LockUnlock light";

                    var textShape = copiedEntry.GetChild(0); // remove title
                    textShape.parent = null;
                    Destroy(textShape);

                    var Slider = copiedEntry.GetChild(0); // remove slider
                    Slider.parent = null;
                    Destroy(Slider);

                    var copiedBtn = copiedEntry.GetChild(0); // fix size
                    copiedBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.24f, 0f);

                    btnText = copiedBtn.GetChild(0).GetComponent<TextMeshProUGUI>(); // set text
                    btnText.text = lightLocked ? "Unlock Light" : "Lock Light";

                    var uiBtn = copiedBtn.GetComponent<Button>();

                    for (int i = 0; i < uiBtn.onClick.GetPersistentEventCount(); i++)
                        uiBtn.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

                    uiBtn.onClick.AddListener(BtnClick);
                }
            }
        }
        catch(UnityException) {
            tries--;

            if(tries > 0) {
                StartCoroutine(InitMenu(1.5f));
            } else {
                BepInEx.Logger.Log(LogLevel.Message, "KK_HCameraLight failed creating UI button!");
            }
        }
    }

    void KK_HFixes_LoadScene(Scene Scene, LoadSceneMode mode) {
        if (!inH && (Scene.name == "H")) {
            inH = true;
            lightLocked = false;

            savedPosition = Vector3.zero;
            savedAngle = Quaternion.Euler(0f, 0f, 0f);

            tries = 5;

            StartCoroutine(InitMenu(4f));
        }
    }

    void KK_HFixes_UnloadScene(Scene Scene) {
        if (inH && (Scene.name == "H")) {
            inH = false;
        }
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
        if (lightLocked && inH && cameraLightTransform != null) {
            cameraLightTransform.position = savedPosition;
            cameraLightTransform.rotation = savedAngle;
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "SetShortcutKey")]
    public static void SetLightParent(HSceneProc __instance) {
        cameraLightTransform = __instance.lightCamera.transform;
    }

}
