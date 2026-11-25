using System;

namespace Protocol0317
{
    public sealed class GameState
    {
        public int PowerCapacity = 4;
        public int PowerLight = 1;
        public int PowerVent = 1;
        public int PowerTerminal = 1;
        public int Integrity = 70;
        public int Score;
        public int Solved;
        public int Stage;
        public bool Finished;
        public bool EndingGood;
        public int UsedPower => PowerLight + PowerVent + PowerTerminal;

        public bool TrySetPower(int subsystem, int value)
        {
            if (Finished || value < 0 || value > 3) return false;
            int old = subsystem == 0 ? PowerLight : subsystem == 1 ? PowerVent : subsystem == 2 ? PowerTerminal : -1;
            if (old < 0 || UsedPower - old + value > PowerCapacity) return false;
            if (subsystem == 0) PowerLight = value;
            else if (subsystem == 1) PowerVent = value;
            else PowerTerminal = value;
            return true;
        }

        public bool Meets(Incident incident)
        {
            if (incident == null || Finished) return false;
            if (UsedPower < incident.requiredPower) return false;
            if (incident.requiresLight && PowerLight < 1) return false;
            if (incident.requiresVent && PowerVent < 2) return false;
            if (incident.requiresTerminal && PowerTerminal < 1) return false;
            return true;
        }

        public void Resolve(Incident incident)
        {
            if (Finished || incident == null) return;
            if (Meets(incident)) { Score += incident.reward; Integrity = Math.Min(100, Integrity + 5); Solved++; }
            else Integrity = Math.Max(0, Integrity - incident.penalty);
            Stage++;
            if (Integrity <= 0) { Finished = true; EndingGood = false; }
        }
    }
}
