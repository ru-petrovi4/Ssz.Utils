using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;
using static Ssz.Utils.Avalonia.Model3D.Model3DControl;

namespace Ssz.Utils.Avalonia.Model3D;

internal class OpenGlContent
{
    #region public functions

    public string Info { get; private set; } = string.Empty;

    public unsafe void Init(GlInterface gl, GlVersion glVersion)
    {
        _glVersion = glVersion;
        CheckError(gl);

        Info = $"Renderer: {gl.GetString(GL_RENDERER)} Version: {gl.GetString(GL_VERSION)}";

        _shader = new Shader(gl, VertexShaderSource, FragmentShaderSource, programHandle =>
        {
            gl.BindAttribLocationString(programHandle, 0, "aPosition");
            gl.BindAttribLocationString(programHandle, 1, "aColor");
        });
        CheckError(gl);

        _vertexBufferObject = new BufferObject<float>(gl, GL_ARRAY_BUFFER);
        CheckError(gl);

        _vertexArrayObject = new VertexArrayObject<float>(gl);
        CheckError(gl);

        _vertexArrayObject.VertexAttributePointer(0, 3, GL_FLOAT, 7, 0); // aPosition
        _vertexArrayObject.VertexAttributePointer(1, 4, GL_FLOAT, 7, 3); // aColor
        gl.EnableVertexAttribArray(0);
        gl.EnableVertexAttribArray(1);
        CheckError(gl);

        _linesVertexBufferObject = new BufferObject<float>(gl, GL_ARRAY_BUFFER);
        CheckError(gl);

        _linesVertexArrayObject = new VertexArrayObject<float>(gl);
        CheckError(gl);

        _linesVertexArrayObject.VertexAttributePointer(0, 3, GL_FLOAT, 7, 0); // aPosition
        _linesVertexArrayObject.VertexAttributePointer(1, 4, GL_FLOAT, 7, 3); // aColor
        gl.EnableVertexAttribArray(0);
        gl.EnableVertexAttribArray(1);
        CheckError(gl);
    }

    public void Deinit(GlInterface gl)
    {
        gl.BindBuffer(GL_ARRAY_BUFFER, 0);
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
        gl.BindVertexArray(0);
        gl.UseProgram(0);

        _vertexBufferObject?.Dispose();
        _vertexArrayObject?.Dispose();

        _linesVertexBufferObject?.Dispose();
        _linesVertexArrayObject?.Dispose();

        _shader?.Dispose();
    }

    public unsafe void OnOpenGlRender(
        GlInterface gl,
        int fb,
        PixelSize size,
        Model3DMessage model3DMessage)
    {
        gl.Viewport(0, 0, size.Width, size.Height);
        gl.ClearDepth((float)1);
        gl.Disable(GL_CULL_FACE);
        gl.Disable(GL_SCISSOR_TEST);
        gl.DepthFunc(GL_LESS);
        gl.DepthMask(1);

        gl.ClearColor((float)0, (float)0, (float)0, (float)0);
        gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        gl.Enable(GL_DEPTH_TEST);

        if (model3DMessage.Model3DScene is not null)
        {
            var points = model3DMessage.Model3DScene.Points;
            if (points is not null)
            {
                _pointsBuffer.Clear();
                _pointsBuffer.AddRangeOfDefault(points.Count * 7);
                Span<float> vertexData = _pointsBuffer.Items;
                for (int i = 0; i < points.Count; i += 1)
                {
                    int offset = i * 7;
                    vertexData[offset] = points[i].Position.X;
                    vertexData[offset + 1] = points[i].Position.Y;
                    vertexData[offset + 2] = points[i].Position.Z;
                    vertexData[offset + 3] = points[i].Color.X;
                    vertexData[offset + 4] = points[i].Color.Y;
                    vertexData[offset + 5] = points[i].Color.Z;
                    vertexData[offset + 6] = points[i].Color.W;
                }
                _vertexBufferObject!.Bind();
                _vertexBufferObject!.BufferData(vertexData);
            }

            var lines = model3DMessage.Model3DScene.Lines;
            if (lines is not null)
            {
                _linesBuffer.Clear();
                int totalLineVertices = 0;

                foreach (var polyline in lines)
                {
                    if (polyline is null || polyline.Count < 2) continue;
                    totalLineVertices += (polyline.Count - 1) * 2;
                }

                _linesBuffer.AddRangeOfDefault(totalLineVertices * 7);
                Span<float> lineData = _linesBuffer.Items;
                int offset = 0;

                foreach (var polyline in lines)
                {
                    if (polyline is null || polyline.Count < 2) continue;
                    for (int i = 0; i < polyline.Count - 1; i++)
                    {
                        var p1 = polyline[i];
                        var p2 = polyline[i + 1];

                        lineData[offset++] = p1.Position.X;
                        lineData[offset++] = p1.Position.Y;
                        lineData[offset++] = p1.Position.Z;
                        lineData[offset++] = p1.Color.X;
                        lineData[offset++] = p1.Color.Y;
                        lineData[offset++] = p1.Color.Z;
                        lineData[offset++] = p1.Color.W;

                        lineData[offset++] = p2.Position.X;
                        lineData[offset++] = p2.Position.Y;
                        lineData[offset++] = p2.Position.Z;
                        lineData[offset++] = p1.Color.X;
                        lineData[offset++] = p1.Color.Y;
                        lineData[offset++] = p1.Color.Z;
                        lineData[offset++] = p1.Color.W;
                    }
                }
                _linesVertexBufferObject!.Bind();
                _linesVertexBufferObject!.BufferData(lineData);
            }
        }

        _shader!.Use();
        CheckError(gl);        

        // Камера как в Helix-подобной orbit navigation:
        // eye = target - forward * distance
        var forward = new Vector3(
            MathF.Cos(model3DMessage.Pitch) * MathF.Cos(model3DMessage.Yaw),
            MathF.Sin(model3DMessage.Pitch),
            MathF.Cos(model3DMessage.Pitch) * MathF.Sin(model3DMessage.Yaw));

        forward = Vector3.Normalize(forward);

        var eye = model3DMessage.Target - forward * model3DMessage.Distance;

        var model = Matrix4x4.Identity;
        var view = Matrix4x4.CreateLookAt(eye, model3DMessage.Target, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4,
            (float)size.Width / (float)size.Height,
            0.01f,
            5000.0f);

        unsafe
        {
            gl.UniformMatrix4fv(gl.GetUniformLocationString(_shader.ProgramHandle, "model"), 1, false, &model);
            gl.UniformMatrix4fv(gl.GetUniformLocationString(_shader.ProgramHandle, "view"), 1, false, &view);
            gl.UniformMatrix4fv(gl.GetUniformLocationString(_shader.ProgramHandle, "projection"), 1, false, &projection);
        }

        if (_pointsBuffer.Count > 0)
        {
            _vertexBufferObject!.Bind();
            _vertexArrayObject!.Bind();
            const int GL_POINTS = 0;
            gl.DrawArrays(GL_POINTS, 0, _pointsBuffer.Count / 7);
            CheckError(gl);
        }

        if (_linesBuffer.Count > 0)
        {
            _linesVertexBufferObject!.Bind();
            _linesVertexArrayObject!.Bind();
            const int GL_LINES = 1;
            gl.DrawArrays(GL_LINES, 0, _linesBuffer.Count / 7);
            CheckError(gl);
        }
    }

    #endregion

    private string GetShader(bool fragment, string shader)
    {
        return shader;
    }

    private string VertexShaderSource => GetShader(false, """
        #version 100
        precision mediump float;
        attribute vec3 aPosition;
        attribute vec4 aColor;
        varying vec4 vertexColor;
        uniform mat4 model;
        uniform mat4 view;
        uniform mat4 projection;
        void main()
        {
            gl_Position = projection * view * model * vec4(aPosition, 1.0);
            vertexColor = aColor;
            gl_PointSize = 2.0;
        }
        """);

    private string FragmentShaderSource => GetShader(true, """
        #version 100
        precision mediump float;
        varying vec4 vertexColor;
        void main()
        {
            gl_FragColor = vertexColor;
        }
        """);

    private static void CheckError(GlInterface gl)
    {
        int err;
        while ((err = gl.GetError()) != GL_NO_ERROR)
            Console.WriteLine(err);
    }

    static Stopwatch St = Stopwatch.StartNew();

    #region private fields

    private Shader? _shader;
    private BufferObject<float>? _vertexBufferObject;
    private VertexArrayObject<float>? _vertexArrayObject;
    private BufferObject<float>? _linesVertexBufferObject;
    private VertexArrayObject<float>? _linesVertexArrayObject;
    private GlVersion _glVersion;

    private readonly FastList<float> _pointsBuffer = new();
    private readonly FastList<float> _linesBuffer = new();

    #endregion
}
