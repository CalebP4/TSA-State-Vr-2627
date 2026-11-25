using System.Collections;
using TMPro;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// A mobile phone. Pick it up and pull the trigger to call 911 on speaker. The
    /// dispatcher's lines come from the scenario script, then the phone can be put down.
    /// </summary>
    [RequireComponent(typeof(FirstAidItem))]
    public class PhoneItem : MonoBehaviour
    {
        public string actionId = "call_911";
        public TextMeshProUGUI screen;
        [TextArea] public string[] dispatcherLines =
        {
            "911, what is the address of your emergency?",
            "Help is on the way. Stay on the line on speaker.",
        };

        public bool Called { get; private set; }
        public bool Calling { get; private set; }
        public System.Action<string> Speak; // shows dispatcher lines in the world (set by scenario script)

        FirstAidItem item;

        void Awake()
        {
            item = GetComponent<FirstAidItem>();
            item.Used += _ => Dial();
            item.Grabbed += _ => { if (!Called && !Calling) SetScreen("<b>Pull TRIGGER</b>\nto call 911"); };
            SetScreen("<size=55%>12:04</size>\n<b>911</b>\n<size=55%>Emergency call</size>");
        }

        public void Dial()
        {
            if (Called || Calling) return;
            var c = ScenarioController.Active;
            if (c != null && c.Phase != ScenarioPhase.Active) return;
            StartCoroutine(CallRoutine());
        }

        IEnumerator CallRoutine()
        {
            Calling = true;
            SetScreen("Calling\n<b>911</b>...");
            Sfx.Play(Sound.Ring, transform.position, 0.7f);
            yield return new WaitForSeconds(1.6f);
            SetScreen("<color=#2FBF71>Connected</color>\n911 - Speaker");
            Called = true;
            Calling = false;
            ScenarioController.Active?.Report(actionId);
            foreach (var line in dispatcherLines)
            {
                Speak?.Invoke("Dispatcher: " + line);
                yield return new WaitForSeconds(3.2f);
            }
            SetScreen("<color=#2FBF71>On call</color>\nHelp is coming");
        }

        void SetScreen(string text)
        {
            if (screen != null) screen.text = text;
        }
    }
}
