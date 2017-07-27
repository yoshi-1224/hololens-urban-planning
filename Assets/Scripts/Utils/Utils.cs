using Mapbox.Utils;
using System.Text;

public static class Utils {

    public static string RenderBold(string str) {
        return "<b>" + str + "</b>";
    }

    public static string FormatLatLong(Vector2d coordinates) {
        string lat = RenderBold("Lat: ") + string.Format(" {0:0.000}", coordinates.x);
        string lng = RenderBold("Long: ") + string.Format(" {0:0.000}", coordinates.y);
        return lat + " " + lng;
    }

    public static string ChangeTextSize(string str, int size) {
        return "<size=" + size + ">" + str + "</size>";
    }

    public static string FormatNumberInDecimalPlace(double number, int decimalPlaces) {
        StringBuilder builder = new StringBuilder("{0:0");
        if (decimalPlaces != 0)
            builder.Append('.');

        builder.Append('0', decimalPlaces);
        builder.Append('}');

        return string.Format(builder.ToString(), number);
    }
}
