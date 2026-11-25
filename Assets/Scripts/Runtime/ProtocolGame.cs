using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Protocol0317
{
    public partial class ProtocolGame : MonoBehaviour
    {
        private GameState state;
        private Incident[] incidents;
        private Text headline, narrative, readout, feedback, buttonText;
        private Button resolveButton;
        private Text[] powerLabels = new Text[3];
        private bool started;
        private bool unexpected;
        private int currentIndex;
        private readonly Color textColor = new Color(.85f, .94f, .94f);
        private Font font;
        private GameObject room;
        private Camera sceneCamera;
        private Light emergencyLight;
        private Light normalLight;
        private Light[] redBeacons;
        private AudioSource sirenSource;
        private bool emergencyMode;
        private bool diagnosticsRunning;
        private bool diagnosticsReady;
        private float diagnosticsTimer;
        private const float diagnosticsDuration = 5f;
        private Button diagnosticButton;
        private Text diagnosticButtonText;
        private Text objectiveText;
        private Text cinematicText;
        private GameObject cinematicPanel;
        private bool cinematicActive;
        private int failureStreak;
        private float criticalWarningClock;
        private readonly float[] storyDurations = { 7f, 6f, 7f, 7f };
        private float emergencyClock;
        private bool cameraTransition;
        private float cameraBlend;
        private Vector3 cameraFromPosition;
        private Quaternion cameraFromRotation;
        private float cameraFromFov;
        private readonly Vector3 terminalClosePosition = new Vector3(0f,1.85f,-0.05f);
        private readonly float terminalCloseFov = 48f;
        private CCTVSystem cctv;
        private StationSystems station;
        private VentilationFan roomFan;
        private bool panelFocused;
        private bool lossChoiceVisible;
        private bool timeLoopMessageVisible;
        private bool tutorialVisible;
        private int tutorialPage;
        private bool endingInProgress;
        private GUIStyle switchboardLabelStyle;
        private GUIStyle switchboardButtonStyle;
        private GUIStyle switchboardHeaderStyle;
        private readonly Vector3 panelClosePosition = new Vector3(4.9f,2.2f,.2f);
        private bool cctvFocused;
        private Canvas screenCanvas;
        private GraphicRaycaster screenRaycaster;
        private AudioSource soundSource;
        private AudioClip clickSound, errorSound, successSound, alarmSound, introSound;
        private bool terminalFocused;
        private Vector3 seatedPosition = new Vector3(0f, 1.85f, -2.4f);
        private Quaternion terminalRotation;
        private GameObject screenRoot;
        private float introTime;
        private bool introActive = true;
        private bool effectActive;
        private Vector3 introStart = new Vector3(-3.7f, 2.5f, -4.4f);
        private Vector3 introEnd = new Vector3(0f, 1.85f, -2.4f);
        private Quaternion introStartRotation;
        private Quaternion introEndRotation;
        private float yaw;
        private float pitch;
        private readonly float introDuration = 10f;
        private readonly Color normalScreen = new Color(.025f,.045f,.055f,.96f);

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            try { incidents = IncidentLoader.Load().incidents; }
            catch (Exception e) { Debug.LogError(e); incidents = new Incident[0]; }
            if (!BindSavedRoom()) BuildRoom(); BuildUI(); cctv = gameObject.AddComponent<CCTVSystem>(); cctv.Initialize(sceneCamera, room.transform); LoadSounds(); station = gameObject.AddComponent<StationSystems>(); station.Initialize(cctv); roomFan = room.GetComponentInChildren<VentilationFan>(); ResetGame();
            screenRoot.SetActive(false);
            introStartRotation = Quaternion.LookRotation(new Vector3(0f,1.85f,0.92f) - introStart);
            introEndRotation = Quaternion.LookRotation(new Vector3(0f,1.85f,0.92f) - introEnd);
            terminalRotation = Quaternion.LookRotation(new Vector3(0f,1.85f,0.92f) - seatedPosition);
            if (introSound != null) soundSource.PlayOneShot(introSound, .45f);
            sceneCamera.transform.SetPositionAndRotation(introStart, introStartRotation);
            if (emergencyLight != null) StartCoroutine(IntroLighting());
        }


        private void Update()
        {
            if (introActive)
            {
                introTime += Time.deltaTime;
                float t = Mathf.Clamp01(introTime / introDuration);
                float smooth = t * t * (3f - 2f * t);
                sceneCamera.transform.SetPositionAndRotation(Vector3.Lerp(introStart,introEnd,smooth),
                    Quaternion.Slerp(introStartRotation,introEndRotation,smooth));
                if (t >= 1f)
                {
                    introActive = false;
                    sceneCamera.transform.position = seatedPosition;
                    screenRoot.SetActive(true);
                    SetTerminalFocus(false);
                    tutorialVisible = true;
                    tutorialPage = 0;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }
            if (cinematicActive || tutorialVisible) return;
            if (started && !state.Finished) { station.Tick(Time.deltaTime); if (roomFan != null) roomFan.enabled = station.IsPowered(2); SetEmergency(station.IsCritical || state.Integrity <= 35 || failureStreak >= 2); if (station.AllDestroyed) Finish(false); }
            if (diagnosticsRunning)
            {
                diagnosticsTimer -= Time.deltaTime;
                if (diagnosticsTimer <= 0f)
                {
                    diagnosticsRunning = false;
                    diagnosticsReady = true;
                    diagnosticButtonText.text = "ДИАГНОСТИКА ГОТОВА";
                    feedback.text = "Диагностика завершена. Подтвердите распределение питания.";
                    PlaySound(successSound, .25f);
                }
                else diagnosticButtonText.text = "СКАНИРОВАНИЕ " + Mathf.CeilToInt(diagnosticsTimer) + " С";
            }
            // Siren and red alarm only for critical station integrity.
            if (emergencyMode)
            {
                emergencyClock += Time.deltaTime;
                float pulse = Mathf.Sin(emergencyClock * 7f) > 0f ? 1f : 0.08f;
                if (redBeacons != null)
                    foreach (var light in redBeacons) if (light != null) light.intensity = 3.2f * pulse;
                if (normalLight != null) normalLight.intensity = .25f;
            }
            if (endingInProgress || lossChoiceVisible || timeLoopMessageVisible || state.Finished) return;
            if (started && panelFocused)
            {
                for (int i = 0; i < 4; i++)
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) station.SendRepair(i);
                if (Input.GetKeyDown(KeyCode.Alpha0)) station.CancelRepair();
            }
            if (Input.GetKeyDown(KeyCode.R)) SetPanelFocus(!panelFocused);
            if (Input.GetKeyDown(KeyCode.Q)) SetCCTVFocus(!cctvFocused);
            if (Input.GetKeyDown(KeyCode.E)) { if (cctvFocused) SetCCTVFocus(false); else SetTerminalFocus(!terminalFocused); }
            if (Input.GetKeyDown(KeyCode.Escape)) { if (panelFocused) SetPanelFocus(false); else if (cctvFocused) SetCCTVFocus(false); else if (terminalFocused) SetTerminalFocus(false); }
            if (cameraTransition)
            {
                cameraBlend = Mathf.Min(1f, cameraBlend + Time.deltaTime * 2.0f);
                float t = cameraBlend * cameraBlend * (3f - 2f * cameraBlend);
                Vector3 targetPosition = panelFocused ? panelClosePosition : cctvFocused ? new Vector3(2.25f,1.85f,-.10f) : terminalFocused ? terminalClosePosition : seatedPosition;
                sceneCamera.transform.position = Vector3.Lerp(cameraFromPosition,targetPosition,t);
                sceneCamera.transform.rotation = Quaternion.Slerp(cameraFromRotation,panelFocused ? Quaternion.LookRotation(new Vector3(6.7f,2.2f,.2f) - targetPosition) : cctvFocused ? Quaternion.LookRotation(new Vector3(2.25f,1.85f,.92f) - targetPosition) : terminalRotation,t);
                sceneCamera.fieldOfView = Mathf.Lerp(cameraFromFov,(terminalFocused || cctvFocused || panelFocused) ? terminalCloseFov : 60f,t);
                if (t >= 1f)
                {
                    cameraTransition = false;
                    if (screenRaycaster != null) screenRaycaster.enabled = terminalFocused;
                    if (cctv != null) cctv.SetInteractive(cctvFocused);
                }
            }
            if (!terminalFocused && !cctvFocused && !panelFocused && !cameraTransition)
            {
                bool looking = Input.GetMouseButton(1);
                Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !looking;
                if (looking)
                {
                    yaw = Mathf.Clamp(yaw + Input.GetAxis("Mouse X") * 2.0f, -75f, 75f);
                    pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2.0f, -40f, 40f);
                    sceneCamera.transform.rotation = terminalRotation * Quaternion.Euler(pitch, yaw, 0f);
                }
            }
        }

    }
}
