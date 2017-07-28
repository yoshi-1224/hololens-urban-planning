using HoloToolkit.Unity;
using Mapbox.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// this class holds information for each of the buildings. Maybe in the future this can be replaced
/// by a web server or something.
/// Also make this hold ALL the data such as map scale etc
/// </summary>
public class TableDataHolder : Singleton<TableDataHolder> {
    public class TableData {
        public string building_name;
        public string building_class;
        public float GPR;
        public float measured_height;
        public int storeys_above_ground;
        public TableData(string name, string _class, float GPR, float measured_height, int storeys) {
            building_name = name;
            building_class = _class;
            this.GPR = GPR;
            this.measured_height = measured_height;
            storeys_above_ground = storeys;
        }
    }

    private string[] classes = { "mixed-use development", "civic & community",
                                "business, trade"};

    public Dictionary<string, TableData> dataDict { get; set; }

    protected override void Awake() {
        base.Awake();
        dataDict = new Dictionary<string, TableData>();
        // initialize the hard-coded dictionary
        dataDict["AXA_tower"] = new TableData("AXA Tower", classes[0], 8.71f, 251.8f, 84);
        dataDict["V_On_shenton"] = new TableData("V On Shenton", classes[0], 12.8f, 2.5f, 1);
        dataDict["Capital_tower"] = new TableData("Capital Tower", classes[2], 13.41f, 247.9f, 83);
        dataDict["Robinson_77"] = new TableData("Robinson 77", classes[2], 11.41f, 181.9f, 61);
        dataDict["SBF_centre"] = new TableData("SBF Centre", classes[0], 12.31f, 98.2f, 33);
        dataDict["Asia_square"] = new TableData("Asia Square", classes[2], 13.9f, 222.9f, 74);
        dataDict["SCCC_S1_Parent(Clone)"] = new TableData("Chinese Culture Centre", classes[1], 2.76f, 0f, 1);
        dataDict["SCCC_S2_Parent(Clone)"] = new TableData("Chinese Culture Centre", classes[1], 2.76f, 0f, 2);
        dataDict["SCCC_S3_Parent(Clone)"] = new TableData("Chinese Culture Centre", classes[1], 2.76f, 0f, 3);
        dataDict["Singapore_conference_hall"] = new TableData("Singapore Conference Hall", classes[2], 0f, 29.88f, 10);
        dataDict["OUE_downtown"] = new TableData("OUE Downtown", classes[2], 13.88f, 193.56f, 65);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
    }

}
