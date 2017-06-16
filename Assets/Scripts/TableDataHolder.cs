using HoloToolkit.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// this class holds information for each of the buildings. Maybe in the future this can be replaced
/// by a web server or something.
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

    public string[] classes = { "mixed-use development", "civic & community insitution",
                                "business, trade"};

    public Dictionary<string, TableData> dataDict;

    public void Start() {
        dataDict = new Dictionary<string, TableData>();
        // initialize the hard-coded dictionary
        dataDict["AXA_tower"] = new TableData("AXA Tower", classes[0], 8.71f, 251.8f, 84);
        dataDict["V_On_shenton"] = new TableData("V On Shenton", classes[0], 12.8f, 2.5f, 1);
        dataDict["Capital_tower"] = new TableData("Capital Tower", classes[2], 13.41f, 247.9f, 83);
        dataDict["Robinson_77"] = new TableData("Robinson 77", classes[2], 11.41f, 181.9f, 61);
        dataDict["SBF_centre"] = new TableData("SBF Centre", classes[0], 12.31f, 98.2f, 33);
        dataDict["Asia_square"] = new TableData("Asia Square", classes[2], 13.9f, 222.9f, 74);

    }
}
