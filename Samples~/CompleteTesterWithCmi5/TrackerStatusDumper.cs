using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xasu;

public class TrackerStatusDumper : MonoBehaviour
{

    public UnityEngine.UI.Text text;


    // Update is called once per frame
    void Update()
    {
        text.text = "CONFIG: \n\n" + JsonConvert.SerializeObject(XasuTracker.Instance.TrackerConfig, Formatting.Indented)
            + "\n\n STATUS: \n\n" + JsonConvert.SerializeObject(XasuTracker.Instance.Status, Formatting.Indented)
            + "\n\n CLA: \n\n" + JsonConvert.SerializeObject(Environment.GetCommandLineArgs(), Formatting.Indented);
    }
}
