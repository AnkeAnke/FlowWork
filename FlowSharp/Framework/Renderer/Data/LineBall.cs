﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Device = SlimDX.Direct3D11.Device;
using Buffer = SlimDX.Direct3D11.Buffer;
using Debug = System.Diagnostics.Debug;
using SlimDX;
using SlimDX.D3DCompiler;
using System.IO;
using System.Runtime.InteropServices;


namespace FlowSharp
{
    class LineBall : Renderable
    {
        protected Vector3 _color;
        protected float _thickness;
        /// <summary>
        /// Set of lines in 3D space.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="scale">Scaling the field extent.</param>
        /// <param name="field"></param>
        public LineBall(Plane plane, LineSet lines)
        {
            _thickness = lines.Thickness * plane.PointSize;
            _color = lines.Color;
            this._vertexSizeBytes = Marshal.SizeOf(typeof(Vector4));
            this._numVertices = lines.NumPoints * 2 - lines.Lines.Length * 2; // Linelist means, all points are there twice, minus the endpoints.
            this._topology = PrimitiveTopology.LineList;

            // Setting up the vertex buffer. 
            GenerateGeometry(plane, lines);

            this._technique = _lineEffect.GetTechniqueByName("Render");
            this._vertexLayout = new InputLayout(_device, _technique.GetPassByIndex(0).Description.Signature, new[] {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0)
            });
        }

        /// <summary>
        /// Setting up the vertex buffer. Vertex size and number has to be known.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="scale"></param>
        protected void GenerateGeometry(Plane plane, LineSet lines)
        {
            // Write poition and UV-map data.
            var stream = new DataStream(_numVertices * _vertexSizeBytes, true, true);
            Vector3 zAxis = plane.ZAxis;
            for (int index = 0; index < lines.Lines.Length; ++index)
            {
                Line line = lines.GetWorldLine(index);
                Debug.Assert(line.Length == lines.Lines[index].Length);
                stream.Write(new Vector4(plane.Origin + (plane.XAxis * line.Positions[0][0] + plane.YAxis * line.Positions[0][1] + zAxis * line.Positions[0][2]), 1.0f));
                for (int point = 1; point < line.Positions.Length - 1; ++point)
                {
                    Vector4 pos = new Vector4(plane.Origin + (plane.XAxis * line.Positions[point][0] + plane.YAxis * line.Positions[point][1] + zAxis * line.Positions[point][2]), 1.0f);
                    stream.Write(pos);
                    stream.Write(pos);
                }
                int end = line.Positions.Length - 1;
                stream.Write(new Vector4(plane.Origin + (plane.XAxis * line.Positions[end][0] + plane.YAxis * line.Positions[end][1] + zAxis * line.Positions[end][2]), 1.0f));
            }
            stream.Position = 0;

            // Create and fill buffer.
            _vertices = new Buffer(_device, stream, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = _numVertices * _vertexSizeBytes,
                Usage = ResourceUsage.Default
            });
            stream.Dispose();
        }

        public override void Render(Device device)
        {
            _lineEffect.GetVariableByName("color").AsVector().Set(_color);
            _lineEffect.GetVariableByName("thickness").AsScalar().Set(_thickness);
            base.Render(device);
        }

        public override void Update(TimeSpan totalTime)
        {
        }

        /// <summary>
        /// Initialize the static components.
        /// </summary>
        /// <param name="device"></param>
        public static void Initialize()
        {
            try
            {
                var bytecode = ShaderBytecode.CompileFromFile("Framework/Renderer/Data/DataEffects/Lines.fx", "fx_5_0", ShaderFlags.None, EffectFlags.None);
                _lineEffect = new Effect(_device, bytecode);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


        }

        /// <summary>
        /// The effects that will be used by the lines.
        /// </summary>
        protected static Effect _lineEffect;

        ///// <summary>
        ///// Device for creating resources.
        ///// </summary>
        //protected static Device _device;
    }
}
