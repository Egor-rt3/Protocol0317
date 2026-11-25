using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Protocol0317
{
    public partial class ProtocolGame : MonoBehaviour
    {
        private void BuildUI()
        {
            // Required for mouse interaction with Unity UI buttons.
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            screenRoot = canvasGO;
            var canvas = canvasGO.GetComponent<Canvas>();
            screenCanvas = canvas;
            screenRaycaster = canvasGO.GetComponent<GraphicRaycaster>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = sceneCamera;
            var canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1600,900);
            canvasGO.transform.position = new Vector3(0f,1.85f,0.92f);
            canvasGO.transform.rotation = Quaternion.identity;
            canvasGO.transform.localScale = Vector3.one * .0008f;
            var scaler = canvasGO.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            CreatePanel(canvasGO.transform, Vector2.zero, Vector2.one, new Color(.012f,.025f,.03f,.98f));
            CreatePanel(canvasGO.transform, new Vector2(.06f,.08f), new Vector2(.65f,.87f), new Color(.025f,.045f,.055f,.91f));
            CreatePanel(canvasGO.transform, new Vector2(.68f,.08f), new Vector2(.96f,.87f), new Color(.035f,.06f,.07f,.95f));
            headline = CreateText(canvasGO.transform, new Vector2(.085f,.75f), new Vector2(.63f,.84f), 29, TextAnchor.MiddleLeft, FontStyle.Bold);
            objectiveText = CreateText(canvasGO.transform, new Vector2(.085f,.85f), new Vector2(.95f,.93f), 18, TextAnchor.MiddleLeft, FontStyle.Bold);
            narrative = CreateText(canvasGO.transform, new Vector2(.085f,.27f), new Vector2(.63f,.72f), 24, TextAnchor.UpperLeft);
            feedback = CreateText(canvasGO.transform, new Vector2(.085f,.12f), new Vector2(.63f,.25f), 20, TextAnchor.MiddleLeft);
            readout = CreateText(canvasGO.transform, new Vector2(.69f,.72f), new Vector2(.95f,.83f), 19, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int i=0;i<3;i++)
            {
                int index = i; float y = .61f-i*.14f;
                powerLabels[i] = CreateText(canvasGO.transform, new Vector2(.69f,y), new Vector2(.86f,y+.08f), 18, TextAnchor.MiddleLeft);
                MakeButton(canvasGO.transform, new Vector2(.86f,y), new Vector2(.905f,y+.08f), "−", ()=>ChangePower(index,-1));
                MakeButton(canvasGO.transform, new Vector2(.908f,y), new Vector2(.953f,y+.08f), "+", ()=>ChangePower(index,1));
            }
            diagnosticButton = MakeButton(canvasGO.transform, new Vector2(.69f,.025f), new Vector2(.95f,.105f), "ЗАПУСТИТЬ ДИАГНОСТИКУ", StartDiagnostics);
            diagnosticButtonText = diagnosticButton.GetComponentInChildren<Text>();
            resolveButton = MakeButton(canvasGO.transform, new Vector2(.69f,.13f), new Vector2(.95f,.24f), "НАЧАТЬ СМЕНУ", BeginOrResolve);
            buttonText = resolveButton.GetComponentInChildren<Text>();
        }

        private static RectTransform Rect(GameObject go, Vector2 min, Vector2 max)
        {
            var r = go.GetComponent<RectTransform>(); r.anchorMin=min; r.anchorMax=max;
            r.offsetMin=Vector2.zero; r.offsetMax=Vector2.zero; return r;
        }
        private void CreatePanel(Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent,false);
            Rect(go,min,max); go.GetComponent<Image>().color=color;
        }
        private Text CreateText(Transform parent, Vector2 min, Vector2 max, int size, TextAnchor anchor, FontStyle style=FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent,false);
            Rect(go,min,max); var t=go.GetComponent<Text>(); t.font=font; t.fontSize=size; t.fontStyle=style;
            t.color=textColor; t.alignment=anchor; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Overflow;
            return t;
        }
        private Button MakeButton(Transform parent, Vector2 min, Vector2 max, string title, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent,false);
            Rect(go,min,max); go.GetComponent<Image>().color=new Color(.13f,.28f,.31f);
            var b=go.GetComponent<Button>(); b.onClick.AddListener(()=>onClick());
            var t=CreateText(go.transform,Vector2.zero,Vector2.one,21,TextAnchor.MiddleCenter,FontStyle.Bold); t.text=title;
            return b;
        }
        // The environment is baked into Main.unity in the editor, so it exists before Play
        // and in Windows standalone builds. UI and gameplay remain initialized at runtime.
        public void BakeEnvironmentForEditor()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (transform.Find("ControlRoom") != null) return;
            BuildRoom();
            room.transform.SetParent(transform, true);
            sceneCamera.transform.SetParent(transform, true);
        }

        private bool BindSavedRoom()
        {
            var saved = transform.Find("ControlRoom");
            var savedCamera = transform.Find("Main Camera");
            if (saved == null || savedCamera == null) return false;
            room = saved.gameObject;
            sceneCamera = savedCamera.GetComponent<Camera>();
            if (sceneCamera == null) return false;
            emergencyLight = saved.Find("EmergencyLight")?.GetComponent<Light>();
            normalLight = saved.Find("CeilingWhiteLight")?.GetComponent<Light>();
            redBeacons = new Light[2];
            for (int i = 0; i < 2; i++)
                redBeacons[i] = saved.Find("RedAlarmBeacon" + i)?.GetComponent<Light>();
            return true;
        }

        private void BuildRoom()
        {
            room = new GameObject("ControlRoom");
            CreateCube("Floor",new Vector3(0,-.2f,0),new Vector3(14,.4f,10),new Color(.13f,.17f,.18f));
            CreateCube("BackWall",new Vector3(0,3,5),new Vector3(14,6,.25f),new Color(.12f,.16f,.18f));
            // Close the room behind the seated operator: no exposed black void.
            CreateCube("FrontWallBehindPlayer",new Vector3(0,3,-5),new Vector3(14,6,.25f),new Color(.12f,.16f,.18f));
            for (int n = 0; n < 5; n++)
                CreateCube("FrontWallRib"+n,new Vector3(-5.5f+n*2.75f,3f,-4.82f),new Vector3(.12f,5.6f,.16f),new Color(.24f,.28f,.29f));
            CreateCube("RearMaintenancePanel",new Vector3(2.5f,2.6f,-4.72f),new Vector3(2.2f,1.8f,.16f),new Color(.2f,.26f,.28f));
            CreateCube("RearCableTray",new Vector3(0f,4.6f,-4.72f),new Vector3(10f,.22f,.22f),new Color(.29f,.31f,.33f));
            CreateCube("LeftWall",new Vector3(-7,3,0),new Vector3(.25f,6,10),new Color(.12f,.16f,.18f));
            CreateCube("RightWall",new Vector3(7,3,0),new Vector3(.25f,6,10),new Color(.12f,.16f,.18f));
            CreateCube("Desk",new Vector3(0,.7f,1),new Vector3(8,1.2f,1.6f),new Color(.18f,.22f,.22f));
            // One physical terminal: its front surface faces the seated camera (-Z).
            CreateCube("TerminalFrame",new Vector3(0f,1.85f,1.1f),new Vector3(1.55f,1.10f,.32f),new Color(.09f,.11f,.13f));
            CreateCube("TerminalDisplay",new Vector3(0f,1.85f,.935f),new Vector3(1.36f,.82f,.015f),new Color(.01f,.025f,.03f));
            CreateCube("CCTVFrame",new Vector3(2.25f,1.85f,1.1f),new Vector3(1.55f,1.10f,.32f),new Color(.08f,.1f,.12f));
            CreateCube("CCTVDisplay",new Vector3(2.25f,1.85f,.935f),new Vector3(1.36f,.82f,.015f),new Color(.005f,.015f,.02f));
            CreateCube("CCTVStand",new Vector3(2.25f,1.12f,1.1f),new Vector3(.12f,.4f,.12f),new Color(.13f,.16f,.18f));
            CreateCube("TerminalStand",new Vector3(0f,1.12f,1.1f),new Vector3(.12f,.4f,.12f),new Color(.13f,.16f,.18f));
            CreateCube("Ceiling",new Vector3(0,6,0),new Vector3(14,.25f,10),new Color(.09f,.12f,.13f));
            CreateCube("ObservationWindow",new Vector3(0,3,4.8f),new Vector3(4,2,.12f),new Color(.025f,.035f,.045f));
            var lamp=new GameObject("EmergencyLight",typeof(Light)); lamp.transform.SetParent(room.transform);
            lamp.transform.position=new Vector3(0,4,0); lamp.GetComponent<Light>().type=LightType.Point;
            lamp.GetComponent<Light>().range=15; lamp.GetComponent<Light>().color=new Color(.7f,.25f,.22f);
            emergencyLight = lamp.GetComponent<Light>();
            emergencyLight.intensity = 0f;
            emergencyLight.color = Color.red;
            var whiteGO = new GameObject("CeilingWhiteLight",typeof(Light));
            whiteGO.transform.SetParent(room.transform);
            whiteGO.transform.position = new Vector3(0f,4.9f,-1f);
            normalLight = whiteGO.GetComponent<Light>();
            normalLight.type = LightType.Point;
            normalLight.range = 14f;
            normalLight.intensity = 1.1f;
            normalLight.color = new Color(.75f,.88f,1f);
            redBeacons = new Light[2];
            for (int i=0;i<2;i++)
            {
                float x = i==0 ? -4.8f : 4.8f;
                CreateCube("RedAlarmHousing"+i,new Vector3(x,4.2f,4.78f),new Vector3(.55f,.35f,.2f),new Color(.24f,.04f,.04f));
                var beacon = new GameObject("RedAlarmBeacon"+i,typeof(Light));
                beacon.transform.SetParent(room.transform);
                beacon.transform.position = new Vector3(x,4.2f,4.4f);
                redBeacons[i] = beacon.GetComponent<Light>();
                redBeacons[i].type = LightType.Point;
                redBeacons[i].color = Color.red;
                redBeacons[i].range = 9f;
                redBeacons[i].intensity = 0f;
            }
            // Mechanical ventilation on the back wall. Fan blades rotate in the XY plane,
            // facing the player at negative Z. The decorative guard remains stationary.
            BuildVentilation();
            // Locked exit and illuminated EXIT sign on the rear wall.
            CreateCube("ExitDoor",new Vector3(-4.7f,1.35f,4.81f),new Vector3(1.55f,2.7f,.15f),new Color(.11f,.16f,.19f));
            CreateCube("ExitDoorFrameTop",new Vector3(-4.7f,2.8f,4.69f),new Vector3(1.9f,.17f,.22f),new Color(.36f,.39f,.4f));
            CreateCube("ExitDoorFrameLeft",new Vector3(-5.56f,1.4f,4.69f),new Vector3(.14f,2.9f,.22f),new Color(.36f,.39f,.4f));
            CreateCube("ExitDoorFrameRight",new Vector3(-3.84f,1.4f,4.69f),new Vector3(.14f,2.9f,.22f),new Color(.36f,.39f,.4f));
            CreateCube("ExitDoorHandle",new Vector3(-4.1f,1.25f,4.65f),new Vector3(.1f,.25f,.13f),new Color(.8f,.72f,.25f));
            CreateWorldLabel("EXIT",new Vector3(-4.7f,3.12f,4.59f),new Vector3(.14f,.14f,.14f),Color.green);
            CreateWorldLabel("LOCKED",new Vector3(-4.7f,1.85f,4.63f),new Vector3(.065f,.065f,.065f),new Color(1f,.2f,.2f));
            // Wall-mounted electrical switchboard, conduits and indicator lamps.
            CreateCube("ElectricalPanel",new Vector3(6.82f,2.2f,.2f),new Vector3(.22f,2.05f,1.65f),new Color(.24f,.27f,.28f));
            CreateCube("ElectricalPanelDoor",new Vector3(6.66f,2.2f,.2f),new Vector3(.06f,1.8f,1.45f),new Color(.13f,.19f,.2f));
            for (int i=0;i<5;i++)
            {
                CreateCube("BreakerSwitch"+i,new Vector3(6.59f,2.65f-i*.25f,-.25f),new Vector3(.09f,.14f,.15f),new Color(.65f,.68f,.63f));
            }
            CreateCube("ElectricalConduit",new Vector3(6.85f,4f,.2f),new Vector3(.16f,1.9f,.16f),new Color(.36f,.38f,.4f));
            CreateCube("StorageRack",new Vector3(-5.7f,1.6f,1.2f),new Vector3(1.3f,3.1f,.75f),new Color(.16f,.2f,.22f));
            for(int i=0;i<3;i++)
            {
                CreateCube("Shelf"+i,new Vector3(-5.7f,.6f+i*.9f,1.2f),new Vector3(1.5f,.12f,1.1f),new Color(.31f,.34f,.35f));
                CreateCube("SupplyBox"+i,new Vector3(-5.7f,.85f+i*.9f,1.2f),new Vector3(.65f,.35f,.6f),new Color(.28f,.3f,.23f));
            }
            CreateCube("WallPipe",new Vector3(0f,4.75f,4.7f),new Vector3(11f,.18f,.18f),new Color(.35f,.37f,.4f));
            var camGO=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)); camGO.tag="MainCamera";
            sceneCamera = camGO.GetComponent<Camera>();
            sceneCamera.fieldOfView = 60f;
            camGO.transform.position=new Vector3(0,1.85f,-2.4f); camGO.transform.LookAt(new Vector3(0,1.85f,.92f));
            camGO.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
            camGO.GetComponent<Camera>().backgroundColor=new Color(.025f,.035f,.045f);
        }
        private void BuildVentilation()
        {
            Vector3 center = new Vector3(4.6f, 3.35f, 4.53f);
            CreateCube("VentDuctHousing", center + new Vector3(0f,0f,.09f),
                new Vector3(2.05f,2.05f,.27f),new Color(.18f,.23f,.25f));
            CreateCube("VentOpening", center + new Vector3(0f,0f,-.065f),
                new Vector3(1.73f,1.73f,.055f),new Color(.018f,.025f,.028f));
            // Fixed louvers on the front, thin enough to see the spinning blades.
            for (int i = -3; i <= 3; i++)
                CreateCube("VentGuard_"+i, center + new Vector3(i*.22f,0f,-.19f),
                    new Vector3(.027f,1.68f,.035f),new Color(.38f,.44f,.45f));
            var rotor = new GameObject("VentilationFan_Rotating");
            rotor.transform.SetParent(room.transform);
            rotor.transform.position = center + new Vector3(0f,0f,-.125f);
            var spinner = rotor.AddComponent<VentilationFan>();
            spinner.rotationsPerMinute = 100f;
            for (int i=0;i<5;i++)
            {
                var blade=GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name="FanBlade_"+i;
                blade.transform.SetParent(rotor.transform,false);
                blade.transform.localRotation=Quaternion.Euler(0f,0f,i*72f);
                // Place each blade along its own local Y axis.
                blade.transform.localPosition=blade.transform.localRotation*new Vector3(0f,.42f,0f);
                blade.transform.localScale=new Vector3(.25f,.72f,.045f);
                var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var material=new Material(shader);
                material.color=new Color(.42f,.5f,.52f);
                blade.GetComponent<Renderer>().material=material;
                if (Application.isPlaying) Destroy(blade.GetComponent<Collider>()); else DestroyImmediate(blade.GetComponent<Collider>());
            }
            var hub=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name="FanAxle";
            hub.transform.SetParent(rotor.transform,false);
            hub.transform.localRotation=Quaternion.Euler(90f,0f,0f);
            hub.transform.localScale=new Vector3(.19f,.055f,.19f);
            if (Application.isPlaying) Destroy(hub.GetComponent<Collider>()); else DestroyImmediate(hub.GetComponent<Collider>());
            CreateCube("VentilationPowerConduit",new Vector3(5.85f,3.35f,4.7f),
                new Vector3(.6f,.12f,.12f),new Color(.28f,.31f,.34f));
        }

        private void CreateWorldLabel(string message, Vector3 position, Vector3 scale, Color color)
        {
            var go = new GameObject("Sign_"+message,typeof(TextMesh));
            go.transform.SetParent(room.transform);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.transform.rotation = Quaternion.Euler(0f,0f,0f);
            var label = go.GetComponent<TextMesh>();
            label.text = message;
            label.font = font;
            label.fontSize = 60;
            label.characterSize = 1f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && font != null) meshRenderer.sharedMaterial = font.material;
        }
        private void CreateCube(string name, Vector3 position, Vector3 scale, Color color)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(room.transform);
            go.transform.position=position; go.transform.localScale=scale;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var mat=new Material(shader);
            mat.color=color; go.GetComponent<Renderer>().material=mat;
        }
    }
}
