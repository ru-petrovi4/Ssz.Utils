using Avalonia.OpenGL;
using System;
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;
using static Avalonia.OpenGL.GlConsts;

namespace Ssz.Utils.Avalonia.Model3D;

public class Texture : IDisposable
{
    #region construction and destruction

    public unsafe Texture(GlInterface gl, string path)
    {
        _gl = gl;

        _handle = _gl.GenTexture();
        Bind();

        ////Loading an image using imagesharp.
        //using (var img = Image.Load<Rgba32>(path))
        //{
        //    //Reserve enough memory from the gpu for the whole image
        //    gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint) img.Width, (uint) img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

        //    img.ProcessPixelRows(accessor =>
        //    {
        //        //ImageSharp 2 does not store images in contiguous memory by default, so we must send the image row by row
        //        for (int y = 0; y < accessor.Height; y++)
        //        {
        //            fixed (void* data = accessor.GetRowSpan(y))
        //            {
        //                //Loading the actual image.
        //                gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, y, (uint) accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, data);
        //            }
        //        }
        //    });
        //}

        SetParameters();

    }

    public unsafe Texture(GlInterface gl, Span<byte> data, int width, int height)
    {
        //Saving the gl instance.
        _gl = gl;

        //Generating the opengl handle;
        _handle = _gl.GenTexture();
        Bind();

        //We want the ability to create a texture using data generated from code aswell.
        fixed (void* d = &data[0])
        {
            //Setting the data of a texture.
            _gl.TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, new IntPtr(d)); // ? PixelFormat.Rgba
            SetParameters();
        }
    }

    public void Dispose()
    {
        //In order to dispose we need to delete the opengl handle for the texure.
        _gl.DeleteTexture(_handle);
    }

    #endregion

    #region public functions

    public void Bind(int textureSlot = GL_TEXTURE0)
    {
        //When we bind a texture we can choose which textureslot we can bind it to.
        _gl.ActiveTexture(textureSlot);
        _gl.BindTexture(GL_TEXTURE_2D, _handle);
    }    

    #endregion

    #region private functions

    private void SetParameters()
    {
        ////Setting some texture perameters so the texture behaves as expected.
        //_gl.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        //_gl.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        //_gl.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
        //_gl.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        //_gl.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
        //_gl.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);
        ////Generating mipmaps.
        //_gl.GenerateMipmap(TextureTarget.Texture2D);
    }

    #endregion

    #region private fields

    private int _handle;
    private GlInterface _gl;

    #endregion
}
