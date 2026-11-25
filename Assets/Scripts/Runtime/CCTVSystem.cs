using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Protocol0317
{
    // Four real-time Unity cameras feed a second physical monitor using RenderTexture.
    public class CCTVSystem : MonoBehaviour
    {
        private readonly string[] names = { "CAM 01  REACTOR", "CAM 02  PUMPS", "CAM 03  VENT", "CAM 04  AIRLOCK" };
        private Camera[] cameras = new Camera[4];
        private RenderTexture[] feeds = new RenderTexture[4];
        private Light[] roomLights = new Light[4];
        private Light[] warnings = new Light[4];
        private Transform[] movingParts = new Transform[4];
        private RawImage image;
        private Text label, status;
        private GraphicRaycaster raycaster;
        private int activeCamera;
        private int activeIncident = -1;
        private bool incidentActive;
        private readonly float[] health = {100f,100f,100f,100f};
        private readonly bool[] powered = {true,true,true,true};
        private readonly ParticleSystem[] steam = new ParticleSystem[4];
        private readonly ParticleSystem[] sparks = new ParticleSystem[4];
        private readonly bool[] destroyedFx = new bool[4];
        private Font font;

        public void Initialize(Camera playerCamera, Transform parent)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < 4; i++) BuildLocation(i);
            var root = new GameObject("CCTV WorldSpace UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(2.25f, 1.85f, .922f);
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * .0008f;
            var rect = root.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1600, 900);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = playerCamera;
            raycaster = root.GetComponent<GraphicRaycaster>(); raycaster.enabled = false;
            Panel(root.transform, new Vector2(0,0), new Vector2(1,1), new Color(.015f,.03f,.04f,1));
            image = Make<RawImage>(root.transform, new Vector2(.045f,.25f), new Vector2(.955f,.90f));
            image.texture = feeds[0]; image.color = Color.white;
            label = Text(root.transform, new Vector2(.05f,.91f), new Vector2(.95f,.99f), 32, TextAnchor.MiddleLeft);
            status = Text(root.transform, new Vector2(.05f,.18f), new Vector2(.95f,.25f), 22, TextAnchor.MiddleLeft);
            for (int i=0;i<4;i++)
            {
                int index=i;
                float x=.04f+i*.24f;
                var button = Make<Button>(root.transform, new Vector2(x,.035f), new Vector2(x+.22f,.15f));
                button.gameObject.AddComponent<Image>().color = new Color(.08f,.24f,.29f,1);
                button.targetGraphic = button.GetComponent<Image>();
                button.onClick.AddListener(()=>SelectCamera(index));
                var t = Text(button.transform, Vector2.zero, Vector2.one, 25, TextAnchor.MiddleCenter);
                t.text = "CAM 0"+(i+1);
            }
            SelectCamera(0);
        }

        private void BuildLocation(int i)
        {
            var root = new GameObject(names[i]+" - 3D ROOM");
            root.transform.position = new Vector3(30f+i*22f,0f,0f);
            Transform tr = root.transform;
            Cube(tr,"Floor",new Vector3(0,-.2f,0),new Vector3(12,.4f,12),new Color(.16f,.19f,.2f));
            Cube(tr,"Back Wall",new Vector3(0,3,5),new Vector3(12,6,.3f),new Color(.13f,.18f,.21f));
            Cube(tr,"Left Wall",new Vector3(-6,3,0),new Vector3(.3f,6,10),new Color(.12f,.15f,.18f));
            Cube(tr,"Right Wall",new Vector3(6,3,0),new Vector3(.3f,6,10),new Color(.12f,.15f,.18f));
            Cube(tr,"Ceiling",new Vector3(0,6,0),new Vector3(12,.3f,12),new Color(.1f,.13f,.16f));
            for(int j=0;j<5;j++) Cube(tr,"Floor stripe",new Vector3(-4+j*2,.025f,1),new Vector3(.12f,.05f,8),new Color(.32f,.34f,.19f));
            var lamp = new GameObject("Room light",typeof(Light)); lamp.transform.SetParent(tr,false);
            lamp.transform.localPosition = new Vector3(0,4,-1);
            var light=lamp.GetComponent<Light>(); roomLights[i]=light; light.type=LightType.Point; light.range=18; light.intensity=2.3f; light.color=new Color(.7f,.85f,1);
            var warn = new GameObject("Emergency warning light",typeof(Light)); warn.transform.SetParent(tr,false);
            warn.transform.localPosition=new Vector3(0,4,3);
            warnings[i]=warn.GetComponent<Light>(); warnings[i].type=LightType.Point; warnings[i].range=14;
            warnings[i].color=Color.red; warnings[i].intensity=0;
            if (i==0)
            {
                Cube(tr,"Reactor pedestal",new Vector3(0,.7f,2),new Vector3(3,1.4f,3),Color.gray);
                var reactor=GameObject.CreatePrimitive(PrimitiveType.Cylinder); reactor.name="Reactor core";
                reactor.transform.SetParent(tr,false); reactor.transform.localPosition=new Vector3(0,2.3f,2);
                reactor.transform.localScale=new Vector3(1.25f,1.1f,1.25f);
                reactor.GetComponent<Renderer>().material.color=new Color(.05f,.8f,.85f);
                movingParts[i]=reactor.transform;
                for(int j=0;j<4;j++) Cube(tr,"Reactor coolant pipe",new Vector3(-4+j*2.6f,1.4f,4.7f),new Vector3(.35f,2.5f,.4f),Color.gray);
            }
            else if (i==1)
            {
                for(int j=0;j<3;j++)
                {
                    var pump=GameObject.CreatePrimitive(PrimitiveType.Cylinder); pump.name="Pump " + j;
                    pump.transform.SetParent(tr,false); pump.transform.localPosition=new Vector3(-3+j*3,1.5f,2);
                    pump.transform.localScale=new Vector3(1,1.4f,1);
                    pump.GetComponent<Renderer>().material.color=new Color(.25f,.42f,.5f);
                    if(j==1) movingParts[i]=pump.transform;
                    Cube(tr,"Pipe",new Vector3(-3+j*3,3.1f,2),new Vector3(.3f,1,.3f),Color.gray);
                }
            }
            else if (i==2)
            {
                Cube(tr,"Ventilation duct",new Vector3(0,2.7f,3.7f),new Vector3(6,3,2),Color.gray);
                var fan=new GameObject("Rotating ventilation rotor"); fan.transform.SetParent(tr,false);
                fan.transform.localPosition=new Vector3(0,2.7f,2.6f); movingParts[i]=fan.transform;
                for(int j=0;j<5;j++)
                {
                    var blade=GameObject.CreatePrimitive(PrimitiveType.Cube); blade.name="Fan blade";
                    blade.transform.SetParent(fan.transform,false);
                    blade.transform.localRotation=Quaternion.Euler(0,0,j*72);
                    blade.transform.localPosition=blade.transform.localRotation*new Vector3(0,1.1f,0);
                    blade.transform.localScale=new Vector3(.55f,2.0f,.13f);
                    blade.GetComponent<Renderer>().material.color=new Color(.4f,.53f,.56f);
                }
            }
            else
            {
                Cube(tr,"Airlock frame",new Vector3(0,2.5f,4.65f),new Vector3(4.2f,5,.5f),Color.gray);
                var door=Cube(tr,"Sealed airlock door",new Vector3(0,2.4f,4.28f),new Vector3(3.4f,4.4f,.2f),new Color(.18f,.35f,.38f));
                movingParts[i]=door.transform;
                Cube(tr,"Warning stripe",new Vector3(0,4.7f,4.08f),new Vector3(3.5f,.15f,.08f),Color.yellow);
            }
            steam[i]=CreateParticles(tr,"Steam leak",new Vector3(i==1 ? 2f : -2f,1.8f,2f),new Color(.7f,.8f,.85f,.55f),3f,1.2f);
            sparks[i]=CreateParticles(tr,"Electrical sparks",new Vector3(1.6f,3.3f,2.5f),new Color(1f,.65f,.12f,1f),.5f,.18f);
            var camObject=new GameObject("Security Camera",typeof(Camera)); camObject.transform.SetParent(tr,false);
            camObject.transform.localPosition=new Vector3(0,3.4f,-4.3f);
            camObject.transform.LookAt(tr.TransformPoint(new Vector3(0,2,2.5f)));
            cameras[i]=camObject.GetComponent<Camera>(); cameras[i].fieldOfView=62;
            cameras[i].clearFlags=CameraClearFlags.SolidColor; cameras[i].backgroundColor=Color.black;
            cameras[i].depth=-10;
            feeds[i]=new RenderTexture(1024,576,16,RenderTextureFormat.ARGB32);
            feeds[i].name="CCTV Feed " + (i+1); feeds[i].Create();
            cameras[i].targetTexture=feeds[i]; cameras[i].enabled=false;
        }

        private ParticleSystem CreateParticles(Transform parent,string title,Vector3 pos,Color color,float lifetime,float size)
        {
            var obj=new GameObject(title,typeof(ParticleSystem)); obj.transform.SetParent(parent,false); obj.transform.localPosition=pos;
            var ps=obj.GetComponent<ParticleSystem>(); var main=ps.main;
            main.loop=true; main.startLifetime=lifetime; main.startSpeed=.6f; main.startSize=size;
            main.startColor=color; main.maxParticles=120; main.simulationSpace=ParticleSystemSimulationSpace.Local;
            var emission=ps.emission; emission.rateOverTime=0f;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.angle=18f; shape.radius=.18f;
            ps.Play(); return ps;
        }
        public void SetRoomState(int i,float hp,bool on)
        {
            if(i<0 || i>=4) return;
            health[i]=Mathf.Clamp(hp,0f,100f); powered[i]=on;
            if(roomLights[i]!=null) roomLights[i].intensity=on && hp>0f ? 2.3f : .15f;
            if(steam[i]!=null) { var e=steam[i].emission; e.rateOverTime=hp<70f? (hp<=0f?26f:10f):0f; }
            if(sparks[i]!=null) { var e=sparks[i].emission; e.rateOverTime=on && hp<35f? (hp<=0f?30f:12f):0f; }
            if(hp<=0f && !destroyedFx[i]) { destroyedFx[i]=true; if(warnings[i]!=null) warnings[i].intensity=4f; }
            if(hp>0f) destroyedFx[i]=false;
            Refresh();
        }
        public void SetInteractive(bool value) { if(raycaster!=null) raycaster.enabled=value; }
        public void SetIncident(int incident, bool active) { activeIncident=incident; incidentActive=active; Refresh(); }
        private int AffectedRoom()
        {
            return activeIncident; // roomIndex is supplied by incident data
        }
        private void Refresh()
        {
            if(label==null) return;
            label.text=names[activeCamera] + "  •  LIVE";
            status.text="INTEGRITY " + Mathf.CeilToInt(health[activeCamera]) + "%  |  " + (health[activeCamera]<=0f?"ROOM DESTROYED":!powered[activeCamera]?"POWER OFF":incidentActive && AffectedRoom()==activeCamera?"ANOMALY DETECTED":"SIGNAL STABLE");
            status.color=health[activeCamera]<40f || !powered[activeCamera] ? new Color(1,.3f,.3f) : new Color(.4f,1,.7f);
        }
        public void SelectCamera(int index)
        {
            activeCamera=Mathf.Clamp(index,0,3);
            for(int i=0;i<4;i++) cameras[i].enabled=i==activeCamera;
            image.texture=feeds[activeCamera]; Refresh();
        }
        private void Update()
        {
            int room=AffectedRoom();
            for(int i=0;i<4;i++)
            {
                bool fault=(incidentActive && room==i) || health[i]<50f;
                if(warnings[i]!=null) warnings[i].intensity=fault && Mathf.Sin(Time.time*7f)>0 ? 3f : 0f;
                if(movingParts[i]!=null)
                {
                    if(i==2 && powered[i] && health[i]>0f && !fault) movingParts[i].Rotate(0,0,Time.deltaTime*150f,Space.Self);
                    if(i==0) movingParts[i].GetComponent<Renderer>().material.color=!powered[i] || health[i]<=0f ? new Color(.12f,.15f,.16f) : fault ? new Color(1f,.14f,.08f) : new Color(.05f,.8f,.85f);
                    if(i==1) movingParts[i].Rotate(0,(powered[i] && health[i]>0f ? (!fault?80f:8f) : 0f)*Time.deltaTime,0,Space.Self);
                }
            }
        }
        private GameObject Cube(Transform parent,string name,Vector3 pos,Vector3 scale,Color color)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name; obj.transform.SetParent(parent,false);
            obj.transform.localPosition=pos; obj.transform.localScale=scale;
            obj.GetComponent<Renderer>().material.color=color; return obj;
        }
        private T Make<T>(Transform parent,Vector2 min,Vector2 max) where T:Component
        {
            var go=new GameObject(typeof(T).Name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>(); rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
            return go.AddComponent<T>();
        }
        private void Panel(Transform p,Vector2 a,Vector2 b,Color c) { Make<Image>(p,a,b).color=c; }
        private Text Text(Transform p,Vector2 a,Vector2 b,int size,TextAnchor anchor)
        {
            var t=Make<Text>(p,a,b); t.font=font;t.fontSize=size;t.alignment=anchor;t.color=Color.white;
            t.horizontalOverflow=HorizontalWrapMode.Wrap; return t;
        }
        private void OnDestroy()
        {
            for(int i=0;i<feeds.Length;i++) if(feeds[i]!=null) { feeds[i].Release(); Destroy(feeds[i]); }
        }
    }
}
