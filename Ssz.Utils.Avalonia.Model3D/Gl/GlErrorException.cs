using Avalonia.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Avalonia.OpenGL.GlConsts;

namespace Ssz.Utils.Avalonia.Model3D;

public class GlErrorException : Exception {
    public GlErrorException(string message) : base (message){ }

    public static void ThrowIfError(GlInterface gl) {
        int error = gl.GetError();
        if (error != GL_NO_ERROR) {
            throw new GlErrorException(error.ToString());
        }
    }
}
