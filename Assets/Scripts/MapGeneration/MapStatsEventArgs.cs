using System;
using System.Collections;
using System.Collections.Generic;

public class MapStatsEventArgs : EventArgs {
    public string CenterLatitudeLongitudeString { get; set; }
    public string MapTheme { get; set; }
    public float MapZoom { get; set; }

}
