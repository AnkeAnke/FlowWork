﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SlimDX;
using System.Runtime.InteropServices;

namespace FlowSharp
{
    class TetGridMapper : DataMapper
    {
        //LineSet _wireframe;
        //PointSet<Point> _vertices;
        Mesh _cubes;
        //TetTreeGrid _grid;'
        GeneralUnstructurdGrid _geometry;
        //Index[] _indices;
        bool update = true;
        PointCloud _vertices;

        PointSet<Point> tmp;

        public TetGridMapper(Plane plane) : base()
        {
            Mapping = ShowSide;
            Plane = plane;

            LoaderVTU loader = new LoaderVTU(Aneurysm.GeometryPart.Wall);
            var hexGrid = loader.LoadGeometry();

            _geometry = loader.Grid;
            hexGrid = null;

            // TMP
            tmp = new PointSet<Point>(new Point[]
    {
                                new Point(new Vector3(-3, 0, -3)),
                                new Point(new Vector3(-6, -3, -6)),
                                new Point(new Vector3(-6, -3, 0))
    });
            // \TMP

            this.Plane = Plane.FitToPoints(Vector3.Zero, 10, tmp);
//            this.Plane = new Plane(Plane.Origin, Plane.XAxis, Plane.YAxis, Plane.ZAxis, 100);
            Plane.PointSize = 10.0f;


            //int[] selection = new int[_grid.Indices.Length / 100];
            //for (int s = 0; s < selection.Length; ++s)
            //    selection[s] = s*100;

            //try
            //{
            //    _indices = _grid.GetAllSides();
            //}
            //catch (Exception e)
            //{
            //    Console.Write(e);
            //    Debug.Assert(false);
            //}


            //Console.WriteLine("Num sides: {0}", _indices.Length);
        }

        public List<Renderable> ShowSide()
        {
            var wire = new List<Renderable>(5);
            if (update)
            {
                update = false;
                _cubes = new Mesh(Plane, _geometry);
            }
            if (_lastSetting == null ||
                WindowWidthChanged ||
                WindowStartChanged ||
                ColormapChanged)
            {
                _cubes.LowerBound = WindowStart;
                _cubes.UpperBound = WindowStart + WindowWidth;
                _cubes.UsedMap = Colormap;
            }
            wire.Add(_cubes);
            if (_vertices == null)
                _vertices = new PointCloud(Plane, tmp);
            //_vertices = new PointCloud(Plane, _geometry.GetVertices());
            wire.Add(_vertices);

            var axes = Plane.GenerateAxisGlyph();
            wire.AddRange(axes);
            return wire;

        }
        public override bool IsUsed(Setting.Element element)
        {
            switch (element)
            {
                case Setting.Element.Colormap:
                case Setting.Element.WindowStart:
                case Setting.Element.WindowWidth:
                    return true;
                default:
                    return false;
            }
        }
    }
}
