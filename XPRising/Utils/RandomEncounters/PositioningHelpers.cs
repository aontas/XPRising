using System;
using ProjectM.Tiles;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;

namespace XPRising.Utils.RandomEncounters
{
    // Credit: https://github.com/AviiNL/vrising-troublemaker
    internal class PositioningHelpers
    {
        private static readonly Random _random = new ();
        private static TileMapCollisionMath.MapData? _mapDataValue;
        private static TileMapCollisionMath.MapData MapData
        {
            get
            {
                if (_mapDataValue == null)
                {
                    TileWorld tileWorld = new TileWorld();
                    _mapDataValue = TileCollisionHelper.CreateMapData(TileCollisionHelper.CreateLinePolygon(), tileWorld);
                }
                return  (TileMapCollisionMath.MapData) _mapDataValue;
            }
        }

        /*internal static float3 GetSpawnPosition(float3 playerPosition, int minDistance, int maxDistance)
        {
            var distance = _random.Next(minDistance, maxDistance);

            var angle = _random.NextDouble() * Math.PI * 2;
            var x = (float)Math.Cos(angle) * distance;
            var y = (float)Math.Sin(angle) * distance;

            var position = new float2(playerPosition.x + x, playerPosition.z + y);

            if (!IsWalkable(position))
            {
                position = OffsetToWalkable(position);
            }

            return new float3(position.x, playerPosition.y, position.y);
        }*/

        internal static bool IsWalkable(float2 pos)
        {
            return IsWalkable(pos, 1, 1, MapData);
        }

        /// <summary>
        /// Checks a circle area with a specified radius to to if there is any collision for normal movement
        /// </summary>
        /// <param name="pos">The position that is being checked</param>
        /// <param name="radius">The radius used for the check</param>
        /// <returns>True if the position is valid for normal movement</returns>
        internal static bool IsWalkable(float2 pos, int radius, int heightLevel, TileMapCollisionMath.MapData _mapData)
        {
            return !TileMapCollisionMath.CheckStaticCircle(mapData: ref _mapData, startPosition: pos, heightLevel: (byte) heightLevel, radius: radius, collidesAgainstFlags: MapCollisionFlags.CollideNormalMovement);
        }

        internal static float2 OffsetToWalkable(float2 pos)
        {
            return OffsetToWalkable(pos, 1, 12, 10);
        }

        /// <summary>
        /// Performs multiple checks around the position to find a valid normal movement position
        /// </summary>
        /// <param name="pos">The position that is being checked</param>
        /// <param name="distance">The distance checked in each loop of the checks</param>
        /// <param name="steps">The number of checks to perform in each loop</param>
        /// <param name="distanceChecks">The number of times to icrement the distance and check around the position again</param>
        /// <returns></returns>
        internal static float2 OffsetToWalkable(float2 pos, int distance, int steps, int distanceChecks)
        {
            var stepAngle = Mathf.PI * 2f / steps;

            for (var d = 1; d < distanceChecks + 1; d++)
            {
                for (var r = 0f; r < Mathf.PI * 2; r += stepAngle)
                {
                    var newPos = pos + new float2(Mathf.Cos(r), Mathf.Sin(r)) * distance * (int)Math.Pow(2, d);
                    if (IsWalkable(newPos))
                        return newPos;
                }
            }

            return pos;
        }
    }
}
