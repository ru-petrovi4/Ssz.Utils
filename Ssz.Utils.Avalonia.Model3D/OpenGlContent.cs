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
// ReSharper disable StringLiteralTypo

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

        // Create the vertex buffer object (VBO) for the vertex data.
        _vertexBufferObject = new BufferObject<float>(gl, GL_ARRAY_BUFFER);        
        CheckError(gl);
        
        _vertexArrayObject = new VertexArrayObject<float>(gl);        
        CheckError(gl);
       
        _vertexArrayObject.VertexAttributePointer(0, 3, GL_FLOAT, 7, 0); // aPosition
        _vertexArrayObject.VertexAttributePointer(1, 4, GL_FLOAT, 7, 3); // aColor
        gl.EnableVertexAttribArray(0);
        gl.EnableVertexAttribArray(1);
        CheckError(gl);
    }

    public void Deinit(GlInterface gl)
    {
        // Unbind everything
        gl.BindBuffer(GL_ARRAY_BUFFER, 0);
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
        gl.BindVertexArray(0);
        gl.UseProgram(0);

        // Delete all resources.
        _vertexBufferObject?.Dispose();
        _vertexArrayObject?.Dispose();

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
            var points = model3DMessage.Model3DScene.Point3DWithColorArray;
            if (points is not null)
            {
                float[] vertexData = new float[points.Length * 7];
                for (int i = 0; i < points.Length; i += 1)
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
                _vertexBufferObject!.BufferData(vertexData);
                _points = points;
            }
        }
       
        _vertexBufferObject!.Bind();
        _vertexArrayObject!.Bind();

        _shader!.Use();
        CheckError(gl);

        // Матрицы трансформации
        var model = Matrix4x4.Identity * Matrix4x4.CreateRotationX(model3DMessage.RotationX) * Matrix4x4.CreateRotationY(model3DMessage.RotationY);
        var view = Matrix4x4.CreateLookAt(new Vector3(0, 0, -model3DMessage.Zoom), Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)size.Width / (float)size.Height, 0.1f, 100.0f);

        unsafe
        {
            gl.UniformMatrix4fv(gl.GetUniformLocationString(_shader.ProgramHandle, "model"), 1, false, &model);
            gl.UniformMatrix4fv(gl.GetUniformLocationString(_shader.ProgramHandle, "view"), 1, false, &view);
            gl.UniformMatrix4fv(gl.GetUniformLocationString(_shader.ProgramHandle, "projection"), 1, false, &projection);
        }

        _vertexArrayObject!.Bind();

        if (_points is not null)
            gl.DrawArrays(0, 0, _points.Length);
        CheckError(gl);
    }

    #endregion

    private string GetShader(bool fragment, string shader)
    {
        return shader;
        //var version = (GlVersion.Type == GlProfileType.OpenGL
        //    ? RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 150 : 120
        //    : 100);
        //var data = "#version " + version + "\n";
        //if (GlVersion.Type == GlProfileType.OpenGLES)
        //    data += "precision mediump float;\n";
        //if (version >= 150)
        //{
        //    shader = shader.Replace("attribute", "in");
        //    if (fragment)
        //        shader = shader
        //            .Replace("varying", "in")
        //            .Replace("//DECLAREGLFRAG", "out vec4 outFragColor;")
        //            .Replace("gl_FragColor", "outFragColor");
        //    else
        //        shader = shader.Replace("varying", "out");
        //}

        //data += shader;

        //return data;
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
    private GlVersion _glVersion;

    private Point3DWithColor[]? _points;

    #endregion    
}
