﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using System.Diagnostics;

namespace FlowSharp
{
    class Point
    {
        public SlimDX.Vector4 Position;
        public virtual SlimDX.Vector3 Color { get; set; } = SlimDX.Vector3.UnitY;
        public virtual float Radius { get; set; } = 0.1f;
        public Point() { }
        public Point(Vector4 pos) { Position = pos; }
        public Point(Vector3 pos) { Position = new Vector4(pos, 0); }
        public static explicit operator Point(Vector4 pos)
        {
            return new Point(pos);
        }
        public static explicit operator Vector4(Point pos)
        {
            return pos.Position;
        }

        public virtual Vector ToVector()
        {
            return new Vector(Position);
        }
    }

    //class InertialPoint : Point
    //{
    //    public float LengthLine = 0;
    //    public VectorField.Integrator.Status Status = VectorField.Integrator.Status.OK;
    //    //public Int2 Origin = Int2.ZERO;
    //}

    class DirectionPoint : Point
    {
        //public float LengthLine = 0;
        public VectorField.Integrator.Status Status = VectorField.Integrator.Status.OK;
        public Vector3 Direction;
        public DirectionPoint(Vector4 pos, Vector3 inertia)
        {
            Position = pos;
            Direction = inertia;
        }

        public DirectionPoint() { }

        public override Vector ToVector()
        {
            Vector vec = new Vector(7);
            for (int n = 0; n < 3; ++n)
            {
                vec[n] = Position[n];
                vec[3 + n] = Direction[n];
            }
            vec.T = Position.W;

            return vec;
        }
    }

    class CriticalPoint2D : Point
    {
        public enum TypeCP
        {
            SADDLE = 0,
            ATTRACTING_NODE,
            ATTRACTING_FOCUS,
            REPELLING_NODE,
            REPELLING_FOCUS
        }

        public enum ComplexDirection
        {
            POSITIVE,
            NEGATIVE,
            POSITIVE_COMPLEX,
            NEGATIVE_COMPLEX
        }
        public SquareMatrix Eigenvectors;
        public ComplexDirection[] Eigenvalues;
        public TypeCP Type;
        public override Vector3 Color
        {
            get
            {
                return _typeColorList[(int)Type];
            }

            set
            {
                base.Color = value;
            }
        }

        private static Vector3[] _typeColorList = new Vector3[] 
        {
            new Vector3(0.8f, 0.8f, 0.01f), // Yellow
            new Vector3(0.01f, 0.1f, 0.8f), // Blue
            new Vector3(0.4f, 0.5f, 1.0f), // LightBlue
            new Vector3(0.8f, 0.01f, 0.1f), // Red
            new Vector3(1.0f, 0.4f, 0.5f), // LightRed
            //new Vector3(0.3f, 0.01f, 0.8f), // Purple
            //new Vector3(0.01f, 0.8f, 0.1f)  // Green
        };

        public CriticalPoint2D(Vector4 position, SquareMatrix eigenvectors, ComplexDirection[] eigenvalues)
        {
            Position = position;
            Eigenvalues = eigenvalues;
            Eigenvectors = eigenvectors;
            SetType();
        }

        public CriticalPoint2D(Vector4 position, SquareMatrix J)
        {
            Position = position;
            Debug.Assert(J.Length == 2);
            Eigenvectors = new SquareMatrix(2);
            Eigenvalues = new ComplexDirection[2];

            float a = J[0][0]; float b = J[1][0]; float c = J[0][1]; float d = J[1][1];
            // Computing eigenvalues.
            float Th = (a - d) * 0.5f;
            float D = a * d - b * c;
            float root = Th * Th - D;
            bool complex = false;
            if (root < 0)
            {
                complex = true;
                root = 0;
            }
            else
                complex = false;
            root = (float)Math.Sqrt(root);
            float l0 = Th + root;
            float l1 = Th - root;

            // Save directional information.
            if(l0 >= 0)
                Eigenvalues[0] = complex ? ComplexDirection.POSITIVE_COMPLEX : ComplexDirection.POSITIVE;
            else
                Eigenvalues[0] = complex ? ComplexDirection.NEGATIVE_COMPLEX : ComplexDirection.NEGATIVE;

            if (l1 >= 0)
                Eigenvalues[1] = complex ? ComplexDirection.POSITIVE_COMPLEX : ComplexDirection.POSITIVE;
            else
                Eigenvalues[1] = complex ? ComplexDirection.NEGATIVE_COMPLEX : ComplexDirection.NEGATIVE;

            // Computing eigenvectors.
            if(c != 0)
            {
                Eigenvectors[0] = new Vec2(l0 - d, c);
                Eigenvectors[1] = new Vec2(l1 - d, c);
            }
            else if(b!= 0)
            {
                Eigenvectors[0] = new Vec2(b, l0 - a);
                Eigenvectors[1] = new Vec2(b, l1 - a);
            }
            else
            {
                Eigenvectors[0] = new Vec2(1, 0);
                Eigenvectors[1] = new Vec2(0, 1);
            }

            // Derive the critical points type now.
            SetType();
        }

        /// <summary>
        /// Derives the critical point type from the eigenvectors and values.
        /// </summary>
        private void SetType()
        {
            Debug.Assert(Eigenvectors.Length == Eigenvalues.Length && (Eigenvalues.Length == 2));

            // Going through the eigenvalues and determining the critical points type.
            int numPos = 0;
            bool complex = false;
            foreach (ComplexDirection dir in Eigenvalues)
            {
                if (dir == ComplexDirection.POSITIVE || dir == ComplexDirection.POSITIVE_COMPLEX)
                    numPos++;
                if (dir == ComplexDirection.NEGATIVE_COMPLEX || dir == ComplexDirection.POSITIVE_COMPLEX)
                    complex = true;
            }

            if (numPos == 2)
            {
                if (complex)
                    Type = TypeCP.REPELLING_FOCUS;
                else
                    Type = TypeCP.REPELLING_NODE;
            }
            else if (numPos == 0)
            {
                if (complex)
                    Type = TypeCP.ATTRACTING_FOCUS;
                else
                    Type = TypeCP.ATTRACTING_NODE;
            }
            else
                Type = TypeCP.SADDLE;
        }
    }

    class FloatCP2D : CriticalPoint2D
    {
        public float Value;
        public FloatCP2D(Vector4 position, SquareMatrix J, float value) : base(position, J)
        {
            Value = value;
        }

        public FloatCP2D(CriticalPoint2D cp, float value) : base(cp.Position, cp.Eigenvectors, cp.Eigenvalues)
        {
            Value = value;
        }
    }
    /// <summary>
    /// Object containing multiple points.
    /// </summary>
    class PointSet<P> where P : Point
    {
        public P[] Points;
        public int Length { get { return Points.Length; } }

        private Vector4? _minPosition, _maxPosition;
        public Vector4 MinPosition
        {
            get
            {
                if (_minPosition == null)
                    ExtractMinMax();
                return (Vector4)_minPosition;
            }
        }
        public Vector4 MaxPosition
        {
            get
            {
                if (_maxPosition == null)
                    ExtractMinMax();
                return (Vector4)_maxPosition;
            }
        }
        public PointSet(P[] points)
        {
            Points = points;
        }

        public PointSet<Point> ToBasicSet()
        {
            return new PointSet<Point>(Points);
        }

        public P this[int index]
        {
            get { return Points[index]; }
            set { Points[index] = value; }
        }

        public void SetTime(float time)
        {
            foreach (P p in Points)
                p.Position.W = time;
        }

        public void ExtractMinMax()
        {
            if (_minPosition != null && _maxPosition != null)
                return;

            Vector4 minPos = Points[0].Position;
            Vector4 maxPos = Points[0].Position;

            // Find min and max in each dimension.
            foreach (P p in Points)
            {
                for (int v = 0; v < 3; ++v)
                {
                    minPos[v] = Math.Min(minPos[v], p.Position[v]);
                    maxPos[v] = Math.Max(maxPos[v], p.Position[v]);
                }
            }

            _minPosition = minPos;
            _maxPosition = maxPos;
        }

        public void RandomizeTimes(float timeStart, float timeEnd)
        {
            Random rand = new Random();
            float range = timeEnd - timeStart;
            for (int p = 0; p < Length; ++p)
                Points[p].Position.W = (float)(rand.NextDouble() * range + timeStart);
        }
    }

    class CriticalPointSet2D : PointSet<CriticalPoint2D>
    {
        public CriticalPointSet2D(CriticalPoint2D[] points) : base(points)
        { }

        public CriticalPointSet2D SelectTypes(CriticalPoint2D.TypeCP[] selection)
        {
            // Save selected types in a faster to query structure.
            Index selectedTypes = new Index(0, Enum.GetNames(typeof(CriticalPoint2D.TypeCP)).Length);
            foreach (CriticalPoint2D.TypeCP type in selection)
                selectedTypes[(int)type] = 1;

            // Take subset.
            List<CriticalPoint2D> cpList = new List<CriticalPoint2D>(Points.Length);
            foreach(CriticalPoint2D cp in Points)
            {
                if (selectedTypes[(int)cp.Type] == 1)
                    cpList.Add(cp);
            }

            // Return subset.
            return new CriticalPointSet2D(cpList.ToArray());
        }
    }
}
