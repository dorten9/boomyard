using System.Globalization;
using System.Text;
using BoomYard.Core;

// Prints the hand rig (Core/HandRig.cs) as JSON for preview.html.
static class Dump
{
    static string V(Vec3 v) { return string.Format(CultureInfo.InvariantCulture, "[{0},{1},{2}]", v.X, v.Y, v.Z); }

    static void Main()
    {
        var sb = new StringBuilder("{");
        foreach (var side in new[] { 1f, -1f })
        {
            sb.Append(side > 0 ? "\"right\":[" : ",\"left\":[");
            bool first = true;
            foreach (var p in HandRig.Fist(side))
            {
                if (!first) sb.Append(',');
                first = false;
                sb.AppendFormat(CultureInfo.InvariantCulture, "{{\"kind\":\"{0}\",\"name\":\"{1}\",\"a\":{2},\"b\":{3},\"size\":{4},\"d\":{5},\"yaw\":{6}}}",
                    p.Kind, p.Name, V(p.A), V(p.B), V(p.Size), p.Diameter, p.Yaw);
            }
            sb.Append(']');
        }
        sb.AppendFormat(",\"wrist\":{0},\"radius\":{1}}}", V(HandRig.WristTop(1f)), HandRig.Radius.ToString(CultureInfo.InvariantCulture));
        System.Console.WriteLine(sb.ToString());
    }
}
