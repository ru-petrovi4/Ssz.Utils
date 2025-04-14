using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using GuidAndName = Ssz.Operator.Core.Utils.GuidAndName;

namespace Ssz.Operator.Core
{
    public static class SerializationExtensions
    {
        #region public functions

        public static void WriteNullableOwnedData(this SerializationWriter writer, IOwnedDataSerializable? value,
            object? context)
        {
            using (writer.EnterBlock(0))
            {
                var hasValue = value is not null;
                writer.Write(hasValue);
                if (hasValue) value!.SerializeOwnedData(writer, context);
            }
        }

        public static void ReadNullableOwnedData(this SerializationReader reader, IOwnedDataSerializable? value,
            object? context)
        {
            using (reader.EnterBlock())
            {
                try
                {
                    var hasValue = reader.ReadBoolean();
                    if (hasValue && value is not null) value.DeserializeOwnedData(reader, context);
                }
                catch (BlockEndingException)
                {
                }
            }
        }

        public static IOwnedDataSerializable? ReadNullableOwnedData(this SerializationReader reader,
            Func<IOwnedDataSerializable> func,
            object? context)
        {
            using (reader.EnterBlock())
            {
                IOwnedDataSerializable? result = null;
                try
                {
                    var hasValue = reader.ReadBoolean();
                    if (hasValue)
                    {
                        result = func();
                        result.DeserializeOwnedData(reader, context);
                    }
                }
                catch (BlockEndingException)
                {
                }

                return result;
            }
        }

        public static void Write(this SerializationWriter writer, Color value)
        {
            writer.Write(value.A);
            writer.Write(value.R);
            writer.Write(value.G);
            writer.Write(value.B);
        }

        public static Color ReadColor(this SerializationReader reader)
        {
            var a = reader.ReadByte();
            var r = reader.ReadByte();
            var g = reader.ReadByte();
            var b = reader.ReadByte();
            return Color.FromArgb(a, r, g, b);
        }

        public static void Write(this SerializationWriter writer, Thickness value)
        {
            writer.Write(value.Left);
            writer.Write(value.Top);
            writer.Write(value.Right);
            writer.Write(value.Bottom);
        }

        public static Thickness ReadThickness(this SerializationReader reader)
        {
            var left = reader.ReadDouble();
            var top = reader.ReadDouble();
            var right = reader.ReadDouble();
            var bottom = reader.ReadDouble();
            return new Thickness(left, top, right, bottom);
        }

        public static void Write(this SerializationWriter writer, Matrix3D value)
        {
            writer.Write(value.M11);
            writer.Write(value.M12);
            writer.Write(value.M13);
            writer.Write(value.M14);
            writer.Write(value.M21);
            writer.Write(value.M22);
            writer.Write(value.M23);
            writer.Write(value.M24);
            writer.Write(value.M31);
            writer.Write(value.M32);
            writer.Write(value.M33);
            writer.Write(value.M34);
            writer.Write(value.OffsetX);
            writer.Write(value.OffsetY);
            writer.Write(value.OffsetZ);
            writer.Write(value.M44);
        }

        public static Matrix3D ReadMatrix3D(this SerializationReader reader)
        {
            var m11 = reader.ReadDouble();
            var m12 = reader.ReadDouble();
            var m13 = reader.ReadDouble();
            var m14 = reader.ReadDouble();
            var m21 = reader.ReadDouble();
            var m22 = reader.ReadDouble();
            var m23 = reader.ReadDouble();
            var m24 = reader.ReadDouble();
            var m31 = reader.ReadDouble();
            var m32 = reader.ReadDouble();
            var m33 = reader.ReadDouble();
            var m34 = reader.ReadDouble();
            var offsetX = reader.ReadDouble();
            var offsetY = reader.ReadDouble();
            var offsetZ = reader.ReadDouble();
            var m44 = reader.ReadDouble();
            return new Matrix3D(m11, m12, m13, m14,
                m21, m22, m23, m24,
                m31, m32, m33, m34,
                offsetX, offsetY, offsetZ, m44);
        }

        public static void WriteDsShapes(this SerializationWriter writer, DsShapeBase[] dsShapes, object? context)
        {
            writer.Write(dsShapes.Length);

            foreach (DsShapeBase dsShape in dsShapes)
                using (writer.EnterBlock(3))
                {
                    var dsShapeTypeGuid = dsShape.GetDsShapeTypeGuid();
                    if (ReferenceEquals(context, SerializationContext.ShortBytes))
                    {
                        writer.Write(dsShapeTypeGuid);
                    }
                    else
                    {
                        if (dsShapeTypeGuid == EmptyDsShape.DsShapeTypeGuid)
                        {
                            writer.Write(false);
                        }
                        else
                        {
                            writer.Write(true);
                            writer.Write(dsShapeTypeGuid);
                            writer.Write(dsShape.GetDsShapeTypeNameToDisplay());
                        }
                    }

                    writer.Write(dsShape, context);
                }
        }

        public static DsShapeBase[] ReadDsShapes(this SerializationReader reader, object? context,
            bool visualDesignMode,
            bool loadXamlContent)
        {
            var length = reader.ReadInt32();
            var dsShapes = new List<DsShapeBase>(length);

            for (var i = 0; i < length; i += 1)
                using (Block block = reader.EnterBlock())
                {
                    switch (block.Version)
                    {
                        case 3:
                        {
                            DsShapeBase? newDsShape;

                            var isNotEmpty = reader.ReadBoolean();
                            if (isNotEmpty)
                            {
                                var dsShapeTypeGuid = reader.ReadGuid();
                                string dsShapeTypeNameToDisplay = reader.ReadString();
                                newDsShape = DsShapeFactory.NewDsShape(dsShapeTypeGuid, visualDesignMode,
                                    loadXamlContent);
                                if (newDsShape is null)
                                {
                                    DsProject.LoggersSet.Logger.LogError("DsShape not found! DsShapeTypeGuid={0}, NameToDisplay={1}",
                                        dsShapeTypeGuid,
                                        dsShapeTypeNameToDisplay);
                                    continue;
                                }
                            }
                            else
                            {
                                newDsShape = new EmptyDsShape(visualDesignMode, loadXamlContent);
                            }

                            reader.ReadOwnedData(newDsShape, context);
                            dsShapes.Add(newDsShape);
                        }
                            break;
                        default:
                            throw new BlockUnsupportedVersionException();
                    }
                }

            return dsShapes.ToArray();
        }

        public static void WriteHashOfXaml(this SerializationWriter writer, string xamlWithRelativePaths,
            string? filesDirectoryFullName)
        {
            if (!string.IsNullOrEmpty(filesDirectoryFullName))
                using (HashAlgorithm hashAlgorithm = SHA256.Create())
                {
                    var usedFileNames = new HashSet<string>();
                    XamlHelper.GetUsedFileNames(xamlWithRelativePaths, usedFileNames);
                    foreach (string usedFileName in usedFileNames)
                    {
                        var fileInfo = new FileInfo(filesDirectoryFullName + @"\" + usedFileName);
                        if (fileInfo.Exists)
                        {
                            using (FileStream stream = File.OpenRead(fileInfo.FullName))
                            {
                                writer.Write(hashAlgorithm.ComputeHash(stream));
                            }

                            xamlWithRelativePaths =
                                xamlWithRelativePaths.Replace(@"=""file:./" + usedFileName + @"""", @"");
                        }
                    }
                }

            xamlWithRelativePaths = XamlHelper.GetXamlWithoutDesc(xamlWithRelativePaths)!;           
            writer.Write(xamlWithRelativePaths);
        }

        public static void WriteGuidAndName(this SerializationWriter writer, GuidAndName value)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(value.Guid);
                writer.Write(value.Name);
            }
        }

        public static GuidAndName ReadGuidAndName(this SerializationReader reader)
        {
            var value = new GuidAndName();
            using (var block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        value.Guid = reader.ReadGuid();
                        value.Name = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
            return value;
        }

        #endregion
    }

    public class SerializationContext
    {
        #region public functions

        public static SerializationContext ShortBytes = new();

        public static SerializationContext FullBytes = new();

        public static SerializationContext IndexFile = new();

        #endregion
    }
}