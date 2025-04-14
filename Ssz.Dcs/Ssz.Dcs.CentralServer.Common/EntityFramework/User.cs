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
    ///    Instructor or Operator
    /// </summary>   
    //[Resource]
    [Index(nameof(UserName))]
    public class User : Identifiable<Int64>, IOwnedDataSerializable
    {
        #region public functions                

        /// <summary>
        ///     ������� � ��������
        /// </summary>
        [Attr]
        public string UserName { get; set; } = @"";

        /// <summary>
        ///     ��������� �����
        /// </summary>
        [Attr]
        public string PersonnelNumber { get; set; } = @"";

        /// <summary>
        ///     Domain\Username
        /// </summary>
        [Attr]
        public string DomainUserName { get; set; } = @"";

        [Attr]
        public string ProcessModelNames { get; set; } = @"";

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(UserName); }
        }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(UserName);
            writer.Write(PersonnelNumber);
            writer.Write(DomainUserName);
            writer.Write(ProcessModelNames);
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            UserName = reader.ReadString();
            PersonnelNumber = reader.ReadString();
            DomainUserName = reader.ReadString();
            ProcessModelNames = reader.ReadString();
        }

        #endregion
    }
}