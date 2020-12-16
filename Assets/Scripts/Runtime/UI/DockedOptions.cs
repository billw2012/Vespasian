// unset

using System;
using UnityEngine;

namespace UI
{
    public class DockedOptions : MonoBehaviour
    {
        public GameObject[] options;
        
        private DockActive playerDocking;

        private void Start()
        {
            this.playerDocking = FindObjectOfType<PlayerController>().GetComponentInChildren<DockActive>();
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