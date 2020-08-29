using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScanResults : MonoBehaviour
{
    public GameObject scanResultPrefab;

    // Start is called before the first frame update
    void Start()
    {
        int idx = 0;
        foreach(var s in FindObjectsOfType<ScanEffect>())
        {
            var indicator = Instantiate(this.scanResultPrefab, this.transform);
            indicator.GetComponent<ScanBar>().target = s;
            var indicatorTransform = indicator.GetComponent<RectTransform>();
            indicatorTransform.localPosition = new Vector3(indicatorTransform.sizeDelta.x * idx, 0, 0);
            idx++;
        }
    }
}
