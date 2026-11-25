using UnityEngine;

namespace Protocol0317
{
    // Persistent station simulation, independent of the scripted incident UI.
    public sealed class StationSystems : MonoBehaviour
    {
        private readonly float[] hp = new float[4];
        private readonly bool[] powered = new bool[4];
        private CCTVSystem cctv;
        private int incidentRoom = -1;
        private bool active;
        private float updateClock;
        public int RepairRoom { get; private set; } = -1;
        public float TravelLeft { get; private set; }
        public void SendRepair(int i)
        {
            if (!active && incidentRoom < 0) return;
            if (i < 0 || i >= 4 || hp[i] >= 100f || RepairRoom == i) return;
            RepairRoom = i;
            TravelLeft = 5f;
        }
        public void CancelRepair() { RepairRoom = -1; TravelLeft = 0f; }
        private void RepairTick(float dt)
        {
            if (RepairRoom < 0) return;
            if (TravelLeft > 0f) { TravelLeft = Mathf.Max(0f, TravelLeft - dt); return; }
            int i = RepairRoom;
            hp[i] = Mathf.Min(100f, hp[i] + 3f * dt);
            if (hp[i] >= 100f) CancelRepair();
        }
        public bool IsCritical { get { for(int i=0;i<4;i++) if(hp[i]<=25f) return true; return false; } }
        public bool AllDestroyed { get { for(int i=0;i<4;i++) if(hp[i]>0f) return false; return true; } }
        public void Initialize(CCTVSystem system) { cctv=system; ResetStation(); }
        public void ResetStation()
        {
            for(int i=0;i<4;i++) { hp[i]=100f; powered[i]=true; }
            incidentRoom=-1; active=false; updateClock=0f; CancelRepair(); Sync();
        }
        public float Health(int i) { return hp[Mathf.Clamp(i,0,3)]; }
        public bool IsPowered(int i) { return powered[Mathf.Clamp(i,0,3)]; }
        public void TogglePower(int i)
        {
            if(i<0 || i>=4 || hp[i]<=0f) return;
            powered[i]=!powered[i]; Sync();
        }
        public void BeginIncident(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex > 3) return;
            incidentRoom=roomIndex;
            active=true; Sync();
        }
        public void ResolveIncident(bool success)
        {
            if(incidentRoom<0) return;
            hp[incidentRoom]=Mathf.Clamp(hp[incidentRoom]+(success?18f:-12f),0f,100f);
            active=!success; Sync();
        }
        public void Tick(float delta)
        {
            updateClock+=delta;
            if(updateClock<.25f) return;
            float dt=updateClock; updateClock=0f;
            if(active && incidentRoom>=0 && hp[incidentRoom]>0f)
                hp[incidentRoom]=Mathf.Max(0f,hp[incidentRoom]-(powered[incidentRoom]?.45f:.12f)*dt);
            // Pump failure / power cut causes reactor cooling loss.
            if((hp[1]<35f || !powered[1]) && hp[0]>0f) hp[0]=Mathf.Max(0f,hp[0]-.35f*dt);
            // Reactor collapse propagates into ventilation and the airlock.
            if(hp[0]<=0f)
            {
                if(hp[2]>0f) hp[2]=Mathf.Max(0f,hp[2]-.65f*dt);
                if(hp[3]>0f) hp[3]=Mathf.Max(0f,hp[3]-.45f*dt);
            }
            // Ventilation failure slowly damages pumps through heat accumulation.
            if((hp[2]<=0f || !powered[2]) && hp[1]>0f) hp[1]=Mathf.Max(0f,hp[1]-.2f*dt);
            for(int i=0;i<4;i++) if(hp[i]<=0f) powered[i]=false;
            RepairTick(dt);
            Sync();
        }
        private void Sync()
        {
            if(cctv!=null) for(int i=0;i<4;i++) cctv.SetRoomState(i,hp[i],powered[i]);
        }
    }
}
