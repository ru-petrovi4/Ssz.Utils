﻿using Microsoft.Extensions.Logging;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class Journal
    {
        #region construction and destruction

        /// <summary>
        ///     Must be initialized before first use.
        /// </summary>
        /// <param name="logger"></param>
        public Journal(ILogger<Journal>? logger = null)
        {
            _logger = logger;
        }

        #endregion

        #region private fields        

        public UInt32 AddItem(string elementId, string settings)
        {
            return 0;
        }

        public void RemoveItem(uint handle)
        {
            
        }

        public void WriteValue(UInt32 handle, UInt64 timeMs, double value, double min, double max)
        {
            
        }

        public void SaveToFiles(string directoryFullName, string fileNameBase, string fileExtension)
        {            
        }

        public void LoadFromFiles(string directoryFullName, string fileNameBase, string fileExtension)
        {            
        }

        #endregion

        #region private fields

        private ILogger<Journal>? _logger;

        #endregion
    }
}
