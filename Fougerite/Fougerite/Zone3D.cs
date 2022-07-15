using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fougerite
{
    public class Zone3D
    {
        private List<Vector2> _points;
        private bool _protected;
        private bool _pvp;
        private List<Entity> tmpPoints;

        public Zone3D(string name)
        {
            this.PVP = true;
            this.Protected = false;
            this.tmpPoints = new List<Entity>();
            this.Points = new List<Vector2>();
            Dictionary<string, Zone3D> zones = World.GetWorld().zones;
            if (!zones.ContainsKey(name))
                zones.Add(name, this);
        }

        public bool Contains(Entity en)
        {
            return this.Contains(en.Location);
        }

        public bool Contains(Fougerite.Player p)
        {
            return this.Contains(p.Location);
        }

        public bool Contains(Vector3 v)
        {
            Vector2 vector = new Vector2(v.x, v.z);
            int num = this.Points.Count - 1;
            bool flag = false;
            int num2 = 0;
            while (num2 < this.Points.Count)
            {
                if ((((this.Points[num2].y <= vector.y) && (vector.y < this.Points[num].y)) || ((this.Points[num].y <= vector.y) && (vector.y < this.Points[num2].y))) && (vector.x < ((((this.Points[num].x - this.Points[num2].x) * (vector.y - this.Points[num2].y)) / (this.Points[num].y - this.Points[num2].y)) + this.Points[num2].x)))
                {
                    flag = !flag;
                }
                num = num2++;
            }
            return flag;
        }

        public static Zone3D Get(string name)
        {
            return World.GetWorld().zones[name] as Zone3D;
        }

        public static Zone3D GlobalContains(Entity e)
        {
            Dictionary<string, Zone3D> zones = World.GetWorld().zones;
            foreach (Zone3D zone in zones.Values)
            {
                if (zone.Contains(e))
                {
                    return zone;
                }
            }
            return null;
        }

        public static Zone3D GlobalContains(Fougerite.Player p)
        {
            Dictionary<string, Zone3D> zones = World.GetWorld().zones;
            foreach (Zone3D zone in zones.Values)
            {
                if (zone.Contains(p))
                {
                    return zone;
                }
            }
            return null;
        }

        public void HideMarkers()
        {
            foreach (Entity entity in this.tmpPoints)
            {
                Util.GetUtil().DestroyObject((entity.Object as StructureComponent).gameObject);
            }
            this.tmpPoints.Clear();
        }

        public void Mark(Vector2 v)
        {
            this.Points.Add(v);
        }

        public void Mark(float x, float y)
        {
            this.Points.Add(new Vector2(x, y));
        }

        public void ShowMarkers()
        {
            this.HideMarkers();
            try
            {
                foreach (Vector2 vector in this.Points)
                {
                    float ground = World.GetWorld().GetGround(vector.x, vector.y);
                    Vector3 location = new Vector3(vector.x, ground, vector.y);
                    object o = World.GetWorld().Spawn(";struct_metal_pillar", location);
                    Entity item = new Entity(o);
                    this.tmpPoints.Add(item);
                }
            }
            catch (Exception e)
            {
                Logger.LogDebug(e.ToString());
            }
        }

        public List<Entity> Entities
        {
            get
            {
                return World.GetWorld().Entities;
            }
        }

        public List<Vector2> Points
        {
            get
            {
                return this._points;
            }
            set
            {
                this._points = value;
            }
        }

        public bool Protected
        {
            get
            {
                return this._protected;
            }
            set
            {
                this._protected = value;
            }
        }

        public bool PVP
        {
            get
            {
                return this._pvp;
            }
            set
            {
                this._pvp = value;
            }
        }
    }
}