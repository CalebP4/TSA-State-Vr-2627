using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{

    /// <summary>
    /// Counts pushes into a surface, e.g. chest thrusts. The zone's position is the
    /// surface and its up axis points out of the body; a push counts when the hand goes
    /// at least <see cref="depth"/> below the surface and comes back up.
    /// </summary>
    public class CompressionZone : HandZone
    {
        public string actionId;
        public float depth = 0.03f;
        public float releaseDepth = 0.01f;
        public event Action Compressed;
        public event Action NotReady;
        public float CurrentDepth { get; private set; }

        bool down;

        void Update()
        {
            var h = FirstHand();
            if (h == null) { down = false; CurrentDepth = 0f; return; }
            CurrentDepth = Mathf.Max(0f, Vector3.Dot(transform.position - h.transform.position, transform.up));
            if (!down && CurrentDepth >= depth)
            {
                if (!Ready) { NotReady?.Invoke(); down = true; return; }
                down = true;
                h.Pulse(0.5f, 0.06f);
                Sfx.Play(Sound.Press, transform.position, 0.7f);
                Report(actionId);
                Compressed?.Invoke();
            }
            else if (down && CurrentDepth <= releaseDepth) down = false;
        }

        protected override void OnHandExit(HandProbe h) { down = false; }
    }
}

