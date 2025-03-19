using Avalonia.OpenGL;
using System;

namespace Ssz.Utils.Avalonia.Model3D;

public class VertexArrayObject<TVertexType> : IDisposable
    where TVertexType : unmanaged    
{
    #region construction and destruction

    public VertexArrayObject(GlInterface gl)
    {
        _gl = gl;

        _handle = _gl.GenVertexArray();
        Bind();      
    }

    public void Dispose()
    {
        _gl.DeleteVertexArray(_handle);
    }

    #endregion        

    public unsafe void VertexAttributePointer(int index, int count, int type, int vertexSize, int offSet)
    {
        _gl.VertexAttribPointer(index, count, type, 0, vertexSize * sizeof(TVertexType), new IntPtr(offSet * sizeof(TVertexType)));
        _gl.EnableVertexAttribArray(index);
    }

    public void Bind()
    {
        _gl.BindVertexArray(_handle);
    }        

    #region private fields

    private int _handle;
    private GlInterface _gl;

    #endregion  
}
