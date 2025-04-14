using Avalonia.OpenGL;
using System;
using static Avalonia.OpenGL.GlConsts;

namespace Ssz.Utils.Avalonia.Model3D;

public class BufferObject<TDataType> : IDisposable
    where TDataType : unmanaged
{
    #region construction and destruction

    public BufferObject(GlInterface gl, int bufferType)
    {
        _gl = gl;
        _bufferType = bufferType;
        //Clear existing error code.
        int error;
        do error = _gl.GetError();
        while (error != GL_NO_ERROR);
        _handle = _gl.GenBuffer();
        Bind();
        //GlErrorException.ThrowIfError(gl);        
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(_handle);
    }

    #endregion

    #region public functions

    public unsafe void BufferData(Span<TDataType> data)
    {
        fixed (void* d = data)
        {
            _gl.BufferData(_bufferType, new IntPtr(data.Length * sizeof(TDataType)), new IntPtr(d), GL_STATIC_DRAW);
        }
        //GlErrorException.ThrowIfError(_gl);
    }

    public void Bind()
    {
        _gl.BindBuffer(_bufferType, _handle);
    }

    #endregion

    #region private fields

    private int _handle;
    private int _bufferType;
    private GlInterface _gl;

    #endregion
}
