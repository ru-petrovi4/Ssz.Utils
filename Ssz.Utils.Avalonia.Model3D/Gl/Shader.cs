using Avalonia.OpenGL;
using System;
using System.IO;
using static Avalonia.OpenGL.GlConsts;

namespace Ssz.Utils.Avalonia.Model3D
{
    public class Shader : IDisposable
    {
        #region construction and destruction

        public Shader(GlInterface gl, string vertexShaderSource, string fragmentShaderSource, Action<int> bindAttribLocation)
        {
            _gl = gl;

            int vertex = LoadShader(GL_VERTEX_SHADER, vertexShaderSource);
            int fragment = LoadShader(GL_FRAGMENT_SHADER, fragmentShaderSource);
            ProgramHandle = _gl.CreateProgram();
            _gl.AttachShader(ProgramHandle, vertex);
            _gl.AttachShader(ProgramHandle, fragment);

            bindAttribLocation(ProgramHandle);
            
            string? error = _gl.LinkProgramAndGetError(ProgramHandle);
            if (!String.IsNullOrEmpty(error))
            {
                throw new Exception($"Program failed to link with error: {error}");
            }
            _gl.DeleteShader(vertex);
            _gl.DeleteShader(fragment);
        }

        public void Dispose()
        {
            _gl.DeleteProgram(ProgramHandle);
        }

        #endregion

        #region public functions

        public int ProgramHandle { get; private set; }

        public void Use()
        {
            _gl.UseProgram(ProgramHandle);
        }

        //public void SetUniform(string name, int value)
        //{
        //    int location = _gl.GetUniformLocation(_shaderProgram, name);
        //    if (location == -1)
        //    {
        //        throw new Exception($"{name} uniform not found on shader.");
        //    }
        //    _gl.Uniform1(location, value);
        //}

        //public void SetUniform(string name, float value)
        //{
        //    int location = _gl.GetUniformLocation(_shaderProgram, name);
        //    if (location == -1)
        //    {
        //        throw new Exception($"{name} uniform not found on shader.");
        //    }
        //    _gl.Uniform1(location, value);
        //}        

        #endregion

        private int LoadShader(int shaderType, string shaderSource)
        {            
            int handle = _gl.CreateShader(shaderType);
            string? infoLog = _gl.CompileShaderAndGetError(handle, shaderSource); 
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception($"Error compiling shader of type {shaderType}, failed with error {infoLog}");
            }

            return handle;
        }

        #region private fields

        private GlInterface _gl;

        #endregion
    }
}
