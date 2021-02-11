// unset

using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class DockedOptions : MonoBehaviour
    {
        private GameObject[] options;
        
        private DockActive playerDocking;

        private void Start()
        {
            this.options = this.GetComponentsInChildren<Button>().Select(b => b.gameObject).ToArray();
            this.playerDocking = ComponentCache.FindObjectOfType<PlayerController>().GetComponentInChildren<DockActive>();
        }

        private void Update()
        {
            foreach (var option in this.options)
            {
                option.SetActive(this.playerDocking.docked);
            }
        }
    }
}