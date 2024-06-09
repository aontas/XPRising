using Unity.Mathematics;

namespace XPRising.Models;

// This is basically a float3, but more serialisable
public struct WaypointData(float x, float y, float z)
{
    public float x = x;
    public float y = y;
    public float z = z;

    public WaypointData(float3 location) : this(location.x, location.y, location.z) {}

    public float3 ToFloat3()
    {
        return new float3(x, y, z);
    }
}