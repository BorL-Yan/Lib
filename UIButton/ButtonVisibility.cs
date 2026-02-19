using UnityEngine;

namespace Lib.UI
{
    public class ButtonVisibility : MonoBehaviour
    {
        [field:SerializeField] public GameObject defaultPanel {get; private set; }
        [field:SerializeField]public GameObject selectedPanel { get; private set; }
        [field:SerializeField]public GameObject clickedPanel { get; private set; }
        
        [field:SerializeField]public float defoulteScale {get; private set; }
        [field:SerializeField]public float selectedScale {get; private set; }
        [field:SerializeField]public float clickedScale {get; private set; }

        public void State(GameObject obj)
        {
            defaultPanel.SetActive(obj == defaultPanel);
            selectedPanel.SetActive(obj == selectedPanel);
            clickedPanel.SetActive(obj == clickedPanel);
        }
    }
}