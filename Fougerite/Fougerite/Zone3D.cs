using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fougerite
{
    /// <summary>
    /// Represents a 3D volumetric area defined by a 2D polygon base and a vertical height range.
    /// Includes optimizations such as Axis-Aligned Bounding Box (AABB) pre-checking.
    /// </summary>
    public class Zone3D
    {
        private List<Vector2> _points;
        private bool _protected;
        private bool _pvp;

        /// <summary>
        /// List of entities used to visually mark the corners of the zone in-game.
        /// </summary>
        private readonly List<Entity> _tmpPoints;

        // Bounding box coordinates for high-performance pre-checking
        private float _minX, _maxX, _minZ, _maxZ;

        /// <summary>
        /// Gets or sets the minimum elevation (altitude) of the zone.
        /// </summary>
        public float MinY { get; set; } = -1000f;

        /// <summary>
        /// Gets or sets the maximum elevation (altitude) of the zone.
        /// </summary>
        public float MaxY { get; set; } = 5000f;

        /// <summary>
        /// Initializes a new instance of the <see cref="Zone3D"/> class and registers it globally.
        /// </summary>
        /// <param name="name">The unique identifier for this zone.</param>
        public Zone3D(string name)
        {
            PVP = true;
            Protected = false;
            _tmpPoints = new List<Entity>();
            Points = new List<Vector2>();
        }

        /// <summary>
        /// Determines whether the specified <see cref="Entity"/> is inside the zone.
        /// </summary>
        /// <param name="en">The entity to check.</param>
        /// <returns>True if the entity's location is within the zone,otherwise, false.</returns>
        public bool Contains(Entity en)
        {
            if (en == null) 
                return false;
            return Contains(en.Location);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Player"/> is inside the zone.
        /// </summary>
        /// <param name="p">The player to check.</param>
        /// <returns>True if the player's location is within the zone,otherwise, false.</returns>
        public bool Contains(Player p)
        {
            if (p == null) 
                return false;
            return Contains(p.Location);
        }

        /// <summary>
        /// Determines whether a 3D position is within the zone boundaries.
        /// </summary>
        /// <remarks>
        /// This method performs checks in three stages:
        /// 1. Vertical Height (Y-axis) check.
        /// 2. AABB (Bounding Box) check for rapid discarding.
        /// 3. Ray-casting (Point-in-Polygon) algorithm for precise 2D boundaries.
        /// </remarks>
        /// <param name="v">The Vector3 position to check.</param>
        /// <returns>True if the position is inside the zone.</returns>
        public bool Contains(Vector3 v)
        {
            // Elevation Check
            if (v.y < MinY || v.y > MaxY) 
                return false;

            // Axis-Aligned Bounding Box.
            // Prevents expensive math if the point is clearly outside the polygon's reach.
            if (v.x < _minX || v.x > _maxX || v.z < _minZ || v.z > _maxZ) 
                return false;

            // Point-in-Polygon logic (Ray-casting algorithm)
            Vector2 vector = new Vector2(v.x, v.z);
            int num = Points.Count - 1;
            bool flag = false;
            int num2 = 0;

            while (num2 < Points.Count)
            {
                if ((((Points[num2].y <= vector.y) && (vector.y < Points[num].y)) ||
                     ((Points[num].y <= vector.y) && (vector.y < Points[num2].y))) &&
                    (vector.x < (Points[num].x - Points[num2].x) * (vector.y - Points[num2].y) /
                        (Points[num].y - Points[num2].y) + Points[num2].x))
                {
                    flag = !flag;
                }

                num = num2++;
            }

            return flag;
        }

        /// <summary>
        /// Destroys all visual markers spawned in the world for this zone.
        /// </summary>
        public void HideMarkers()
        {
            foreach (Entity entity in _tmpPoints)
            {
                if (entity != null && entity.Object != null)
                {
                    entity.Destroy();
                }
            }

            _tmpPoints.Clear();
        }

        /// <summary>
        /// Adds a corner point to the zone polygon using a <see cref="Vector2"/>.
        /// </summary>
        /// <param name="v">The 2D coordinates (X, Z).</param>
        public void Mark(Vector2 v)
        {
            Points.Add(v);
            UpdateBounds(v.x, v.y);
        }

        /// <summary>
        /// Adds a corner point to the zone polygon using raw coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Z coordinate (maps to Vector2.y).</param>
        public void Mark(float x, float y)
        {
            Vector2 v = new Vector2(x, y);
            Points.Add(v);
            UpdateBounds(x, y);
        }

        /// <summary>
        /// Recalculates the Axis-Aligned Bounding Box (AABB) whenever a new point is added.
        /// </summary>
        /// <param name="x">X coordinate of the new point.</param>
        /// <param name="z">Z coordinate of the new point.</param>
        private void UpdateBounds(float x, float z)
        {
            if (Points.Count == 1)
            {
                _minX = _maxX = x;
                _minZ = _maxZ = z;
            }
            else
            {
                if (x < _minX) _minX = x;
                if (x > _maxX) _maxX = x;
                if (z < _minZ) _minZ = z;
                if (z > _maxZ) _maxZ = z;
            }
        }

        /// <summary>
        /// Spawns physical pillars at each corner of the zone to make it visible to players.
        /// </summary>
        public void ShowMarkers()
        {
            HideMarkers();
            try
            {
                foreach (Vector2 vector in Points)
                {
                    float ground = World.GetWorld().GetGround(vector.x, vector.y);
                    Vector3 location = new Vector3(vector.x, ground, vector.y);
                    // Spawn a metal pillar to represent the corner
                    Entity item = World.GetWorld().SpawnEntity(";struct_metal_pillar", location);
                    if (item != null) 
                        _tmpPoints.Add(item);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Zone3D ShowMarkers Error: {e}");
            }
        }

        /// <summary>
        /// Gets a list of all entities currently existing in the world.
        /// </summary>
        public List<Entity> Entities
        {
            get { return World.GetWorld().Entities; }
        }

        /// <summary>
        /// Gets or sets the list of vertices (2D points) that form the zone floor.
        /// </summary>
        public List<Vector2> Points
        {
            get { return _points; }
            set { _points = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the zone is protected from damage or building.
        /// Handle this in your own HurtEvent hook.
        /// </summary>
        public bool Protected
        {
            get { return _protected; }
            set { _protected = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether PvP (Player vs Player) is allowed in this zone.
        /// Handle this in your own HurtEvent hook.
        /// </summary>
        public bool PVP
        {
            get { return _pvp; }
            set { _pvp = value; }
        }
    }
}