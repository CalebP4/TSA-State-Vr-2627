using System.Collections;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// A pair of disposable gloves. Grab them and hold for a moment to put them on:
    /// both controllers turn glove-blue and the pack disappears.
    /// </summary>
    [RequireComponent(typeof(FirstAidItem))]
    public class GloveBox : MonoBehaviour
    {
        public string actionId = "gloves";
        public float holdSeconds = 0.7f;
        public bool Worn { get; private set; }

        FirstAidItem item;
        Coroutine routine;

        void Awake()
        {
            item = GetComponent<FirstAidItem>();
            item.Grabbed += _ => { if (routine == null && !Worn) routine = StartCoroutine(PutOn()); };
            item.Released += _ => { if (routine != null) { StopCoroutine(routine); routine = null; } };
        }

        IEnumerator PutOn()
        {
            var c = ScenarioController.Active;
            while (c != null && c.Phase != ScenarioPhase.Active) yield return null;
            yield return new WaitForSeconds(holdSeconds);
            Wear();
        }

        public void Wear()
        {
            if (Worn) return;
            Worn = true;
            routine = null;
            foreach (var h in HandProbe.All) h.SetGloved(true);
            Sfx.Play(Sound.Whoosh, transform.position, 0.6f);
            ScenarioController.Active?.Report(actionId);
            item.ForceRelease();
            gameObject.SetActive(false);
        }
    }
}
