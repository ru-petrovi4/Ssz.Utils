using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;
using Microsoft.EntityFrameworkCore;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace Ssz.Dcs.CentralServer.Common.EntityFramework
{
    /// <summary>
    ///    Operator
    /// </summary>   
    [Resource]
    public class User : Identifiable<Int64>, IOwnedDataSerializable
    {
        #region public functions                

        /// <summary>
        ///     Фамилия и инициалы
        /// </summary>
        [Attr]
        public string UserName { get; set; } = @"";

        /// <summary>
        ///     Табельный номер
        /// </summary>
        [Attr]
        public string PersonnelNumber { get; set; } = @"";

        /// <summary>
        ///     Domain\Username
        /// </summary>
        [Attr]
        public string WindowsUserName { get; set; } = @"";

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(UserName); }
        }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(UserName);
            writer.Write(PersonnelNumber);
            writer.Write(WindowsUserName);
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            UserName = reader.ReadString();
            PersonnelNumber = reader.ReadString();
            WindowsUserName = reader.ReadString();
        }

        #endregion
    }
}