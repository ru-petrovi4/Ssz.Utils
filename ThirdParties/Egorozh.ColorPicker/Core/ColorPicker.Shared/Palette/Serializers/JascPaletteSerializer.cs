﻿/*
﻿The MIT License (MIT)

Copyright © 2013-2017 Cyotek Ltd.

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Drawing;
using System.IO;
using System.Text;

namespace Egorozh.ColorPicker
{
    /// <summary>
    /// Serializes and deserializes color palettes into and from the Jasc palette format.
    /// </summary>
    public class JascPaletteSerializer : PaletteSerializer
    {
        #region Properties

        /// <summary>
        /// Gets the default extension for files generated with this palette format.
        /// </summary>
        /// <value>The default extension for files generated with this palette format.</value>
        public override string[] DefaultExtension => new[] {"pal"};

        /// <summary>
        /// Gets a descriptive name of the palette format
        /// </summary>
        /// <value>The descriptive name of the palette format.</value>
        public override string Name => "JASC Palette";

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether this instance can read palette from data the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns><c>true</c> if this instance can read palette data from the specified stream; otherwise, <c>false</c>.</returns>
        public override bool CanReadFrom(Stream stream)
        {
            bool result;

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            try
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string header;
                    string version;

                    // check signature
                    header = reader.ReadLine();
                    version = reader.ReadLine();

                    result = header == "JASC-PAL" && version == "0100";
                }
            }
            catch
            {
                result = false;
            }

            return result;
        }

        public override List<Color> DeserializeNew(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var results = new List<Color>();

            using (var reader = new StreamReader(stream))
            {
                // check signature
                var header = reader.ReadLine();
                var version = reader.ReadLine();

                if (header != "JASC-PAL" || version != "0100")
                    throw new InvalidDataException("Invalid palette file");

                var colorCount = Convert.ToInt32(reader.ReadLine());

                for (var i = 0; i < colorCount; i++)
                {
                    var data = reader.ReadLine();
                    var parts = !string.IsNullOrEmpty(data)
                        ? data.Split(new[]
                        {
                            ' ',
                            '\t'
                        }, StringSplitOptions.RemoveEmptyEntries)
                        : new string[0];

                    if (!int.TryParse(parts[0], out var r) || !int.TryParse(parts[1], out var g) ||
                        !int.TryParse(parts[2], out var b))
                    {
                        throw new InvalidDataException($"Invalid palette contents found with data '{data}'");
                    }

                    results.Add(Color.FromArgb((byte) r, (byte) g, (byte) b));
                }
            }

            return results;
        }

        public override void Serialize(Stream stream, IEnumerable<Color> palette)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            using var writer = new StreamWriter(stream, Encoding.UTF8);

            writer.WriteLine("JASC-PAL");
            writer.WriteLine("0100");
            writer.WriteLine(palette.Count());

            foreach (var color in palette)
            {
                writer.Write("{0} ", color.R);
                writer.Write("{0} ", color.G);
                writer.Write("{0} ", color.B);
                writer.WriteLine();
            }
        }

        #endregion
    }
}